namespace DbMetaTool.Domain.Models;

/// <summary>
/// Represents a database table definition.
/// </summary>
public class Table
{
    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of columns in this table.
    /// </summary>
    public List<Column> Columns { get; set; } = new();
}
