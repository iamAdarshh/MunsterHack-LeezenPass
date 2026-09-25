namespace LeezenPass.Api.Infrastructure.Captcha;

public interface ICaptchaVerifier
{
  Task<bool> VerifyAsync(string? token, CancellationToken ct);
}
