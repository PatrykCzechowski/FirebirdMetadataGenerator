namespace DbMetaTool.Domain.Interfaces;

/// <summary>
/// Defines contract for executing SQL scripts against a database.
/// </summary>
public interface IScriptExecutor
{
    /// <summary>
    /// Executes multiple SQL scripts from a directory.
    /// </summary>
    /// <param name="scriptsDirectory">Directory containing .sql files.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task ExecuteScriptsFromDirectoryAsync(string scriptsDirectory);
}
