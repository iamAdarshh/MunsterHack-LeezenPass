using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Theft;

namespace LeezenPass.Api.Tests.Domain;

public class TheftTests
{
  private static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

  private static Bike NewBike() => new(Guid.NewGuid(), FrameNumber.Create("WGZ12345678"), Now);

  [Fact]
  public void Reporting_stolen_opens_a_report_and_marks_the_bike()
  {
    var bike = NewBike();

    var report = bike.ReportStolen(Now.AddHours(-2), 51.9625, 7.6261, LockType.Cable, Now);

    Assert.Equal(BikeStatus.Stolen, bike.Status);
    Assert.Same(report, bike.OpenTheftReport);
    Assert.Equal(TheftReportStatus.Open, report.Status);
    Assert.Equal("altstadt", report.District);
    Assert.Equal((51.9625, 7.6261), (report.Latitude, report.Longitude));
    Assert.Equal(4326, report.Location.SRID);
  }

  [Fact]
  public void Cannot_report_a_stolen_bike_twice()
  {
    var bike = NewBike();
    bike.ReportStolen(Now, 51.96, 7.62, LockType.Chain, Now);

    Assert.False(bike.CanReportStolen);
    Assert.Throws<InvalidOperationException>(() => bike.ReportStolen(Now, 51.96, 7.62, LockType.Chain, Now));
  }

  [Fact]
  public void Recovery_closes_the_report_and_allows_a_new_one()
  {
    var bike = NewBike();
    var first = bike.ReportStolen(Now, 51.96, 7.62, LockType.Chain, Now);

    bike.MarkRecovered();

    Assert.Equal(BikeStatus.Recovered, bike.Status);
    Assert.Equal(TheftReportStatus.Recovered, first.Status);
    Assert.Null(bike.OpenTheftReport);

    var second = bike.ReportStolen(Now.AddDays(30), 51.96, 7.62, LockType.UBolt, Now.AddDays(30));
    Assert.Same(second, bike.OpenTheftReport);
  }

  [Fact]
  public void Only_stolen_bikes_can_be_recovered()
  {
    Assert.Throws<InvalidOperationException>(() => NewBike().MarkRecovered());
  }

  [Fact]
  public void Police_case_number_marks_report_as_police_reported()
  {
    var report = NewBike().ReportStolen(Now, 51.96, 7.62, LockType.Chain, Now);
    Assert.False(report.IsPoliceReported);

    report.UpdateDetails("  am Hauptbahnhof ", " 123/26 ");

    Assert.True(report.IsPoliceReported);
    Assert.Equal(("am Hauptbahnhof", "123/26"), (report.LocationNote, report.PoliceCaseNo));

    report.UpdateDetails(null, "  ");
    Assert.False(report.IsPoliceReported);
  }

  [Theory]
  [InlineData(51.9625, 7.6261, "altstadt")]     // Prinzipalmarkt
  [InlineData(51.9021, 7.6386, "hiltrup")]      // Hiltrup centre
  [InlineData(51.9510, 7.6010, "aaseestadt")]   // Aasee
  [InlineData(51.9170, 7.7310, "wolbeck")]
  [InlineData(52.5200, 13.4050, "outside")]     // Berlin
  [InlineData(51.9607, 7.3000, "outside")]      // ~15 km west of Roxel
  public void Maps_points_to_the_nearest_district(double lat, double lon, string expected)
  {
    Assert.Equal(expected, MuensterDistricts.ForPoint(lat, lon));
  }

  [Fact]
  public void Haversine_distance_is_in_kilometres()
  {
    // Münster Hbf -> Prinzipalmarkt is roughly 0.8 km.
    var km = MuensterDistricts.DistanceKm(51.9567, 7.6353, 51.9625, 7.6261);
    Assert.InRange(km, 0.7, 1.0);
  }
}
