using DbMetaTool.Domain.Interfaces;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;

namespace DbMetaTool.Infrastructure;

/// <summary>
/// Executes SQL scripts against a Firebird database.
/// </summary>
public class FirebirdScriptExecutor(string connectionString, ILogger<FirebirdScriptExecutor> logger)
    : IScriptExecutor
{
    private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    private readonly ILogger<FirebirdScriptExecutor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task ExecuteScriptAsync(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            return;

        await using var connection = new FbConnection(_connectionString);
        await connection.OpenAsync();

        var statements = SplitScriptIntoStatements(script);

        foreach (var statement in statements.Where(statement => !string.IsNullOrWhiteSpace(statement)))
        {
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await using var command = new FbCommand(statement, connection, transaction);
                command.CommandTimeout = 300; // 5 minutes timeout

                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                var errorMessage = $"Failed to execute statement: {statement[..Math.Min(200, statement.Length)]}";
                if (statement.Length > 200)
                {
                    errorMessage += "...";
                }
                errorMessage += $"\nFirebird error: {ex.Message}";
                
                throw new InvalidOperationException(errorMessage, ex);
            }
        }
    }

    public async Task ExecuteScriptsFromDirectoryAsync(string scriptsDirectory)
    {
        if (!Directory.Exists(scriptsDirectory))
            throw new DirectoryNotFoundException($"Scripts directory not found: {scriptsDirectory}");

        _logger.LogInformation("Executing scripts from directory: {Directory}", scriptsDirectory);

        var scriptFiles = GetScriptFilesInOrder(scriptsDirectory);

        if (scriptFiles.Count == 0)
        {
            _logger.LogWarning("No script files found in the directory");
            return;
        }

        _logger.LogInformation("Found {Count} script file(s) to execute", scriptFiles.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (var scriptFile in scriptFiles)
        {
            _logger.LogDebug("Executing: {FileName}", Path.GetFileName(scriptFile));
            
            var scriptContent = await File.ReadAllTextAsync(scriptFile);
            
            try
            {
                await ExecuteScriptAsync(scriptContent);
                successCount++;
                _logger.LogInformation("  ✓ Success: {FileName}", Path.GetFileName(scriptFile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "  ✗ Failed: {FileName}", Path.GetFileName(scriptFile));
                throw new InvalidOperationException($"Failed to execute script file: {scriptFile}", ex);
            }
        }

        _logger.LogInformation("Script execution completed. Success: {SuccessCount}, Failures: {FailureCount}", 
            successCount, failureCount);
    }

    private static List<string> GetScriptFilesInOrder(string scriptsDirectory)
    {
        var allFiles = Directory.GetFiles(scriptsDirectory, "*.sql", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();

        var orderedFiles = new List<string>();

        orderedFiles.AddRange(allFiles.Where(f => 
            f.Contains(Path.DirectorySeparatorChar + "domains" + Path.DirectorySeparatorChar) || 
            Path.GetFileName(f).ToLowerInvariant().Contains("domain")));

        orderedFiles.AddRange(allFiles.Where(f => 
            f.Contains(Path.DirectorySeparatorChar + "tables" + Path.DirectorySeparatorChar) || 
            Path.GetFileName(f).ToLowerInvariant().Contains("table")));

        orderedFiles.AddRange(allFiles.Where(f => 
            f.Contains(Path.DirectorySeparatorChar + "procedures" + Path.DirectorySeparatorChar) || 
            Path.GetFileName(f).ToLowerInvariant().Contains("procedure")));

        orderedFiles.AddRange(allFiles.Except(orderedFiles));

        return orderedFiles.Distinct().ToList();
    }

    private static List<string> SplitScriptIntoStatements(string script)
    {
        var statements = new List<string>();
        var currentStatement = new System.Text.StringBuilder();
        var lines = script.Split(['\r', '\n'], StringSplitOptions.None);
        var customTerminator = ";";

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("SET TERM", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmedLine.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    customTerminator = parts[2];
                }
                continue;
            }

            if (trimmedLine.StartsWith("--"))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmedLine) && currentStatement.Length == 0)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                if (currentStatement.Length > 0)
                {
                    currentStatement.AppendLine();
                }
                currentStatement.Append(line);
            }

            if (trimmedLine.EndsWith(customTerminator))
            {
                var statement = currentStatement.ToString();
                if (statement.EndsWith(customTerminator))
                {
                    statement = statement.Substring(0, statement.Length - customTerminator.Length).Trim();
                }
                
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    statements.Add(statement);
                }
                
                currentStatement.Clear();
            }
        }

        if (currentStatement.Length > 0)
        {
            var statement = currentStatement.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(statement))
            {
                if (statement.EndsWith(customTerminator))
                {
                    statement = statement.Substring(0, statement.Length - customTerminator.Length).Trim();
                }
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    statements.Add(statement);
                }
            }
        }

        return statements;
    }
}
