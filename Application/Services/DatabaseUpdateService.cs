using DbMetaTool.Application.Validators;
using DbMetaTool.Common.Exceptions;
using DbMetaTool.Domain.Interfaces;
using DbMetaTool.Domain.Models;
using DbMetaTool.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DbMetaTool.Application.Services;

/// <summary>
/// Service for updating an existing database from scripts.
/// </summary>
public class DatabaseUpdateService(
    IScriptExecutor scriptExecutor,
    IMetadataReader dbReader,
    IScriptGenerator scriptGenerator,
    ILoggerFactory loggerFactory,
    ILogger<DatabaseUpdateService> logger)
{
    private readonly IScriptExecutor _scriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));
    private readonly IMetadataReader _dbReader = dbReader ?? throw new ArgumentNullException(nameof(dbReader));
    private readonly IScriptGenerator _scriptGenerator = scriptGenerator ?? throw new ArgumentNullException(nameof(scriptGenerator));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    private readonly ILogger<DatabaseUpdateService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task UpdateDatabaseAsync(string connectionString, string scriptsDirectory)
    {
        ParameterValidator.ValidateConnectionString(connectionString);
        ParameterValidator.ValidateDirectory(scriptsDirectory, "--scripts-dir", mustExist: true);

        try
        {
            _logger.LogInformation("Starting database synchronization...");

            var fileReader = new FileMetadataReader(scriptsDirectory, _loggerFactory.CreateLogger<FileMetadataReader>());

            _logger.LogInformation("Reading database metadata...");
            var dbDomains = (await _dbReader.ReadDomainsAsync()).ToList();
            var dbTables = (await _dbReader.ReadTablesAsync()).ToList();
            var dbProcs = (await _dbReader.ReadStoredProceduresAsync()).ToList();

            _logger.LogInformation("Reading file metadata...");
            var fileDomains = (await fileReader.ReadDomainsAsync()).ToList();
            var fileTables = (await fileReader.ReadTablesAsync()).ToList();
            var fileProcs = (await fileReader.ReadStoredProceduresAsync()).ToList();

            var domainsToCreate = fileDomains.Where(fd => !dbDomains.Any(dd => dd.Name.Equals(fd.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            foreach (var domain in domainsToCreate)
            {
                _logger.LogInformation("Creating new domain: {DomainName}", domain.Name);
                var script = _scriptGenerator.GenerateDomainScript(domain);
                await _scriptExecutor.ExecuteScriptAsync(script);
            }

            var procsToDrop = dbProcs.Where(dp => !fileProcs.Any(fp => fp.Name.Equals(dp.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            foreach (var proc in procsToDrop)
            {
                _logger.LogInformation("Dropping redundant procedure: {ProcedureName}", proc.Name);
                await _scriptExecutor.ExecuteScriptAsync($"DROP PROCEDURE {proc.Name};");
            }

            var tablesToDrop = dbTables.Where(dt => !fileTables.Any(ft => ft.Name.Equals(dt.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            foreach (var table in tablesToDrop)
            {
                _logger.LogInformation("Dropping redundant table: {TableName}", table.Name);
                await _scriptExecutor.ExecuteScriptAsync($"DROP TABLE {table.Name};");
            }

            var tablesToCreate = fileTables.Where(ft => !dbTables.Any(dt => dt.Name.Equals(ft.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            foreach (var table in tablesToCreate)
            {
                _logger.LogInformation("Creating new table: {TableName}", table.Name);
                var script = _scriptGenerator.GenerateTableScript(table);
                await _scriptExecutor.ExecuteScriptAsync(script);
            }

            var tablesToUpdate = fileTables.Where(ft => dbTables.Any(dt => dt.Name.Equals(ft.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            foreach (var fileTable in tablesToUpdate)
            {
                var dbTable = dbTables.First(dt => dt.Name.Equals(fileTable.Name, StringComparison.OrdinalIgnoreCase));
                
                var columnsToDrop = dbTable.Columns.Where(dc => !fileTable.Columns.Any(fc => fc.Name.Equals(dc.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                foreach (var col in columnsToDrop)
                {
                    _logger.LogInformation("Dropping column {ColumnName} from table {TableName}", col.Name, fileTable.Name);
                    var script = _scriptGenerator.GenerateDropColumnScript(fileTable.Name, col.Name);
                    await _scriptExecutor.ExecuteScriptAsync(script);
                }

                var columnsToAdd = fileTable.Columns.Where(fc => !dbTable.Columns.Any(dc => dc.Name.Equals(fc.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                foreach (var col in columnsToAdd)
                {
                    _logger.LogInformation("Adding column {ColumnName} to table {TableName}", col.Name, fileTable.Name);
                    var script = _scriptGenerator.GenerateAddColumnScript(fileTable.Name, col);
                    await _scriptExecutor.ExecuteScriptAsync(script);
                }
            }

            _logger.LogInformation("Updating procedures (Stub Pass)...");
            foreach (var proc in fileProcs)
            {
                var stubProc = new StoredProcedure
                {
                    Name = proc.Name,
                    InputParameters = proc.InputParameters,
                    OutputParameters = proc.OutputParameters,
                    Source = "BEGIN SUSPEND; END" // Minimal body, generator adds ^
                };
                
                var script = _scriptGenerator.GenerateProcedureScript(stubProc);
                await _scriptExecutor.ExecuteScriptAsync(script);
            }

            _logger.LogInformation("Updating procedures (Full Pass)...");
            foreach (var proc in fileProcs)
            {
                var script = _scriptGenerator.GenerateProcedureScript(proc);
                await _scriptExecutor.ExecuteScriptAsync(script);
            }

            _logger.LogInformation("Database synchronization completed successfully.");
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Failed to synchronize database.", ex);
        }
    }
}
