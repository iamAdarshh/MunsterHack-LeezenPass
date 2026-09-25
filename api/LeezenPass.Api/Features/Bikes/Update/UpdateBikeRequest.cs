using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Features.Bikes.Update;

public sealed record UpdateBikeRequest : IBikeDetailsRequest
{
  public Guid Id { get; init; }
  public string FrameNumber { get; init; } = string.Empty;

  /// <summary>Empty = keep the stored code (it is never sent back to the client).</summary>
  public string? FeinCode { get; init; }

  public bool RemoveFeinCode { get; init; }
  public string? Brand { get; init; }
  public string? Model { get; init; }
  public BikeType Type { get; init; } = BikeType.Other;
  public string? ColorPrimary { get; init; }
  public string? ColorSecondary { get; init; }
  public bool IsEbike { get; init; }
  public string? BatterySerial { get; init; }
  public List<string> Features { get; init; } = [];
  public DateOnly? PurchaseDate { get; init; }
}
