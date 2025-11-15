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
}
