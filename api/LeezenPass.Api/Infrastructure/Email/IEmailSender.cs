namespace LeezenPass.Api.Infrastructure.Email;

public interface IEmailSender
{
  Task SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed record EmailMessage(string To, string Subject, string TextBody);
