using System.Net.Http.Json;
using Edge360.Application.Notifications;
using Microsoft.Extensions.Options;

namespace Edge360.Infrastructure.Notifications;

/// <summary>
/// "Push" transport that POSTs a JSON payload to a configurable webhook URL. Compatible with
/// self-hosted push gateways. Enabled only when a URL is configured.
/// </summary>
public sealed class WebhookNotificationChannel : INotificationChannel
{
    private readonly HttpClient _http;
    private readonly WebhookOptions _options;

    public WebhookNotificationChannel(HttpClient http, IOptions<WebhookOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public string Name => "push";
    public bool IsEnabled => _options.IsConfigured;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Url)
            {
                Content = JsonContent.Create(new
                {
                    recipientUserId = message.RecipientUserId,
                    recipient = message.RecipientName,
                    pushTokens = message.PushTokens,
                    title = message.Title,
                    body = message.Body,
                    eventType = message.EventType,
                    severity = message.Severity
                })
            };

            if (!string.IsNullOrWhiteSpace(_options.AuthorizationHeader))
                request.Headers.TryAddWithoutValidation("Authorization", _options.AuthorizationHeader);

            var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode
                ? NotificationResult.Sent()
                : NotificationResult.Failed($"Webhook returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return NotificationResult.Failed(ex.Message);
        }
    }
}
