namespace Edge360.Application.Common.Abstractions;

/// <summary>Ambient information about the authenticated caller for the current request.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    string? IpAddress { get; }

    /// <summary>Returns the authenticated user id or throws if the request is anonymous.</summary>
    Guid RequireUserId();
}
