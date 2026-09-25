using System.Buffers.Text;
using System.Security.Cryptography;

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

  public void SetFrameNumber(FrameNumber frameNumber)
  {
    FrameNoRaw = frameNumber.Raw;
    FrameNoNorm = frameNumber.Normalized;
    FrameNoLoose = frameNumber.Loose;
  }

  public void SetFeinCode(FeinCode? feinCode, ReadOnlySpan<byte> secret) =>
    FeinCodeHash = feinCode?.ComputeHash(secret);
}
