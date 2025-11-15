using DbMetaTool.Application.Validators;
using DbMetaTool.Common.Exceptions;
using DbMetaTool.Domain.Interfaces;

namespace DbMetaTool.Application.Services;

/// <summary>
/// Service for exporting metadata from database to SQL scripts.
/// </summary>
public class MetadataExportService(
    IMetadataReader metadataReader,
    IScriptGenerator scriptGenerator)
{
    private readonly IMetadataReader _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
    private readonly IScriptGenerator _scriptGenerator = scriptGenerator ?? throw new ArgumentNullException(nameof(scriptGenerator));

    public async Task ExportScriptsAsync(string connectionString, string outputDirectory)
    {
        ParameterValidator.ValidateConnectionString(connectionString);
        ParameterValidator.ValidateDirectory(outputDirectory, "--output-dir", mustExist: false);

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            Console.WriteLine($"Created output directory: {outputDirectory}");
        }

        try
        {
            Console.WriteLine("Reading database metadata...");
            
            var domains = await _metadataReader.ReadDomainsAsync();
            var tables = await _metadataReader.ReadTablesAsync();
            var procedures = await _metadataReader.ReadStoredProceduresAsync();

            var enumerable = domains.ToList();
            var enumerable1 = tables.ToList();
            var storedProcedures = procedures.ToList();
            Console.WriteLine($"Found {enumerable.Count} domain(s), {enumerable1.Count} table(s), {storedProcedures.Count} procedure(s).");

            Console.WriteLine("Generating scripts...");

            var domainsDir = Path.Combine(outputDirectory, "domains");
            var tablesDir = Path.Combine(outputDirectory, "tables");
            var proceduresDir = Path.Combine(outputDirectory, "procedures");

            Directory.CreateDirectory(domainsDir);
            Directory.CreateDirectory(tablesDir);
            Directory.CreateDirectory(proceduresDir);

            int counter = 1;
            foreach (var domain in enumerable)
            {
                var script = _scriptGenerator.GenerateDomainScript(domain);
                var fileName = Path.Combine(domainsDir, $"{counter:D3}_domain_{SanitizeFileName(domain.Name)}.sql");
                await File.WriteAllTextAsync(fileName, script);
                Console.WriteLine($"  Generated: {Path.GetFileName(fileName)}");
                counter++;
            }

            counter = 1;
            foreach (var table in enumerable1)
            {
                var script = _scriptGenerator.GenerateTableScript(table);
                var fileName = Path.Combine(tablesDir, $"{counter:D3}_table_{SanitizeFileName(table.Name)}.sql");
                await File.WriteAllTextAsync(fileName, script);
                Console.WriteLine($"  Generated: {Path.GetFileName(fileName)}");
                counter++;
            }

            counter = 1;
            foreach (var procedure in storedProcedures)
            {
                var script = _scriptGenerator.GenerateProcedureScript(procedure);
                var fileName = Path.Combine(proceduresDir, $"{counter:D3}_procedure_{SanitizeFileName(procedure.Name)}.sql");
                await File.WriteAllTextAsync(fileName, script);
                Console.WriteLine($"  Generated: {Path.GetFileName(fileName)}");
                counter++;
            }

            Console.WriteLine("Export completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Export error details: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            throw new DatabaseOperationException("Failed to export scripts.", ex);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
            .Replace(" ", "_")
            .ToLowerInvariant();
    }
}
