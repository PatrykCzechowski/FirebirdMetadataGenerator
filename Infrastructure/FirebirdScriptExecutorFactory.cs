using DbMetaTool.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DbMetaTool.Infrastructure;

/// <summary>
/// Factory for creating FirebirdScriptExecutor instances.
/// </summary>
public class FirebirdScriptExecutorFactory(ILoggerFactory loggerFactory) : IScriptExecutorFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    public IScriptExecutor Create(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        var logger = _loggerFactory.CreateLogger<FirebirdScriptExecutor>();
        return new FirebirdScriptExecutor(connectionString, logger);
    }
}
