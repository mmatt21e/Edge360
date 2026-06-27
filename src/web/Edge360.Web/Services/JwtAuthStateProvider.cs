using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Edge360.Web.Services;

/// <summary>
/// Builds the authentication state from the stored JWT access token by decoding its claims.
/// </summary>
public sealed class JwtAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly TokenStore _tokens;

    public JwtAuthStateProvider(TokenStore tokens) => _tokens = tokens;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaims(token);
        if (claims is null)
            return Anonymous;

        var identity = new ClaimsIdentity(claims, "jwt", "name", ClaimTypes.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static IEnumerable<Claim>? ParseClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return null;

        try
        {
            var payload = Base64UrlDecode(parts[1]);
            var map = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
            if (map is null)
                return null;

            var claims = new List<Claim>();
            foreach (var (key, value) in map)
            {
                if (value.ValueKind == JsonValueKind.Array)
                    claims.AddRange(value.EnumerateArray().Select(v => new Claim(key, v.ToString())));
                else
                    claims.Add(new Claim(key, value.ToString()));
            }
            return claims;
        }
        catch
        {
            return null;
        }
    }

    private static string Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
}
