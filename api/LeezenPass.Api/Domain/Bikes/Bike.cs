using System.Buffers.Text;
using System.Security.Cryptography;
using LeezenPass.Api.Domain.Theft;

namespace LeezenPass.Api.Domain.Bikes;

public class Bike
{
  private Bike() { }

  public Bike(Guid ownerId, FrameNumber frameNumber, DateTimeOffset createdAt)
  {
    Id = Guid.CreateVersion7();
    OwnerId = ownerId;
    PublicToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));
    CreatedAt = createdAt;
    SetFrameNumber(frameNumber);
  }

  public Guid Id { get; private set; }
  public Guid OwnerId { get; private set; }

  /// <summary>Unguessable id for public pages (QR tag, verification). Never the database id.</summary>
  public string PublicToken { get; private set; } = string.Empty;

  public string FrameNoRaw { get; private set; } = string.Empty;
  public string FrameNoNorm { get; private set; } = string.Empty;
  public string FrameNoLoose { get; private set; } = string.Empty;
  public string? FeinCodeHash { get; private set; }

  public string? Brand { get; set; }
  public string? Model { get; set; }
  public BikeType Type { get; set; } = BikeType.Other;
  public string? ColorPrimary { get; set; }
  public string? ColorSecondary { get; set; }
  public bool IsEbike { get; set; }
  public string? BatterySerial { get; set; }
  public List<string> Features { get; set; } = [];
  public DateOnly? PurchaseDate { get; set; }

  public BikeStatus Status { get; private set; } = BikeStatus.Active;
  public DateTimeOffset CreatedAt { get; private set; }

  public List<BikePhoto> Photos { get; private set; } = [];
  public List<TheftReport> TheftReports { get; private set; } = [];

  /// <summary>The report that made the bike stolen, if it is stolen right now. Needs TheftReports loaded.</summary>
  public TheftReport? OpenTheftReport => TheftReports.FirstOrDefault(t => t.Status == TheftReportStatus.Open);

  public void SetFrameNumber(FrameNumber frameNumber)
  {
    FrameNoRaw = frameNumber.Raw;
    FrameNoNorm = frameNumber.Normalized;
    FrameNoLoose = frameNumber.Loose;
  }

  public void SetFeinCode(FeinCode? feinCode, ReadOnlySpan<byte> secret) =>
    FeinCodeHash = feinCode?.ComputeHash(secret);

  public void ClearFeinCode() => FeinCodeHash = null;

  public bool CanReportStolen => Status != BikeStatus.Stolen;

  public bool CanMarkRecovered => Status == BikeStatus.Stolen;

  /// <summary>Opens a theft report and puts the bike on the public stolen list. Not allowed while already stolen.</summary>
  public TheftReport ReportStolen(DateTimeOffset stolenAt, double latitude, double longitude, LockType lockType, DateTimeOffset now)
  {
    if (!CanReportStolen)
    {
      throw new InvalidOperationException("Bike is already reported stolen.");
    }

    var report = new TheftReport(Id, stolenAt, latitude, longitude, lockType, now);
    TheftReports.Add(report);
    Status = BikeStatus.Stolen;
    return report;
  }

  /// <summary>Takes the bike off the public stolen list. Needs TheftReports loaded.</summary>
  public void MarkRecovered()
  {
    if (!CanMarkRecovered)
    {
      throw new InvalidOperationException("Only stolen bikes can be marked as recovered.");
    }

    OpenTheftReport?.MarkRecovered();
    Status = BikeStatus.Recovered;
  }

  /// <summary>Descriptive attributes. Colours/features must be keys from <see cref="BikeCatalog"/>.</summary>
  public void UpdateDetails(BikeDetails details)
  {
    Brand = Clean(details.Brand);
    Model = Clean(details.Model);
    Type = details.Type;
    ColorPrimary = details.ColorPrimary;
    ColorSecondary = details.ColorSecondary == details.ColorPrimary ? null : details.ColorSecondary;
    IsEbike = details.IsEbike;
    // A battery serial only makes sense on an e-bike.
    BatterySerial = details.IsEbike ? Clean(details.BatterySerial) : null;
    Features = details.Features.Distinct().Order(StringComparer.Ordinal).ToList();
    PurchaseDate = details.PurchaseDate;
  }

  private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record BikeDetails(
  string? Brand,
  string? Model,
  BikeType Type,
  string? ColorPrimary,
  string? ColorSecondary,
  bool IsEbike,
  string? BatterySerial,
  IReadOnlyList<string> Features,
  DateOnly? PurchaseDate);
