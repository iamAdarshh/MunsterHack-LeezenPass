namespace LeezenPass.Api.Infrastructure.Captcha;

public enum CaptchaProvider
{
  /// <summary>Fake when Features:UseFakes is on, otherwise Turnstile.</summary>
  Auto,
  Fake,
  Turnstile,
}

public class CaptchaOptions
{
  public const string Section = "Captcha";

  public CaptchaProvider Provider { get; set; } = CaptchaProvider.Auto;

  /// <summary>Cloudflare Turnstile secret key. The web app needs the matching VITE_TURNSTILE_SITE_KEY.</summary>
  public string SecretKey { get; set; } = string.Empty;
}
