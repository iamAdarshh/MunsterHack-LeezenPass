using NetTopologySuite.Geometries;

namespace LeezenPass.Api.Domain.Theft;

public enum TheftReportStatus { Open, Recovered, Closed }

public enum LockType { UBolt, Chain, Folding, Cable, Frame, None, Other }

public class TheftReport
{
  private TheftReport() { }

  /// <summary>Location is a WGS84 point, SRID 4326. Note the order: new Point(lon, lat).</summary>
  public TheftReport(Guid bikeId, DateTimeOffset stolenAt, Point location, LockType lockType, DateTimeOffset createdAt)
  {
    Id = Guid.CreateVersion7();
    BikeId = bikeId;
    StolenAt = stolenAt;
    Location = location;
    LockType = lockType;
    CreatedAt = createdAt;
  }

  public Guid Id { get; private set; }
  public Guid BikeId { get; private set; }
  public DateTimeOffset StolenAt { get; private set; }
  public Point Location { get; private set; } = Point.Empty;
  public string? LocationNote { get; set; }
  public LockType LockType { get; private set; }
  public string? PoliceCaseNo { get; set; }
  public TheftReportStatus Status { get; private set; } = TheftReportStatus.Open;
  public DateTimeOffset CreatedAt { get; private set; }
}
