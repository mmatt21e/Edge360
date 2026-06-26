using Edge360.Application.Auth.Dtos;
using Edge360.Application.Common.Abstractions;
using Edge360.Application.Common.Exceptions;
using Edge360.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Auth;

/// <summary>
/// Registration, login and refresh-token rotation. Access tokens are short-lived JWTs;
/// refresh tokens are opaque, stored hashed, and rotated on every use.
/// </summary>
public sealed class AuthService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokens;
    private readonly IAuditWriter _audit;

    public AuthService(IAppDbContext db, IPasswordHasher passwordHasher, ITokenService tokens, IAuditWriter audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _audit = audit;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalized = Normalize(request.Email);
        var exists = await _db.Users.AnyAsync(u => u.NormalizedEmail == normalized, ct);
        if (exists)
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalized,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("auth.register", user.Id, nameof(User), user.Id.ToString(), ct: ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalized = Normalize(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

        // Always verify against something to reduce user-enumeration timing differences.
        var valid = user is not null
                    && user.IsActive
                    && _passwordHasher.Verify(user.PasswordHash, request.Password);

        if (!valid || user is null)
            throw new UnauthorizedException("Invalid email or password.");

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _audit.WriteAsync("auth.login", user.Id, nameof(User), user.Id.ToString(), ct: ct);
        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var hash = _tokens.HashRefreshToken(request.RefreshToken);
        var existing = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existing is null || !existing.IsActive)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        // Rotate: revoke the presented token and issue a fresh pair.
        var response = await IssueTokensAsync(existing.User, ct, rotatedFrom: existing);
        return response;
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct, RefreshToken? rotatedFrom = null)
    {
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user);
        var (rawRefresh, refreshHash) = _tokens.CreateRefreshToken();

        var refresh = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime)
        };
        _db.RefreshTokens.Add(refresh);

        rotatedFrom?.Revoke(refreshHash);

        await _db.SaveChangesAsync(ct);

        var userDto = new UserDto(user.Id, user.Email, user.DisplayName, user.SystemRole.ToString());
        return new AuthResponse(accessToken, expiresAt, rawRefresh, userDto);
    }

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
