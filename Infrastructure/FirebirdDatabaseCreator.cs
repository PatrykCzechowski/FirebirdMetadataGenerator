using DbMetaTool.Domain.Interfaces;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;

namespace DbMetaTool.Infrastructure;

/// <summary>
/// Creates new Firebird databases.
/// </summary>
public class FirebirdDatabaseCreator(ILogger<FirebirdDatabaseCreator> logger) : IDatabaseCreator
{
    private const string DefaultUser = "SYSDBA";
    private const string DefaultPassword = "masterkey";
    private const int DefaultPageSize = 16384;
    private const string DefaultCharset = "UTF8";

    private readonly ILogger<FirebirdDatabaseCreator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<string> CreateDatabaseAsync(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path cannot be null or empty.", nameof(databasePath));
        }

        _logger.LogInformation("Creating new Firebird database at: {DatabasePath}", databasePath);

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("Created directory: {Directory}", directory);
        }

        if (File.Exists(databasePath))
        {
            throw new InvalidOperationException($"Database file already exists: {databasePath}");
        }

        var createConnectionString = BuildCreateDatabaseConnectionString(databasePath);

        try
        {
            await Task.Run(() =>
            {
                FbConnection.CreateDatabase(createConnectionString, overwrite: false);
            });

            _logger.LogInformation("Successfully created database: {DatabasePath}", databasePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database at {DatabasePath}", databasePath);
            throw;
        }

        return BuildConnectionString(databasePath);
    }

    private static string BuildCreateDatabaseConnectionString(string databasePath)
    {
        return $"User={DefaultUser};Password={DefaultPassword};Database={databasePath};" +
               $"DataSource=localhost;Charset={DefaultCharset};Dialect=3;" +
               $"Page Size={DefaultPageSize}";
    }

    private static string BuildConnectionString(string databasePath)
    {
        var builder = new FbConnectionStringBuilder
        {
            DataSource = "localhost",
            Database = databasePath,
            UserID = DefaultUser,
            Password = DefaultPassword,
            Charset = DefaultCharset,
            ServerType = FbServerType.Default,
            Dialect = 3
        };

        return builder.ToString();
    }
}
