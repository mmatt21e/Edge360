using System.Net;
using System.Net.Http.Headers;

namespace Edge360.Web.Services;

/// <summary>
/// Attaches the bearer token to outgoing API requests and, on a 401, attempts a single token
/// refresh before retrying. Resolves <see cref="AuthClient"/> lazily to avoid a DI cycle.
/// </summary>
public sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly TokenStore _tokens;
    private readonly IServiceProvider _services;

    public AuthHeaderHandler(TokenStore tokens, IServiceProvider services)
    {
        _tokens = tokens;
        _services = services;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await AttachTokenAsync(request);
        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // Try to refresh once, then retry the original request.
        var authClient = (AuthClient)_services.GetService(typeof(AuthClient))!;
        if (await authClient.TryRefreshAsync())
        {
            response.Dispose();
            var retry = await CloneAsync(request);
            await AttachTokenAsync(retry);
            return await base.SendAsync(retry, ct);
        }

        return response;
    }

    private async Task AttachTokenAsync(HttpRequestMessage request)
    {
        var token = await _tokens.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }
}
