namespace LeezenPass.Api.Domain.Theft;

/// <summary>
/// Maps a point to the nearest part of Münster (approximate centres, nearest-centre lookup).
/// Good enough to say "stolen in Kreuzviertel" publicly without revealing the exact spot.
/// </summary>
public static class MuensterDistricts
{
  public const string Outside = "outside";

  /// <summary>Points further than this from every centre count as outside Münster.</summary>
  public const double MaxDistanceKm = 6;

  public sealed record District(string Key, double Latitude, double Longitude);

  public static readonly IReadOnlyList<District> All =
  [
    new("altstadt", 51.9625, 7.6261),
    new("kreuzviertel", 51.9700, 7.6130),
    new("mauritz", 51.9650, 7.6500),
    new("hansaviertel", 51.9530, 7.6420),
    new("suedviertel", 51.9480, 7.6300),
    new("geist", 51.9460, 7.6180),
    new("aaseestadt", 51.9500, 7.6000),
    new("sentrup", 51.9660, 7.5900),
    new("gievenbeck", 51.9700, 7.5650),
    new("mecklenbeck", 51.9320, 7.5850),
    new("roxel", 51.9530, 7.5300),
    new("albachten", 51.9200, 7.5300),
    new("nienberge", 52.0000, 7.5550),
    new("kinderhaus", 51.9950, 7.6100),
    new("coerde", 51.9930, 7.6500),
    new("sprakel", 52.0350, 7.6250),
    new("gelmer", 52.0050, 7.6950),
    new("handorf", 51.9880, 7.7100),
    new("gremmendorf", 51.9390, 7.6800),
    new("angelmodde", 51.9230, 7.6950),
    new("wolbeck", 51.9170, 7.7300),
    new("berg_fidel", 51.9230, 7.6220),
    new("hiltrup", 51.9020, 7.6380),
    new("amelsbueren", 51.8820, 7.6000),
  ];

  public static readonly IReadOnlySet<string> Keys = All.Select(d => d.Key).Append(Outside).ToHashSet();

  public static string ForPoint(double latitude, double longitude)
  {
    var nearest = All.MinBy(d => DistanceKm(latitude, longitude, d.Latitude, d.Longitude))!;
    return DistanceKm(latitude, longitude, nearest.Latitude, nearest.Longitude) <= MaxDistanceKm ? nearest.Key : Outside;
  }

  /// <summary>Haversine great-circle distance.</summary>
  public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
  {
    const double earthRadiusKm = 6371;
    static double Rad(double deg) => deg * Math.PI / 180;

    var dLat = Rad(lat2 - lat1);
    var dLon = Rad(lon2 - lon1);
    var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
      + (Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2));
    return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
  }
}
