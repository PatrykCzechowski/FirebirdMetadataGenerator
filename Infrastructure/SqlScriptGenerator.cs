using System.Text;
using DbMetaTool.Domain.Interfaces;
using DbMetaTool.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DbMetaTool.Infrastructure;

/// <summary>
/// Generates SQL scripts from metadata models.
/// </summary>
public class SqlScriptGenerator(ILogger<SqlScriptGenerator> logger) : IScriptGenerator
{
    private readonly ILogger<SqlScriptGenerator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string GenerateDomainScript(Domain.Models.Domain domain)
    {
        if (domain == null)
            throw new ArgumentNullException(nameof(domain));

        var sb = new StringBuilder();
        sb.Append($"CREATE DOMAIN {domain.Name} AS {domain.DataType}");

        if (!string.IsNullOrWhiteSpace(domain.DefaultValue))
        {
            var defaultValue = domain.DefaultValue.Trim();
            if (!defaultValue.StartsWith("DEFAULT", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($" DEFAULT {defaultValue}");
            }
            else
            {
                sb.Append($" {defaultValue}");
            }
        }

        if (!domain.IsNullable)
        {
            sb.Append(" NOT NULL");
        }

        if (!string.IsNullOrWhiteSpace(domain.CheckConstraint))
        {
            var checkConstraint = domain.CheckConstraint.Trim();
            if (!checkConstraint.StartsWith("CHECK", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($" CHECK ({checkConstraint})");
            }
            else
            {
                sb.Append($" {checkConstraint}");
            }
        }

        sb.AppendLine(";");
        return sb.ToString();
    }

    public string GenerateTableScript(Table table)
    {
        if (table == null)
            throw new ArgumentNullException(nameof(table));

        if (table.Columns == null || table.Columns.Count == 0)
            throw new ArgumentException("Table must have at least one column.", nameof(table));

        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE {table.Name}");
        sb.AppendLine("(");

        var columnDefinitions = new List<string>();

        foreach (var column in table.Columns.OrderBy(c => c.Position))
        {
            var columnDef = new StringBuilder();
            columnDef.Append($"    {column.Name} ");

            if (column.IsDomainBased)
            {
                columnDef.Append(column.DataType);
            }
            else
            {
                columnDef.Append(column.DataType);
            }

            if (!column.IsNullable)
            {
                columnDef.Append(" NOT NULL");
            }

            if (!string.IsNullOrWhiteSpace(column.DefaultValue))
            {
                var defaultValue = column.DefaultValue.Trim();
                if (!defaultValue.StartsWith("DEFAULT", StringComparison.OrdinalIgnoreCase))
                {
                    columnDef.Append($" DEFAULT {defaultValue}");
                }
                else
                {
                    columnDef.Append($" {defaultValue}");
                }
            }

            columnDefinitions.Add(columnDef.ToString());
        }

        sb.AppendLine(string.Join(",\n", columnDefinitions));
        sb.AppendLine(");");

        return sb.ToString();
    }

    public string GenerateProcedureScript(StoredProcedure procedure)
    {
        if (procedure == null)
            throw new ArgumentNullException(nameof(procedure));

        _logger.LogDebug("Generating script for procedure: {ProcedureName}", procedure.Name);

        var sb = new StringBuilder();
        
        sb.AppendLine("SET TERM ^ ;");
        sb.AppendLine();
        
        sb.Append($"CREATE OR ALTER PROCEDURE {procedure.Name}");

        if (procedure.InputParameters.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("(");
            var inputParams = procedure.InputParameters
                .OrderBy(p => p.Position)
                .Select(p => $"    {p.Name} {p.DataType}");
            sb.Append(string.Join(",\n", inputParams));
            sb.AppendLine();
            sb.Append(")");
        }

        if (procedure.OutputParameters.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("RETURNS");
            sb.AppendLine("(");
            var outputParams = procedure.OutputParameters
                .OrderBy(p => p.Position)
                .Select(p => $"    {p.Name} {p.DataType}");
            sb.Append(string.Join(",\n", outputParams));
            sb.AppendLine();
            sb.Append(")");
        }

        sb.AppendLine();
        sb.AppendLine("AS");
        sb.Append(procedure.Source.TrimEnd());

        if (!procedure.Source.TrimEnd().EndsWith("^"))
        {
            sb.AppendLine();
            sb.Append("^");
        }

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("SET TERM ; ^");

        return sb.ToString();
    }

    public string GenerateAddColumnScript(string tableName, Column column)
    {
        var sb = new StringBuilder();
        sb.Append($"ALTER TABLE {tableName} ADD {column.Name} {column.DataType}");
        
        if (!column.IsNullable)
        {
            sb.Append(" NOT NULL");
        }

        if (!string.IsNullOrWhiteSpace(column.DefaultValue))
        {
            var defaultValue = column.DefaultValue.Trim();
            if (!defaultValue.StartsWith("DEFAULT", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($" DEFAULT {defaultValue}");
            }
            else
            {
                sb.Append($" {defaultValue}");
            }
        }
        
        sb.Append(";");
        return sb.ToString();
    }

    public string GenerateDropColumnScript(string tableName, string columnName)
    {
        return $"ALTER TABLE {tableName} DROP {columnName};";
    }
}
