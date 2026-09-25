using NetTopologySuite.Geometries;

namespace LeezenPass.Api.Domain.Theft;

public enum TheftReportStatus { Open, Recovered, Closed }

public enum LockType { UBolt, Chain, Folding, Cable, Frame, None, Other }

/// <summary>Created through <see cref="Bikes.Bike.ReportStolen"/>. The exact location stays owner-only; the public sees the district.</summary>
public class TheftReport
{
  public const int MaxLocationNoteLength = 200;
  public const int MaxPoliceCaseNoLength = 40;

  private TheftReport() { }

  internal TheftReport(Guid bikeId, DateTimeOffset stolenAt, double latitude, double longitude, LockType lockType, DateTimeOffset createdAt)
  {
    Id = Guid.CreateVersion7();
    BikeId = bikeId;
    StolenAt = stolenAt;
    // WGS84, SRID 4326. Note the order: x = longitude, y = latitude.
    Location = new Point(longitude, latitude) { SRID = 4326 };
    District = MuensterDistricts.ForPoint(latitude, longitude);
    LockType = lockType;
    CreatedAt = createdAt;
  }

  public Guid Id { get; private set; }
  public Guid BikeId { get; private set; }
  public DateTimeOffset StolenAt { get; private set; }
  public Point Location { get; private set; } = Point.Empty;

  /// <summary>Key from <see cref="MuensterDistricts"/>, derived from the location. The only location data made public.</summary>
  public string District { get; private set; } = MuensterDistricts.Outside;

  public string? LocationNote { get; private set; }
  public LockType LockType { get; private set; }
  public string? PoliceCaseNo { get; private set; }
  public TheftReportStatus Status { get; private set; } = TheftReportStatus.Open;
  public DateTimeOffset CreatedAt { get; private set; }

  public double Latitude => Location.Y;
  public double Longitude => Location.X;

  /// <summary>"Police case no." vs "self-reported" label on the public list.</summary>
  public bool IsPoliceReported => PoliceCaseNo is not null;

  /// <summary>Owners usually get the case number after filing with the police, so it can be added later.</summary>
  public void UpdateDetails(string? locationNote, string? policeCaseNo)
  {
    LocationNote = string.IsNullOrWhiteSpace(locationNote) ? null : locationNote.Trim();
    PoliceCaseNo = string.IsNullOrWhiteSpace(policeCaseNo) ? null : policeCaseNo.Trim();
  }

  internal void MarkRecovered() => Status = TheftReportStatus.Recovered;
}
