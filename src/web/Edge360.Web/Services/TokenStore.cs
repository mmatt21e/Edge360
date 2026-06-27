using Blazored.LocalStorage;
using Edge360.Web.Models;

namespace Edge360.Web.Services;

/// <summary>Persists the auth session (tokens + user) in browser local storage.</summary>
public sealed class TokenStore
{
    private const string AccessKey = "edge360.accessToken";
    private const string RefreshKey = "edge360.refreshToken";
    private const string UserKey = "edge360.user";

    private readonly ILocalStorageService _storage;

    public TokenStore(ILocalStorageService storage) => _storage = storage;

    public ValueTask<string?> GetAccessTokenAsync() => _storage.GetItemAsync<string?>(AccessKey);
    public ValueTask<string?> GetRefreshTokenAsync() => _storage.GetItemAsync<string?>(RefreshKey);
    public ValueTask<UserDto?> GetUserAsync() => _storage.GetItemAsync<UserDto?>(UserKey);

    public async Task SaveAsync(AuthResponse auth)
    {
        await _storage.SetItemAsync(AccessKey, auth.AccessToken);
        await _storage.SetItemAsync(RefreshKey, auth.RefreshToken);
        await _storage.SetItemAsync(UserKey, auth.User);
    }

    public async Task ClearAsync()
    {
        await _storage.RemoveItemAsync(AccessKey);
        await _storage.RemoveItemAsync(RefreshKey);
        await _storage.RemoveItemAsync(UserKey);
    }
}
