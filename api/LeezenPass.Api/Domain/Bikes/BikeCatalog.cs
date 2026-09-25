namespace LeezenPass.Api.Domain.Bikes;

/// <summary>
/// Fixed vocabularies for colours and features. Stored as keys (not German/English text) so the UI
/// can translate them and sightings can be matched against them later.
/// </summary>
public static class BikeCatalog
{
  public static readonly IReadOnlySet<string> Colors = new HashSet<string>(StringComparer.Ordinal)
  {
    "black", "white", "grey", "silver", "red", "blue", "green", "yellow",
    "orange", "brown", "beige", "pink", "purple", "other",
  };

  public static readonly IReadOnlySet<string> Features = new HashSet<string>(StringComparer.Ordinal)
  {
    "rack", "fenders", "hub_dynamo", "lights", "kickstand", "basket", "child_seat",
    "frame_lock", "suspension_fork", "disc_brakes", "coaster_brake", "bell",
  };

  public const int MaxPhotosPerBike = 12;
}
