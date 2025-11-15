using DbMetaTool.Domain.Models;

namespace DbMetaTool.Domain.Interfaces;

/// <summary>
/// Defines contract for reading database metadata.
/// </summary>
public interface IMetadataReader
{
    /// <summary>
    /// Reads all user-defined domains from the database.
    /// </summary>
    /// <returns>Collection of domain definitions.</returns>
    Task<IEnumerable<Domain.Models.Domain>> ReadDomainsAsync();

    /// <summary>
    /// Reads all user tables from the database.
    /// </summary>
    /// <returns>Collection of table definitions with columns.</returns>
    Task<IEnumerable<Table>> ReadTablesAsync();

    /// <summary>
    /// Reads all stored procedures from the database.
    /// </summary>
    /// <returns>Collection of stored procedure definitions.</returns>
    Task<IEnumerable<StoredProcedure>> ReadStoredProceduresAsync();
}
