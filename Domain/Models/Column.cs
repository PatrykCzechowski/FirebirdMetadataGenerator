namespace DbMetaTool.Domain.Models;

/// <summary>
/// Represents a table column definition.
/// </summary>
public class Column
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type (or domain name).
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this column is based on a domain.
    /// </summary>
    public bool IsDomainBased { get; set; }

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
    /// Gets or sets whether the column allows NULL values.
    /// </summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>
    /// Gets or sets the default value expression.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the position of the column in the table.
    /// </summary>
    public int Position { get; set; }
}
