namespace DbMetaTool.Domain.Interfaces;

/// <summary>
/// Defines contract for creating new Firebird databases.
/// </summary>
public interface IDatabaseCreator
{
    /// <summary>
    /// Creates a new empty Firebird database.
    /// </summary>
    /// <param name="databasePath">Full path where the database file should be created.</param>
    /// <returns>Connection string to the newly created database.</returns>
    Task<string> CreateDatabaseAsync(string databasePath);
}
