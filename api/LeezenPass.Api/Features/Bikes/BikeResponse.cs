using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Theft;

namespace LeezenPass.Api.Features.Bikes;

/// <summary>Owner view of a bike. Never returned by public endpoints. The FEIN code itself is never returned.</summary>
public sealed record BikeResponse(
  Guid Id,
  string PublicToken,
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
  IReadOnlyList<BikePhotoResponse> Photos,
  TheftResponse? Theft)
{
  /// <summary>Needs Photos and the open theft report loaded (see <see cref="BikeQueries.WithDetails"/>).</summary>
  public static BikeResponse From(Bike bike) => new(
    bike.Id,
    bike.PublicToken,
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
    bike.Photos.OrderBy(p => p.CreatedAt).Select(BikePhotoResponse.From).ToList(),
    bike.OpenTheftReport is { } report ? TheftResponse.From(report) : null);
}

public sealed record BikePhotoResponse(Guid Id, PhotoKind Kind, string Url, string ThumbnailUrl)
{
  public static BikePhotoResponse From(BikePhoto photo)
  {
    var url = $"/api/bikes/{photo.BikeId}/photos/{photo.Id}";
    return new BikePhotoResponse(photo.Id, photo.Kind, url, url + "?size=thumb");
  }
}

/// <summary>Owner view of the open theft report, including the exact location.</summary>
public sealed record TheftResponse(
  Guid Id,
  DateTimeOffset StolenAt,
  double Latitude,
  double Longitude,
  string District,
  string? LocationNote,
  LockType LockType,
  string? PoliceCaseNo,
  DateTimeOffset CreatedAt)
{
  public static TheftResponse From(TheftReport r) =>
    new(r.Id, r.StolenAt, r.Latitude, r.Longitude, r.District, r.LocationNote, r.LockType, r.PoliceCaseNo, r.CreatedAt);
}
