using DbMetaTool.Application.Validators;
using DbMetaTool.Common.Exceptions;
using DbMetaTool.Domain.Interfaces;

namespace DbMetaTool.Application.Services;

/// <summary>
/// Service for updating an existing database from scripts.
/// </summary>
public class DatabaseUpdateService(IScriptExecutor scriptExecutor)
{
    private readonly IScriptExecutor _scriptExecutor = scriptExecutor ?? throw new ArgumentNullException(nameof(scriptExecutor));

    public async Task UpdateDatabaseAsync(string connectionString, string scriptsDirectory)
    {
        ParameterValidator.ValidateConnectionString(connectionString);
        ParameterValidator.ValidateDirectory(scriptsDirectory, "--scripts-dir", mustExist: true);

        try
        {
            Console.WriteLine("Updating database from scripts...");
            
            await _scriptExecutor.ExecuteScriptsFromDirectoryAsync(scriptsDirectory);

            Console.WriteLine("Database update completed successfully.");
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Failed to update database.", ex);
        }
    }
}
