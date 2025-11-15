namespace DbMetaTool.Domain.Models;

/// <summary>
/// Represents a stored procedure parameter definition.
/// </summary>
public class ProcedureParameter
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter type (0 = input, 1 = output).
    /// </summary>
    public short ParameterType { get; set; }

    /// <summary>
    /// Gets or sets the field type code.
    /// </summary>
    public short FieldType { get; set; }

    /// <summary>
    /// Gets or sets the data type as SQL string.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter position.
    /// </summary>
    public short Position { get; set; }

    /// <summary>
    /// Gets or sets whether this is an input parameter.
    /// </summary>
    public bool IsInput => ParameterType == 0;

    /// <summary>
    /// Gets or sets whether this is an output parameter.
    /// </summary>
    public bool IsOutput => ParameterType == 1;
}
