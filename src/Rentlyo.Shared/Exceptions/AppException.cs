namespace Rentlyo.Shared.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}

public sealed class ValidationException : AppException
{
    public ValidationException(string message) : base(message)
    {
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message) : base(message)
    {
    }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

public sealed class BusinessException : AppException
{
    public BusinessException(string message) : base(message)
    {
    }
}
