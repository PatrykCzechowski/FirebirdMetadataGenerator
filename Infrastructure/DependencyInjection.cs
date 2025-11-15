using DbMetaTool.Application.Services;
using DbMetaTool.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DbMetaTool.Infrastructure;

/// <summary>
/// Dependency Injection configuration for the application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configures all services for dependency injection.
    /// </summary>
    public static IServiceProvider ConfigureServices(string? connectionString = null)
    {
        var services = new ServiceCollection();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/dbmetatool-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddSingleton(new ConnectionStringHolder(connectionString));
        }

        services.AddSingleton<IScriptExecutorFactory, FirebirdScriptExecutorFactory>();

        services.AddScoped<IMetadataReader>(sp =>
        {
            var connStr = sp.GetService<ConnectionStringHolder>()?.Value;
            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException("Connection string is required for IMetadataReader");
            return new FirebirdMetadataReader(connStr, sp.GetRequiredService<ILogger<FirebirdMetadataReader>>());
        });

        services.AddScoped<IScriptExecutor>(sp =>
        {
            var connStr = sp.GetService<ConnectionStringHolder>()?.Value;
            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException("Connection string is required for IScriptExecutor");
            return new FirebirdScriptExecutor(connStr, sp.GetRequiredService<ILogger<FirebirdScriptExecutor>>());
        });

        services.AddScoped<IScriptGenerator>(sp => 
            new SqlScriptGenerator(sp.GetRequiredService<ILogger<SqlScriptGenerator>>()));
        
        services.AddScoped<IDatabaseCreator>(sp => 
            new FirebirdDatabaseCreator(sp.GetRequiredService<ILogger<FirebirdDatabaseCreator>>()));

        services.AddScoped<DatabaseBuildService>();
        services.AddScoped<MetadataExportService>();
        services.AddScoped<DatabaseUpdateService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Helper class to hold connection string in DI container.
    /// </summary>
    private class ConnectionStringHolder(string value)
    {
        public string Value { get; } = value;
    }

    /// <summary>
    /// Closes and flushes Serilog logger.
    /// </summary>
    public static void Cleanup()
    {
        Log.CloseAndFlush();
    }
}
