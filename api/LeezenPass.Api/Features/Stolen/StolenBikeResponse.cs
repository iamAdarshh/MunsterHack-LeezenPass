using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Theft;
using LeezenPass.Api.Infrastructure.Data;

namespace LeezenPass.Api.Features.Stolen;

/// <summary>
/// Public view of a stolen bike. Privacy: no owner data, no frame number, no FEIN, no exact location or time,
/// no free-text note (may contain an address), and only side/detail photos (never receipt or frame-number close-up).
/// </summary>
public sealed record StolenBikeResponse(
  string Token,
  BikeType Type,
  string? Brand,
  string? Model,
  string? ColorPrimary,
  string? ColorSecondary,
  bool IsEbike,
  IReadOnlyList<string> Features,
  DateOnly StolenOn,
  string District,
  bool PoliceReported,
  IReadOnlyList<PublicPhotoResponse> Photos);

public sealed record PublicPhotoResponse(Guid Id, PhotoKind Kind, string Url, string ThumbnailUrl);

/// <summary>Flat projection row; mapped to <see cref="StolenBikeResponse"/> in memory.</summary>
public sealed record StolenBikeRow(
  string Token,
  BikeType Type,
  string? Brand,
  string? Model,
  string? ColorPrimary,
  string? ColorSecondary,
  bool IsEbike,
  List<string> Features,
  DateTimeOffset StolenAt,
  string District,
  bool PoliceReported,
  List<PublicPhotoRow> Photos)
{
  private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

  public StolenBikeResponse ToResponse() => new(
    Token, Type, Brand, Model, ColorPrimary, ColorSecondary, IsEbike, Features,
    DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(StolenAt, Berlin).DateTime),
    District,
    PoliceReported,
    Photos.Select(p =>
    {
      var url = $"/api/stolen/{Token}/photos/{p.Id}";
      return new PublicPhotoResponse(p.Id, p.Kind, url, url + "?size=thumb");
    }).ToList());
}

public sealed record PublicPhotoRow(Guid Id, PhotoKind Kind);

public sealed record StolenBikeFilter(
  string? Token = null,
  BikeType? Type = null,
  string? Color = null,
  string? District = null,
  IReadOnlyCollection<Guid>? BikeIds = null);

public static class StolenBikeQueries
{
  public static readonly PhotoKind[] PublicPhotoKinds = [PhotoKind.Side, PhotoKind.Detail];

  /// <summary>Currently stolen bikes with their open report, newest first. One SQL query; filters run in the database.</summary>
  public static IQueryable<StolenBikeRow> StolenBikes(this AppDbContext db, StolenBikeFilter filter)
  {
    var query = db.TheftReports
      .Where(t => t.Status == TheftReportStatus.Open)
      .Join(db.Bikes.Where(b => b.Status == BikeStatus.Stolen), t => t.BikeId, b => b.Id, (t, b) => new { t, b });

    if (filter.Token is not null)
    {
      query = query.Where(x => x.b.PublicToken == filter.Token);
    }

    if (filter.BikeIds is not null)
    {
      query = query.Where(x => filter.BikeIds.Contains(x.b.Id));
    }

    if (filter.Type is not null)
    {
      query = query.Where(x => x.b.Type == filter.Type);
    }

    if (filter.Color is not null)
    {
      query = query.Where(x => x.b.ColorPrimary == filter.Color || x.b.ColorSecondary == filter.Color);
    }

    if (filter.District is not null)
    {
      query = query.Where(x => x.t.District == filter.District);
    }

    return query
      .OrderByDescending(x => x.t.StolenAt)
      .Select(x => new StolenBikeRow(
        x.b.PublicToken,
        x.b.Type,
        x.b.Brand,
        x.b.Model,
        x.b.ColorPrimary,
        x.b.ColorSecondary,
        x.b.IsEbike,
        x.b.Features,
        x.t.StolenAt,
        x.t.District,
        x.t.PoliceCaseNo != null,
        x.b.Photos
          .Where(p => PublicPhotoKinds.Contains(p.Kind))
          .OrderBy(p => p.CreatedAt)
          .Select(p => new PublicPhotoRow(p.Id, p.Kind))
          .ToList()));
  }
}
