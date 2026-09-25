namespace LeezenPass.Api.Configurations;

public class FeinOptions
{
  public const string Section = "Fein";

  /// <summary>Server-side HMAC key for FEIN code hashes. At least 32 bytes. Never commit a real one.</summary>
  public string HmacSecret { get; set; } = string.Empty;
}
