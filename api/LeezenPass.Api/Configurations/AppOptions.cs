namespace LeezenPass.Api.Configurations;

public class AppOptions
{
  public const string Section = "App";

  /// <summary>Public origin of the web app, used in QR codes and share links (no trailing slash).</summary>
  public string PublicBaseUrl { get; set; } = "http://localhost:5173";
}
