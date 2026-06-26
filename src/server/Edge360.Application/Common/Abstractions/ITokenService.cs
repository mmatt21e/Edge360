using Edge360.Domain.Entities;

namespace Edge360.Application.Common.Abstractions;

/// <summary>Issues JWT access tokens and opaque refresh tokens.</summary>
public interface ITokenService
{
    /// <summary>Creates a signed JWT for the user and returns it with its UTC expiry.</summary>
    (string token, DateTimeOffset expiresAt) CreateAccessToken(User user);

    /// <summary>Generates a new opaque refresh token, returning the raw value and its stored hash.</summary>
    (string raw, string hash) CreateRefreshToken();

    /// <summary>Hashes a raw refresh token so it can be matched against stored values.</summary>
    string HashRefreshToken(string raw);
}
