namespace LeezenPass.Api.Infrastructure.Seed;

/// <summary>
/// The fixed parts of the demo: accounts and frame numbers the presenter types on stage. Everything else is generated.
/// Keep seed/README.md in sync.
/// </summary>
public static class DemoScenario
{
  /// <summary>Demo users only exist on a DemoMode instance, so a shared, documented password is fine.</summary>
  public const string Password = "LeezenDemo2026";

  public const string EmailDomain = "demo.local";

  public const string Owner = "owner@demo.local";
  public const string Buyer = "buyer@demo.local";
  public const string Partner = "partner@demo.local";
  public const string Admin = "admin@demo.local";

  /// <summary>owner's Gazelle, reported stolen at the main station. Check → <c>stolen</c>.</summary>
  public const string StolenFrame = "GZ-2024-OB77";

  /// <summary>Typo of <see cref="StolenFrame"/> (O→0, B→8), not registered. Check → <c>possible_match</c>.</summary>
  public const string LookAlikeFrame = "GZ-2024-0877";

  /// <summary>owner's Kalkhoff with an open transfer (<see cref="TransferCode"/>). Check → <c>verified_transfer</c>.</summary>
  public const string TransferFrame = "KH-5521-7730";

  public const string TransferFein = "MS-DE 2345 6789";

  /// <summary>Fixed so the buyer can type it on stage. Valid 48 h from seeding: re-seed before the pitch.</summary>
  public const string TransferCode = "PASS-2345";

  /// <summary>owner's Stevens, registered and clean. Check → <c>unknown</c> (registration is never revealed).</summary>
  public const string CleanFrame = "ST-8830-1145";

  public const string CleanFein = "MS-DE 4711 0815";

  /// <summary>Registered by the partner shop for a customer: <c>ThirdPartyVerified</c>.</summary>
  public const string PartnerFrame = "RM-3301-9920";

  /// <summary>Never registered. Check → <c>unknown</c>, same response as <see cref="CleanFrame"/>.</summary>
  public const string UnregisteredFrame = "XY-0000-1234";
}
