namespace LeezenPass.Api.Infrastructure.Captcha;

/// <summary>Accepts every token so the demo works offline.</summary>
public class FakeCaptchaVerifier : ICaptchaVerifier
{
  public Task<bool> VerifyAsync(string? token, CancellationToken ct) => Task.FromResult(true);
}
