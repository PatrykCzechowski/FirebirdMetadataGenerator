namespace DbMetaTool.Domain.Interfaces;

/// <summary>
/// Factory for creating IScriptExecutor instances with specific connection strings.
/// </summary>
public interface IScriptExecutorFactory
{
    /// <summary>
    /// Creates a script executor for the given connection string.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>A new IScriptExecutor instance.</returns>
    IScriptExecutor Create(string connectionString);
}
