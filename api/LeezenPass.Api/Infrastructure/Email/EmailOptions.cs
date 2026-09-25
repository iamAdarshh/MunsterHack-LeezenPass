namespace LeezenPass.Api.Infrastructure.Email;

public class EmailOptions
{
  public const string Section = "Email";

  public string SmtpHost { get; set; } = "localhost";
  public int SmtpPort { get; set; } = 1025;
  public bool UseSsl { get; set; }
  public string? Username { get; set; }
  public string? Password { get; set; }
  public string From { get; set; } = "LeezenPass <noreply@leezenpass.local>";
}
