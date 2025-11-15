namespace DbMetaTool.Common.Exceptions;

/// <summary>
/// Exception thrown when database operation fails.
/// </summary>
public class DatabaseOperationException(string message, Exception innerException) : Exception(message, innerException);
