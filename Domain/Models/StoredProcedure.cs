namespace DbMetaTool.Domain.Models;

/// <summary>
/// Represents a stored procedure definition.
/// </summary>
public class StoredProcedure
{
    /// <summary>
    /// Gets or sets the procedure name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the complete procedure source code (body only).
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input parameters.
    /// </summary>
    public List<ProcedureParameter> InputParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the output parameters.
    /// </summary>
    public List<ProcedureParameter> OutputParameters { get; set; } = new();
}
