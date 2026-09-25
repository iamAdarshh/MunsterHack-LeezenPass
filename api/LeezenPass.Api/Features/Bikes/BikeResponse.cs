using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Features.Bikes;

/// <summary>Owner view of a bike. Never returned by public endpoints. The FEIN code itself is never returned.</summary>
public sealed record BikeResponse(
  Guid Id,
  string FrameNumber,
  string? Brand,
  string? Model,
  BikeType Type,
  string? ColorPrimary,
  string? ColorSecondary,
  bool IsEbike,
  string? BatterySerial,
  IReadOnlyList<string> Features,
  DateOnly? PurchaseDate,
  BikeStatus Status,
  bool HasFeinCode,
  DateTimeOffset CreatedAt,
  IReadOnlyList<BikePhotoResponse> Photos)
{
  public static BikeResponse From(Bike bike) => new(
    bike.Id,
    bike.FrameNoRaw,
    bike.Brand,
    bike.Model,
    bike.Type,
    bike.ColorPrimary,
    bike.ColorSecondary,
    bike.IsEbike,
    bike.BatterySerial,
    bike.Features,
    bike.PurchaseDate,
    bike.Status,
    bike.FeinCodeHash is not null,
    bike.CreatedAt,
    bike.Photos.OrderBy(p => p.CreatedAt).Select(BikePhotoResponse.From).ToList());
}

public sealed record BikePhotoResponse(Guid Id, PhotoKind Kind, string Url, string ThumbnailUrl)
{
  public static BikePhotoResponse From(BikePhoto photo)
  {
    var url = $"/api/bikes/{photo.BikeId}/photos/{photo.Id}";
    return new BikePhotoResponse(photo.Id, photo.Kind, url, url + "?size=thumb");
  }
}
