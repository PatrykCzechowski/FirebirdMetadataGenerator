namespace DbMetaTool.Domain.Models;

/// <summary>
/// Represents a Firebird domain definition.
/// </summary>
public class Domain
{
    /// <summary>
    /// Gets or sets the domain name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the underlying data type (e.g., VARCHAR, INTEGER).
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field length (for character types).
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Gets or sets the numeric precision.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the numeric scale.
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Gets or sets whether the domain allows NULL values.
    /// </summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>
    /// Gets or sets the default value expression.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the CHECK constraint expression.
    /// </summary>
    public string? CheckConstraint { get; set; }
}
