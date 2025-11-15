using DbMetaTool.Common.Exceptions;

namespace DbMetaTool.Application.Validators;

/// <summary>
/// Validates command-line parameters and paths.
/// </summary>
public static class ParameterValidator
{
    public static void ValidateDirectory(string path, string parameterName, bool mustExist = true)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ValidationException($"Parameter {parameterName} cannot be null or empty.");

        if (mustExist && !Directory.Exists(path))
            throw new ValidationException($"Directory does not exist: {path}");

        if (mustExist)
        {
            return;
        }
        try
        {
            var directoryInfo = new DirectoryInfo(path);
            if (directoryInfo.Parent != null && !directoryInfo.Parent.Exists)
            {
                throw new ValidationException($"Parent directory does not exist: {directoryInfo.Parent.FullName}");
            }
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            throw new ValidationException($"Invalid directory path: {path}", ex);
        }
    }

    public static void ValidateConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ValidationException("Connection string cannot be null or empty.");

        if (!connectionString.Contains("database", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("initial catalog", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Connection string must contain database path.");
        }
    }

    public static void ValidateDatabasePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ValidationException("Database path cannot be null or empty.");

        try
        {
            Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ValidationException("Database path must include a file name.");

            if (!fileName.EndsWith(".fdb", StringComparison.OrdinalIgnoreCase) &&
                !fileName.EndsWith(".gdb", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Database file must have .fdb or .gdb extension.");
            }
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            throw new ValidationException($"Invalid database path: {path}", ex);
        }
    }
}
