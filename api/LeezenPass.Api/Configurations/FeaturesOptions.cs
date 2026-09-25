namespace LeezenPass.Api.Configurations;

public class FeaturesOptions
{
  public const string Section = "Features";

  /// <summary>Switches every external dependency (email, captcha, vision, ...) to its Fake. Use for offline demos.</summary>
  public bool UseFakes { get; set; }

  /// <summary>Data is synthetic; the web app shows the "Demo-Daten" banner.</summary>
  public bool DemoMode { get; set; }
}
