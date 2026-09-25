using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LeezenPass.Api.Infrastructure.Email;

/// <summary>Sends via SMTP (Mailpit in dev).</summary>
public class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
  private readonly EmailOptions _options = options.Value;

  public async Task SendAsync(EmailMessage message, CancellationToken ct)
  {
    var mime = new MimeMessage();
    mime.From.Add(MailboxAddress.Parse(_options.From));
    mime.To.Add(MailboxAddress.Parse(message.To));
    mime.Subject = message.Subject;
    mime.Body = new TextPart("plain") { Text = message.TextBody };

    using var client = new SmtpClient();
    await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort,
      _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
    if (!string.IsNullOrEmpty(_options.Username))
    {
      await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, ct);
    }

    await client.SendAsync(mime, ct);
    await client.DisconnectAsync(true, ct);

    // No recipient in logs (personal data).
    logger.LogInformation("Email sent: {Subject}", message.Subject);
  }
}
