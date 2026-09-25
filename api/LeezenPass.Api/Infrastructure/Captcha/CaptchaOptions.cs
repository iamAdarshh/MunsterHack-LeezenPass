namespace LeezenPass.Api.Infrastructure.Captcha;

public class CaptchaOptions
{
  public const string Section = "Captcha";

  /// <summary>Cloudflare Turnstile secret key.</summary>
  public string SecretKey { get; set; } = string.Empty;
}
