using System.Text.RegularExpressions;
using DbMetaTool.Domain.Interfaces;
using DbMetaTool.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DbMetaTool.Infrastructure;

public class FileMetadataReader(string scriptsDirectory, ILogger<FileMetadataReader> logger) : IMetadataReader
{
    private readonly string _scriptsDirectory = scriptsDirectory ?? throw new ArgumentNullException(nameof(scriptsDirectory));
    private readonly ILogger<FileMetadataReader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IEnumerable<Domain.Models.Domain>> ReadDomainsAsync()
    {
        var domains = new List<Domain.Models.Domain>();
        var files = GetSqlFiles("domains");

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var domain = ParseDomain(content);
            if (domain != null)
            {
                domains.Add(domain);
            }
        }

        return domains;
    }

    public async Task<IEnumerable<Table>> ReadTablesAsync()
    {
        var tables = new List<Table>();
        var files = GetSqlFiles("tables");

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var table = ParseTable(content);
            if (table != null)
            {
                tables.Add(table);
            }
        }

        return tables;
    }

    public async Task<IEnumerable<StoredProcedure>> ReadStoredProceduresAsync()
    {
        var procedures = new List<StoredProcedure>();
        var files = GetSqlFiles("procedures");

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var procedure = ParseProcedure(content);
            if (procedure != null)
            {
                procedures.Add(procedure);
            }
        }

        return procedures;
    }

    private IEnumerable<string> GetSqlFiles(string subfolder)
    {
        var path = Path.Combine(_scriptsDirectory, subfolder);
        if (!Directory.Exists(path))
        {
            // Try looking in the root or other structures if specific folder doesn't exist
            // But based on workspace info, they are in examples/domains, examples/tables etc.
            // The user passes the root scripts directory.
            
            // If the user passes "examples", then "examples/domains" should exist.
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(path, "*.sql");
    }

    private Domain.Models.Domain? ParseDomain(string content)
    {
        // CREATE DOMAIN D_NAME AS VARCHAR(100) CHARACTER SET UTF8;
        var match = Regex.Match(content, @"CREATE\s+DOMAIN\s+(\w+)\s+AS\s+(.+?)(?:;|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success) return null;

        var name = match.Groups[1].Value;
        var definition = match.Groups[2].Value.Trim();

        // Simple parsing of definition
        // This is a simplification. A real parser would be better but this fits the task constraints.
        return new Domain.Models.Domain
        {
            Name = name,
            DataType = definition // Store the whole definition as DataType for now, or try to split it
        };
    }

    private Table? ParseTable(string content)
    {
        // CREATE TABLE CUSTOMERS ( ... );
        var match = Regex.Match(content, @"CREATE\s+TABLE\s+(\w+)\s*\(([\s\S]+)\);", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success) return null;

        var name = match.Groups[1].Value;
        var body = match.Groups[2].Value;

        var columns = new List<Column>();
        var lines = SplitColumns(body);

        short position = 1;
        foreach (var line in lines)
        {
            var parts = line.Trim().Split(new[] { ' ' }, 2);
            if (parts.Length < 2) continue;

            var colName = parts[0];
            var colDef = parts[1];

            // Remove trailing comma if present
            if (colDef.EndsWith(",")) colDef = colDef[..^1];

            columns.Add(new Column
            {
                Name = colName,
                DataType = colDef, // This contains type, not null, default etc.
                Position = position++,
                // We might need to parse IsNullable, DefaultValue etc if we want precise comparison
                // But for "Add missing columns", the full definition is enough to generate the ADD COLUMN script.
                // For "Drop redundant", name is enough.
            });
        }

        return new Table
        {
            Name = name,
            Columns = columns
        };
    }

    private StoredProcedure? ParseProcedure(string content)
    {
        // CREATE OR ALTER PROCEDURE Name (...) RETURNS (...) AS ...
        var match = Regex.Match(content, @"CREATE\s+(?:OR\s+ALTER\s+)?PROCEDURE\s+(\w+)(?:\s*\(([\s\S]*?)\))?\s*(?:RETURNS\s*\(([\s\S]*?)\))?\s*AS\s*([\s\S]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        if (!match.Success) return null;

        var name = match.Groups[1].Value;
        var inputParamsStr = match.Groups[2].Value;
        var outputParamsStr = match.Groups[3].Value;
        var body = match.Groups[4].Value;

        // Clean up body (remove SET TERM etc if captured)
        // The regex captures everything after AS.
        
        // Remove "SET TERM ; ^" from the end if present
        var setTermIndex = body.LastIndexOf("SET TERM", StringComparison.OrdinalIgnoreCase);
        if (setTermIndex != -1)
        {
            body = body.Substring(0, setTermIndex);
        }

        // Remove the terminator '^'
        var terminatorIndex = body.LastIndexOf('^');
        if (terminatorIndex != -1)
        {
            body = body.Substring(0, terminatorIndex);
        }
        
        return new StoredProcedure
        {
            Name = name,
            InputParameters = ParseParameters(inputParamsStr, false),
            OutputParameters = ParseParameters(outputParamsStr, true),
            Source = body.Trim()
        };
    }

    private List<ProcedureParameter> ParseParameters(string paramsStr, bool isOutput)
    {
        var parameters = new List<ProcedureParameter>();
        if (string.IsNullOrWhiteSpace(paramsStr)) return parameters;

        var lines = SplitColumns(paramsStr);
        short position = 1;

        foreach (var line in lines)
        {
            var parts = line.Trim().Split(new[] { ' ' }, 2);
            if (parts.Length < 2) continue;

            parameters.Add(new ProcedureParameter
            {
                Name = parts[0],
                DataType = parts[1].TrimEnd(','),
                Position = position++,
                ParameterType = (short)(isOutput ? 1 : 0) // 0=Input, 1=Output
            });
        }

        return parameters;
    }

    private List<string> SplitColumns(string content)
    {
        // Split by comma, but ignore commas inside parentheses (e.g. DECIMAL(10,2))
        var result = new List<string>();
        var current = "";
        int parenLevel = 0;

        foreach (var c in content)
        {
            if (c == '(') parenLevel++;
            if (c == ')') parenLevel--;

            if (c == ',' && parenLevel == 0)
            {
                if (!string.IsNullOrWhiteSpace(current))
                    result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        if (!string.IsNullOrWhiteSpace(current))
            result.Add(current.Trim());

        return result;
    }
}
