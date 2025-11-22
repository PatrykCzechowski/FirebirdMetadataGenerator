using DbMetaTool.Application.Validators;
using DbMetaTool.Common.Exceptions;
using DbMetaTool.Domain.Interfaces;
using DbMetaTool.Domain.Models;
using DbMetaTool.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DbMetaTool.Application.Services;

/// <summary>
/// Service for building a new database from scripts.
/// </summary>
public class DatabaseBuildService(
    IDatabaseCreator databaseCreator,
    IScriptExecutorFactory scriptExecutorFactory,
    IScriptGenerator scriptGenerator,
    ILoggerFactory loggerFactory,
    ILogger<DatabaseBuildService> logger)
{
    private readonly IScriptGenerator _scriptGenerator = scriptGenerator ?? throw new ArgumentNullException(nameof(scriptGenerator));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    private readonly ILogger<DatabaseBuildService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task BuildDatabaseAsync(string databaseDirectory, string scriptsDirectory)
    {
        ParameterValidator.ValidateDirectory(scriptsDirectory, "--scripts-dir", mustExist: true);
        ParameterValidator.ValidateDirectory(databaseDirectory, "--db-dir", mustExist: false);

        if (!Directory.Exists(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
            Console.WriteLine($"Created database directory: {databaseDirectory}");
        }

        var databasePath = Path.Combine(databaseDirectory, "database.fdb");
        ParameterValidator.ValidateDatabasePath(databasePath);

        try
        {
            _logger.LogInformation("Creating new database...");
            var connectionString = await databaseCreator.CreateDatabaseAsync(databasePath);

            _logger.LogInformation("Executing scripts...");
            var scriptExecutor = scriptExecutorFactory.Create(connectionString);
            
            var fileReader = new FileMetadataReader(scriptsDirectory, _loggerFactory.CreateLogger<FileMetadataReader>());

            await ExecuteRawScripts(scriptExecutor, scriptsDirectory, "domains");

            await ExecuteRawScripts(scriptExecutor, scriptsDirectory, "tables");

            _logger.LogInformation("Executing procedures (Stub Pass)...");
            var procedures = await fileReader.ReadStoredProceduresAsync();
            foreach (var proc in procedures)
            {
                var stubProc = new StoredProcedure
                {
                    Name = proc.Name,
                    InputParameters = proc.InputParameters,
                    OutputParameters = proc.OutputParameters,
                    Source = "BEGIN SUSPEND; END"
                };
                var script = _scriptGenerator.GenerateProcedureScript(stubProc);
                await scriptExecutor.ExecuteScriptAsync(script);
            }

            _logger.LogInformation("Executing procedures (Full Pass)...");
            var procedureFiles = Directory.GetFiles(Path.Combine(scriptsDirectory, "procedures"), "*.sql");
            foreach (var file in procedureFiles)
            {
                var content = await File.ReadAllTextAsync(file);
                var newContent = System.Text.RegularExpressions.Regex.Replace(content, @"CREATE\s+PROCEDURE", "CREATE OR ALTER PROCEDURE", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                _logger.LogDebug("Executing updated script: {FileName}", Path.GetFileName(file));
                await scriptExecutor.ExecuteScriptAsync(newContent);
            }

            _logger.LogInformation("Database build completed successfully.");
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Failed to build database.", ex);
        }
    }

    private async Task ExecuteRawScripts(IScriptExecutor executor, string rootDir, string subDir)
    {
        var path = Path.Combine(rootDir, subDir);
        if (Directory.Exists(path))
        {
            _logger.LogInformation("Executing scripts from {SubDir}...", subDir);
            await executor.ExecuteScriptsFromDirectoryAsync(path);
        }
    }
}
