namespace Core.Exceptions;

public class BadRequestException : BusinessException
{
    public BadRequestException(string message) : base(message)
    {
    }
}