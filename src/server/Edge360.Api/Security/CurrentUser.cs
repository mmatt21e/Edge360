using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Edge360.Application.Common.Abstractions;
using Edge360.Application.Common.Exceptions;

namespace Edge360.Api.Security;

/// <summary>Resolves the authenticated caller from the current HTTP context.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var sub = _accessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public Guid RequireUserId() => UserId ?? throw new UnauthorizedException("No authenticated user on the request.");
}
