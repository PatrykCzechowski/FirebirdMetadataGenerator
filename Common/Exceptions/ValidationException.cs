namespace DbMetaTool.Common.Exceptions;

/// <summary>
/// Exception thrown when parameter validation fails.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
