using DbMetaTool.Domain.Models;

namespace DbMetaTool.Domain.Interfaces;

/// <summary>
/// Defines contract for generating SQL scripts from metadata models.
/// </summary>
public interface IScriptGenerator
{
    /// <summary>
    /// Generates CREATE DOMAIN script.
    /// </summary>
    /// <param name="domain">The domain definition.</param>
    /// <returns>SQL script text.</returns>
    string GenerateDomainScript(Domain.Models.Domain domain);

    /// <summary>
    /// Generates CREATE TABLE script.
    /// </summary>
    /// <param name="table">The table definition.</param>
    /// <returns>SQL script text.</returns>
    string GenerateTableScript(Table table);

    /// <summary>
    /// Generates CREATE PROCEDURE script.
    /// </summary>
    /// <param name="procedure">The procedure definition.</param>
    /// <returns>SQL script text.</returns>
    string GenerateProcedureScript(StoredProcedure procedure);

    /// <summary>
    /// Generates ALTER TABLE ADD COLUMN script.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="column">The column definition.</param>
    /// <returns>SQL script text.</returns>
    string GenerateAddColumnScript(string tableName, Column column);

    /// <summary>
    /// Generates ALTER TABLE DROP COLUMN script.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columnName">The column name.</param>
    /// <returns>SQL script text.</returns>
    string GenerateDropColumnScript(string tableName, string columnName);
}
