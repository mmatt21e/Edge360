namespace Edge360.Application.Common.Exceptions;

/// <summary>Base class for expected, mappable application errors.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

/// <summary>A requested resource does not exist.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }

    public static NotFoundException For(string entity, object key) =>
        new($"{entity} '{key}' was not found.");
}

/// <summary>A request conflicts with existing state (e.g. duplicate email).</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>The caller is authenticated but not permitted to perform the action.</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message) { }
}

/// <summary>The caller is not authenticated or credentials are invalid.</summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication failed.") : base(message) { }
}

/// <summary>Input failed business/validation rules. Carries per-field messages.</summary>
public sealed class AppValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public AppValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
