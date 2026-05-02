namespace VeterinaryBackend.Domain.Exceptions;

public class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}
