using DbMetaTool.Application.Validators;
using DbMetaTool.Common.Exceptions;
using DbMetaTool.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DbMetaTool.Application.Services;

/// <summary>
/// Service for building a new database from scripts.
/// </summary>
public class DatabaseBuildService(
    IDatabaseCreator databaseCreator,
    IScriptExecutorFactory scriptExecutorFactory,
    ILogger<DatabaseBuildService> logger)
{
    private readonly IDatabaseCreator _databaseCreator = databaseCreator ?? throw new ArgumentNullException(nameof(databaseCreator));
    private readonly IScriptExecutorFactory _scriptExecutorFactory = scriptExecutorFactory ?? throw new ArgumentNullException(nameof(scriptExecutorFactory));
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
            var connectionString = await _databaseCreator.CreateDatabaseAsync(databasePath);

            _logger.LogInformation("Executing scripts...");
            var scriptExecutor = _scriptExecutorFactory.Create(connectionString);
            await scriptExecutor.ExecuteScriptsFromDirectoryAsync(scriptsDirectory);

            _logger.LogInformation("Database build completed successfully.");
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Failed to build database.", ex);
        }
    }
}
