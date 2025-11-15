using System.Data;
using DbMetaTool.Domain.Interfaces;
using DbMetaTool.Domain.Models;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;

namespace DbMetaTool.Infrastructure;

/// <summary>
/// Reads metadata from Firebird database system tables.
/// </summary>
public class FirebirdMetadataReader(string connectionString, ILogger<FirebirdMetadataReader> logger)
    : IMetadataReader
{
    private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    private readonly ILogger<FirebirdMetadataReader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IEnumerable<Domain.Models.Domain>> ReadDomainsAsync()
    {
        var domains = new List<Domain.Models.Domain>();

        const string query = @"
            SELECT 
                TRIM(f.RDB$FIELD_NAME) AS DOMAIN_NAME,
                f.RDB$FIELD_TYPE AS FIELD_TYPE,
                f.RDB$FIELD_LENGTH AS FIELD_LENGTH,
                f.RDB$FIELD_PRECISION AS FIELD_PRECISION,
                f.RDB$FIELD_SCALE AS FIELD_SCALE,
                f.RDB$NULL_FLAG AS NULL_FLAG,
                f.RDB$DEFAULT_SOURCE AS DEFAULT_SOURCE,
                f.RDB$VALIDATION_SOURCE AS VALIDATION_SOURCE,
                f.RDB$CHARACTER_SET_ID AS CHAR_SET_ID,
                f.RDB$COLLATION_ID AS COLLATION_ID
            FROM RDB$FIELDS f
            WHERE f.RDB$FIELD_NAME NOT STARTING WITH 'RDB$'
                AND f.RDB$FIELD_NAME NOT STARTING WITH 'MON$'
                AND f.RDB$FIELD_NAME NOT STARTING WITH 'SEC$'
            ORDER BY f.RDB$FIELD_NAME";

        await using var connection = new FbConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new FbCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var fieldType = reader.GetInt16("FIELD_TYPE");
            var fieldLength = reader.IsDBNull("FIELD_LENGTH") ? null : (int?)reader.GetInt32("FIELD_LENGTH");
            var precision = reader.IsDBNull("FIELD_PRECISION") ? null : (short?)reader.GetInt16("FIELD_PRECISION");
            var scale = reader.IsDBNull("FIELD_SCALE") ? null : (short?)reader.GetInt16("FIELD_SCALE");
            var charSetId = reader.IsDBNull("CHAR_SET_ID") ? null : (int?)reader.GetInt16("CHAR_SET_ID");
            
            // For character types, calculate character length from byte length
            int? charLength = null;
            if (fieldType == 14 || fieldType == 37) // CHAR or VARCHAR
            {
                // UTF8 (charset 4) uses 4 bytes per character in Firebird
                // NONE (charset 0) or OCTETS (charset 1) use 1 byte per character
                var bytesPerChar = charSetId == 4 ? 4 : 1;
                charLength = fieldLength / bytesPerChar;
            }

            var domain = new Domain.Models.Domain
            {
                Name = reader.GetString("DOMAIN_NAME"),
                DataType = MapFirebirdTypeToSqlType(fieldType, precision, scale, charLength),
                Length = charLength,
                Precision = reader.IsDBNull("FIELD_PRECISION") ? null : reader.GetInt16("FIELD_PRECISION"),
                Scale = reader.IsDBNull("FIELD_SCALE") ? Math.Abs(reader.GetInt16("FIELD_SCALE")) : null,
                IsNullable = reader.IsDBNull("NULL_FLAG") || reader.GetInt16("NULL_FLAG") != 1,
                DefaultValue = reader.IsDBNull("DEFAULT_SOURCE") ? null : reader.GetString("DEFAULT_SOURCE").Trim(),
                CheckConstraint = reader.IsDBNull("VALIDATION_SOURCE") ? null : reader.GetString("VALIDATION_SOURCE").Trim()
            };

            domains.Add(domain);
        }

        return domains;
    }

    public async Task<IEnumerable<Table>> ReadTablesAsync()
    {
        var tables = new List<Table>();

        const string tablesQuery = @"
            SELECT TRIM(RDB$RELATION_NAME) AS TABLE_NAME
            FROM RDB$RELATIONS
            WHERE RDB$VIEW_BLR IS NULL
                AND RDB$SYSTEM_FLAG = 0
                AND RDB$RELATION_NAME NOT STARTING WITH 'RDB$'
                AND RDB$RELATION_NAME NOT STARTING WITH 'MON$'
                AND RDB$RELATION_NAME NOT STARTING WITH 'SEC$'
            ORDER BY RDB$RELATION_NAME";

        await using var connection = new FbConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new FbCommand(tablesQuery, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var tableName = reader.GetString("TABLE_NAME");
            var table = new Table
            {
                Name = tableName,
                Columns = (await ReadTableColumnsAsync(connection, tableName)).ToList()
            };
            tables.Add(table);
        }

        return tables;
    }

    private async Task<IEnumerable<Column>> ReadTableColumnsAsync(FbConnection connection, string tableName)
    {
        var columns = new List<Column>();

        const string columnsQuery = @"
            SELECT 
                TRIM(rf.RDB$FIELD_NAME) AS COLUMN_NAME,
                rf.RDB$FIELD_POSITION AS FIELD_POSITION,
                TRIM(rf.RDB$FIELD_SOURCE) AS FIELD_SOURCE,
                rf.RDB$NULL_FLAG AS NULL_FLAG,
                rf.RDB$DEFAULT_SOURCE AS DEFAULT_SOURCE,
                f.RDB$FIELD_TYPE AS FIELD_TYPE,
                f.RDB$FIELD_LENGTH AS FIELD_LENGTH,
                f.RDB$FIELD_PRECISION AS FIELD_PRECISION,
                f.RDB$FIELD_SCALE AS FIELD_SCALE,
                f.RDB$CHARACTER_SET_ID AS CHAR_SET_ID
            FROM RDB$RELATION_FIELDS rf
            JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
            WHERE rf.RDB$RELATION_NAME = @TableName
            ORDER BY rf.RDB$FIELD_POSITION";

        await using var command = new FbCommand(columnsQuery, connection);
        command.Parameters.AddWithValue("@TableName", tableName);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var fieldSource = reader.GetString("FIELD_SOURCE");
            var isDomainBased = !fieldSource.StartsWith("RDB$");
            
            var fieldType = reader.GetInt16("FIELD_TYPE");
            var fieldLength = reader.IsDBNull("FIELD_LENGTH") ? null : (int?)reader.GetInt32("FIELD_LENGTH");
            var precision = reader.IsDBNull("FIELD_PRECISION") ? null : (short?)reader.GetInt16("FIELD_PRECISION");
            var scale = reader.IsDBNull("FIELD_SCALE") ? null : (short?)reader.GetInt16("FIELD_SCALE");
            var charSetId = reader.IsDBNull("CHAR_SET_ID") ? null : (int?)reader.GetInt16("CHAR_SET_ID");
            
            int? charLength = null;
            if (fieldType == 14 || fieldType == 37)
            {
                var bytesPerChar = charSetId == 4 ? 4 : 1;
                charLength = fieldLength / bytesPerChar;
            }

            var column = new Column
            {
                Name = reader.GetString("COLUMN_NAME"),
                Position = reader.GetInt16("FIELD_POSITION"),
                IsDomainBased = isDomainBased,
                DataType = isDomainBased 
                    ? fieldSource 
                    : MapFirebirdTypeToSqlType(fieldType, precision, scale, charLength),
                Length = charLength,
                Precision = precision,
                Scale = scale.HasValue ? Math.Abs(scale.Value) : null,
                IsNullable = reader.IsDBNull("NULL_FLAG") || reader.GetInt16("NULL_FLAG") != 1,
                DefaultValue = reader.IsDBNull("DEFAULT_SOURCE") ? null : reader.GetString("DEFAULT_SOURCE").Trim()
            };

            columns.Add(column);
        }

        return columns;
    }

    public async Task<IEnumerable<StoredProcedure>> ReadStoredProceduresAsync()
    {
        _logger.LogInformation("Reading stored procedures from database...");
        var procedures = new List<StoredProcedure>();

        const string query = @"
            SELECT 
                TRIM(RDB$PROCEDURE_NAME) AS PROCEDURE_NAME,
                RDB$PROCEDURE_SOURCE AS PROCEDURE_SOURCE
            FROM RDB$PROCEDURES
            WHERE RDB$SYSTEM_FLAG = 0
                AND RDB$PROCEDURE_NAME NOT STARTING WITH 'RDB$'
            ORDER BY RDB$PROCEDURE_NAME";

        try
        {
            await using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new FbCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var procedureName = reader.GetString("PROCEDURE_NAME");
                
                var parameters = await ReadProcedureParametersAsync(connection, procedureName);

                var procedure = new StoredProcedure
                {
                    Name = procedureName,
                    Source = reader.IsDBNull("PROCEDURE_SOURCE") 
                        ? string.Empty 
                        : reader.GetString("PROCEDURE_SOURCE"),
                    InputParameters = parameters.Where(p => p.IsInput).ToList(),
                    OutputParameters = parameters.Where(p => p.IsOutput).ToList()
                };

                procedures.Add(procedure);
            }

            _logger.LogInformation("Successfully read {Count} stored procedures", procedures.Count);
            return procedures;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read stored procedures from database");
            throw;
        }
    }

    private async Task<List<ProcedureParameter>> ReadProcedureParametersAsync(FbConnection connection, string procedureName)
    {
        var parameters = new List<ProcedureParameter>();

        const string query = @"
            SELECT 
                TRIM(pp.RDB$PARAMETER_NAME) AS PARAM_NAME,
                pp.RDB$PARAMETER_TYPE AS PARAM_TYPE,
                pp.RDB$PARAMETER_NUMBER AS PARAM_NUMBER,
                f.RDB$FIELD_TYPE AS FIELD_TYPE,
                f.RDB$FIELD_LENGTH AS FIELD_LENGTH,
                f.RDB$FIELD_PRECISION AS FIELD_PRECISION,
                f.RDB$FIELD_SCALE AS FIELD_SCALE,
                f.RDB$CHARACTER_SET_ID AS CHAR_SET_ID
            FROM RDB$PROCEDURE_PARAMETERS pp
            JOIN RDB$FIELDS f ON pp.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
            WHERE TRIM(pp.RDB$PROCEDURE_NAME) = @ProcedureName
            ORDER BY pp.RDB$PARAMETER_TYPE, pp.RDB$PARAMETER_NUMBER";

        await using var command = new FbCommand(query, connection);
        command.Parameters.AddWithValue("@ProcedureName", procedureName);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var fieldType = reader.GetInt16("FIELD_TYPE");
            var fieldLength = reader.IsDBNull("FIELD_LENGTH") ? null : (int?)reader.GetInt32("FIELD_LENGTH");
            var fieldPrecision = reader.IsDBNull("FIELD_PRECISION") ? null : (int?)reader.GetInt16("FIELD_PRECISION");
            var fieldScale = reader.IsDBNull("FIELD_SCALE") ? null : (int?)reader.GetInt16("FIELD_SCALE");
            var charSetId = reader.IsDBNull("CHAR_SET_ID") ? null : (int?)reader.GetInt16("CHAR_SET_ID");

            var parameter = new ProcedureParameter
            {
                Name = reader.GetString("PARAM_NAME"),
                ParameterType = reader.GetInt16("PARAM_TYPE"),
                Position = reader.GetInt16("PARAM_NUMBER"),
                FieldType = fieldType,
                DataType = MapFirebirdTypeToSqlTypeForParams(fieldType, fieldLength, fieldPrecision, fieldScale, charSetId)
            };

            parameters.Add(parameter);
        }

        return parameters;
    }

    private static string MapFirebirdTypeToSqlType(short fieldType, short? precision, short? scale, int? charLength)
    {
        return fieldType switch
        {
            7 => scale.HasValue && scale.Value > 0 ? $"NUMERIC(18, {scale.Value})" : "SMALLINT",
            8 => scale.HasValue && scale.Value > 0 ? $"NUMERIC(18, {scale.Value})" : "INTEGER",
            10 => "FLOAT",
            12 => "DATE",
            13 => "TIME",
            14 => charLength.HasValue ? $"CHAR({charLength.Value})" : "CHAR(1)",
            16 => scale.HasValue && scale.Value > 0 
                ? $"NUMERIC({precision ?? 18}, {scale.Value})" 
                : "BIGINT",
            27 => "DOUBLE PRECISION",
            35 => "TIMESTAMP",
            37 => charLength.HasValue ? $"VARCHAR({charLength.Value})" : "VARCHAR(1)",
            261 => "BLOB SUB_TYPE TEXT",
            _ => "VARCHAR(50)"
        };
    }

    private static string MapFirebirdTypeToSqlTypeForParams(short fieldType, int? fieldLength, int? precision, int? scale, int? charSetId)
    {
        int? charLength = null;
        if ((fieldType == 14 || fieldType == 37) && fieldLength.HasValue)
        {
            var bytesPerChar = charSetId == 4 ? 4 : 1;
            charLength = fieldLength.Value / bytesPerChar;
        }
        
        return fieldType switch
        {
            7 => scale.HasValue && scale.Value < 0 ? $"NUMERIC({precision ?? 9},{Math.Abs(scale.Value)})" : "SMALLINT",
            8 => scale.HasValue && scale.Value < 0 ? $"NUMERIC({precision ?? 18},{Math.Abs(scale.Value)})" : "INTEGER",
            10 => "FLOAT",
            12 => "DATE",
            13 => "TIME",
            14 => charSetId == 4 ? $"CHAR({charLength}) CHARACTER SET UTF8" : $"CHAR({charLength})",
            16 => scale.HasValue && scale.Value < 0 ? $"NUMERIC({precision ?? 18},{Math.Abs(scale.Value)})" : "BIGINT",
            27 => "DOUBLE PRECISION",
            35 => "TIMESTAMP",
            37 => charSetId == 4 ? $"VARCHAR({charLength}) CHARACTER SET UTF8" : $"VARCHAR({charLength})",
            261 => "BLOB SUB_TYPE TEXT",
            _ => $"UNKNOWN_TYPE({fieldType})"
        };
    }
}

/// <summary>
/// Extension methods for DbDataReader.
/// </summary>
internal static class DataReaderExtensions
{
    public static string GetString(this IDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetString(ordinal);
    }

    public static short GetInt16(this IDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetInt16(ordinal);
    }

    public static int GetInt32(this IDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetInt32(ordinal);
    }

    public static bool IsDBNull(this IDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal);
    }
}
