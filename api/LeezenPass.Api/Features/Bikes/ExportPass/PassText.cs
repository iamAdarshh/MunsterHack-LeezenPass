using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Features.Bikes.ExportPass;

/// <summary>German labels (the pass is for German police/insurers) for the catalog keys. Mirrors web/src/i18n/de.json.</summary>
public static class PassText
{
  public static string Type(BikeType type) => type switch
  {
    BikeType.City => "Stadtrad",
    BikeType.Trekking => "Trekkingrad",
    BikeType.Mountain => "Mountainbike",
    BikeType.Road => "Rennrad",
    BikeType.Gravel => "Gravelbike",
    BikeType.Cargo => "Lastenrad",
    BikeType.Folding => "Faltrad",
    BikeType.Kids => "Kinderrad",
    _ => "Sonstiges",
  };

  public static string Status(BikeStatus status) => status switch
  {
    BikeStatus.Stolen => "GESTOHLEN GEMELDET",
    BikeStatus.Recovered => "Wiedergefunden",
    _ => "Registriert",
  };

  private static readonly Dictionary<string, string> ColorNames = new()
  {
    ["black"] = "Schwarz",
    ["white"] = "Weiß",
    ["grey"] = "Grau",
    ["silver"] = "Silber",
    ["red"] = "Rot",
    ["blue"] = "Blau",
    ["green"] = "Grün",
    ["yellow"] = "Gelb",
    ["orange"] = "Orange",
    ["brown"] = "Braun",
    ["beige"] = "Beige",
    ["pink"] = "Rosa",
    ["purple"] = "Lila",
    ["other"] = "Andere",
  };

  private static readonly Dictionary<string, string> FeatureNames = new()
  {
    ["rack"] = "Gepäckträger",
    ["fenders"] = "Schutzbleche",
    ["hub_dynamo"] = "Nabendynamo",
    ["lights"] = "Licht",
    ["kickstand"] = "Ständer",
    ["basket"] = "Korb",
    ["child_seat"] = "Kindersitz",
    ["frame_lock"] = "Rahmenschloss",
    ["suspension_fork"] = "Federgabel",
    ["disc_brakes"] = "Scheibenbremsen",
    ["coaster_brake"] = "Rücktrittbremse",
    ["bell"] = "Klingel",
  };

  public static string Colors(string? primary, string? secondary) =>
    string.Join(" / ", new[] { primary, secondary }.OfType<string>().Select(c => ColorNames.GetValueOrDefault(c, c)))
      is { Length: > 0 } text ? text : "–";

  public static string Features(IEnumerable<string> features) =>
    string.Join(", ", features.Select(f => FeatureNames.GetValueOrDefault(f, f))) is { Length: > 0 } text ? text : "–";
}
