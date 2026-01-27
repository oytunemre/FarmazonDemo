namespace FarmazonDemo.Services.Common;

public abstract class ApiException : Exception
{
    public int StatusCode { get; }

    protected ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(404, message) { }
}

public sealed class ConflictException : ApiException
{
    public ConflictException(string message) : base(409, message) { }
}

public sealed class BadRequestException : ApiException
{
    public BadRequestException(string message) : base(400, message) { }
}

public sealed class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message) : base(401, message) { }
}
