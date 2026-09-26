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
    OwnershipHistory.Add(new BikeOwnershipHistory(Id, ownerId, createdAt));
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

  /// <summary>Only changed through the Mark* methods below, never set directly.</summary>
  public TrustLevel TrustLevel { get; private set; } = TrustLevel.SelfDeclared;
  public TrustSource? TrustSource { get; private set; }
  public DateTimeOffset? TrustVerifiedAt { get; private set; }

  public List<BikePhoto> Photos { get; private set; } = [];
  public List<TheftReport> TheftReports { get; private set; } = [];
  public List<BikeOwnershipHistory> OwnershipHistory { get; private set; } = [];

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

  /// <summary>A stolen bike can't change hands through LeezenPass.</summary>
  public bool CanTransfer => Status != BikeStatus.Stolen;

  /// <summary>
  /// Hands the bike to a new owner and keeps the history. Needs OwnershipHistory and Photos loaded.
  /// The seller's personal data does not travel with the bike: receipt photos (name/address) are removed
  /// and the FEIN code (encodes the seller's address) is cleared. Returns the removed photos so the
  /// caller can delete their files.
  /// </summary>
  public IReadOnlyList<BikePhoto> TransferTo(Guid newOwnerId, DateTimeOffset now)
  {
    if (!CanTransfer)
    {
      throw new InvalidOperationException("A stolen bike cannot be transferred.");
    }

    if (newOwnerId == OwnerId)
    {
      throw new InvalidOperationException("The bike already belongs to this user.");
    }

    foreach (var entry in OwnershipHistory.Where(h => h.ToAt is null))
    {
      entry.Close(now);
    }

    OwnershipHistory.Add(new BikeOwnershipHistory(Id, newOwnerId, now));
    OwnerId = newOwnerId;
    // A recovered bike starts fresh with its new owner.
    Status = BikeStatus.Active;
    FeinCodeHash = null;

    var receipts = Photos.Where(p => p.Kind == PhotoKind.Receipt).ToList();
    Photos.RemoveAll(p => p.Kind == PhotoKind.Receipt);
    return receipts;
  }

  /// <summary>
  /// Receipt check and possession check both passed. Never lowers a higher level (a partner-verified bike
  /// stays partner-verified).
  /// </summary>
  public void MarkEvidenceChecked(DateTimeOffset now)
  {
    if (TrustLevel >= TrustLevel.EvidenceChecked)
    {
      return;
    }

    TrustLevel = TrustLevel.EvidenceChecked;
    TrustSource = Bikes.TrustSource.ReceiptPossession;
    TrustVerifiedAt = now;
  }

  /// <summary>Registered by a partner (bike shop, ADFC coding event) who has seen the bike and its papers.</summary>
  public void MarkPartnerVerified(DateTimeOffset now)
  {
    TrustLevel = TrustLevel.ThirdPartyVerified;
    TrustSource = Bikes.TrustSource.Partner;
    TrustVerifiedAt = now;
  }

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
