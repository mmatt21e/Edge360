using System.Net;
using System.Net.Mail;
using Edge360.Application.Notifications;
using Microsoft.Extensions.Options;

namespace Edge360.Infrastructure.Notifications;

/// <summary>Sends email via SMTP. Enabled only when an SMTP host is configured.</summary>
public sealed class SmtpEmailNotificationChannel : INotificationChannel
{
    private readonly EmailOptions _options;

    public SmtpEmailNotificationChannel(IOptions<EmailOptions> options) => _options = options.Value;

    public string Name => "email";
    public bool IsEnabled => _options.IsConfigured;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.Email))
            return NotificationResult.Skip();

        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.UseSsl };
            if (!string.IsNullOrWhiteSpace(_options.Username))
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);

            using var mail = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = $"[Edge360] {message.Title}",
                Body = message.Body
            };
            mail.To.Add(message.Email);

            await client.SendMailAsync(mail, ct);
            return NotificationResult.Sent();
        }
        catch (Exception ex)
        {
            return NotificationResult.Failed(ex.Message);
        }
    }
}
