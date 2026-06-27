using System.Net.Http.Json;
using Edge360.Web.Models;

namespace Edge360.Web.Services;

/// <summary>
/// Calls the anonymous auth endpoints with a plain HttpClient (no bearer handler), and keeps the
/// token store + auth state in sync. Used for login, register, refresh and logout.
/// </summary>
public sealed class AuthClient
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokens;
    private readonly JwtAuthStateProvider _authState;

    public AuthClient(HttpClient http, TokenStore tokens, JwtAuthStateProvider authState)
    {
        _http = http;
        _tokens = tokens;
        _authState = authState;
    }

    public async Task<string?> LoginAsync(LoginRequest request)
    {
        var resp = await _http.PostAsJsonAsync("/api/auth/login", request);
        return await CompleteAsync(resp);
    }

    public async Task<string?> RegisterAsync(RegisterRequest request)
    {
        var resp = await _http.PostAsJsonAsync("/api/auth/register", request);
        return await CompleteAsync(resp);
    }

    /// <summary>Attempts a token refresh. Returns true on success.</summary>
    public async Task<bool> TryRefreshAsync()
    {
        var refresh = await _tokens.GetRefreshTokenAsync();
        if (string.IsNullOrWhiteSpace(refresh))
            return false;

        try
        {
            var resp = await _http.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(refresh));
            if (!resp.IsSuccessStatusCode)
                return false;

            var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth is null)
                return false;

            await _tokens.SaveAsync(auth);
            _authState.NotifyAuthChanged();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _tokens.ClearAsync();
        _authState.NotifyAuthChanged();
    }

    /// <summary>Returns null on success, or a user-facing error message on failure.</summary>
    private async Task<string?> CompleteAsync(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode)
        {
            var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth is not null)
            {
                await _tokens.SaveAsync(auth);
                _authState.NotifyAuthChanged();
                return null;
            }
            return "Unexpected empty response.";
        }

        return await ProblemReader.ReadMessageAsync(resp);
    }
}
