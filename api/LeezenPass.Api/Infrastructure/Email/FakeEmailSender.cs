namespace LeezenPass.Api.Infrastructure.Email;

public class FakeEmailSender(ILogger<FakeEmailSender> logger) : IEmailSender
{
  public Task SendAsync(EmailMessage message, CancellationToken ct)
  {
    // No recipient or body in logs (personal data).
    logger.LogInformation("Fake email not sent: {Subject}", message.Subject);
    return Task.CompletedTask;
  }
}
