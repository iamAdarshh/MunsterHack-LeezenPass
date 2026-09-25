using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Features.Bikes.Register;

public sealed record RegisterBikeRequest : IBikeDetailsRequest
{
  public string FrameNumber { get; init; } = string.Empty;
  public string? FeinCode { get; init; }
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
