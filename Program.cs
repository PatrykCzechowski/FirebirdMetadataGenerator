using DbMetaTool.Application.Services;
using DbMetaTool.Common.Exceptions;
using DbMetaTool.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DbMetaTool
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  build-db --db-dir <path> --scripts-dir <path>");
                Console.WriteLine("  export-scripts --connection-string <connStr> --output-dir <path>");
                Console.WriteLine("  update-db --connection-string <connStr> --scripts-dir <path>");
                return 1;
            }

            try
            {
                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "build-db":
                        {
                            string dbDir = GetArgValue(args, "--db-dir");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            BuildDatabase(dbDir, scriptsDir);
                            Console.WriteLine("Database built successfully.");
                            return 0;
                        }

                    case "export-scripts":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string outputDir = GetArgValue(args, "--output-dir");

                            ExportScripts(connStr, outputDir);
                            Console.WriteLine("Scripts exported successfully.");
                            return 0;
                        }

                    case "update-db":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            UpdateDatabase(connStr, scriptsDir);
                            Console.WriteLine("Database updated successfully.");
                            return 0;
                        }

                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return -1;
            }
        }

        private static string GetArgValue(string[] args, string name)
        {
            int idx = Array.IndexOf(args, name);
            if (idx == -1 || idx + 1 >= args.Length)
                throw new ArgumentException($"Missing required parameter {name}");
            return args[idx + 1];
        }

        private static void BuildDatabase(string databaseDirectory, string scriptsDirectory)
        {
            try
            {
                var serviceProvider = DependencyInjection.ConfigureServices();
                var logger = serviceProvider.GetRequiredService<ILogger<DatabaseBuildService>>();
                
                logger.LogInformation("=== BUILD DATABASE STARTED ===");
                logger.LogInformation("Database Directory: {DatabaseDirectory}", databaseDirectory);
                logger.LogInformation("Scripts Directory: {ScriptsDirectory}", scriptsDirectory);

                var service = serviceProvider.GetRequiredService<DatabaseBuildService>();
                service.BuildDatabaseAsync(databaseDirectory, scriptsDirectory).GetAwaiter().GetResult();
                
                logger.LogInformation("=== BUILD DATABASE COMPLETED SUCCESSFULLY ===");
                DependencyInjection.Cleanup();
            }
            catch (ValidationException ex)
            {
                DependencyInjection.Cleanup();
                throw new ArgumentException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                DependencyInjection.Cleanup();
                throw new InvalidOperationException("Failed to build database.", ex);
            }
        }
        
        private static void ExportScripts(string connectionString, string outputDirectory)
        {
            try
            {
                var serviceProvider = DependencyInjection.ConfigureServices(connectionString);
                var logger = serviceProvider.GetRequiredService<ILogger<MetadataExportService>>();
                
                logger.LogInformation("=== EXPORT SCRIPTS STARTED ===");
                logger.LogInformation("Output Directory: {OutputDirectory}", outputDirectory);

                var service = serviceProvider.GetRequiredService<MetadataExportService>();
                service.ExportScriptsAsync(connectionString, outputDirectory).GetAwaiter().GetResult();
                
                logger.LogInformation("=== EXPORT SCRIPTS COMPLETED SUCCESSFULLY ===");
                DependencyInjection.Cleanup();
            }
            catch (ValidationException ex)
            {
                DependencyInjection.Cleanup();
                throw new ArgumentException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                DependencyInjection.Cleanup();
                throw new InvalidOperationException("Failed to export scripts.", ex);
            }
        }
        
        private static void UpdateDatabase(string connectionString, string scriptsDirectory)
        {
            try
            {
                var serviceProvider = DependencyInjection.ConfigureServices(connectionString);
                var logger = serviceProvider.GetRequiredService<ILogger<DatabaseUpdateService>>();
                
                logger.LogInformation("=== UPDATE DATABASE STARTED ===");
                logger.LogInformation("Scripts Directory: {ScriptsDirectory}", scriptsDirectory);

                var service = serviceProvider.GetRequiredService<DatabaseUpdateService>();
                service.UpdateDatabaseAsync(connectionString, scriptsDirectory).GetAwaiter().GetResult();
                
                logger.LogInformation("=== UPDATE DATABASE COMPLETED SUCCESSFULLY ===");
                DependencyInjection.Cleanup();
            }
            catch (ValidationException ex)
            {
                DependencyInjection.Cleanup();
                throw new ArgumentException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                DependencyInjection.Cleanup();
                throw new InvalidOperationException("Failed to update database.", ex);
            }
        }
    }
}
