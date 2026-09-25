using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Vision;

namespace LeezenPass.Api.Tests.Infrastructure;

public class VisionResponseMapperTests
{
  private static VisionModelOutput Output(
    string? type = "city", string? primary = "black", string? secondary = "none", string? brand = "",
    List<string>? features = null, string? frame = "", double? confidence = 0.8) =>
    new(type, primary, secondary, brand, features ?? [], frame, confidence);

  [Fact]
  public void Maps_a_valid_answer()
  {
    var s = VisionResponseMapper.Map(Output(
      type: "trekking", primary: "red", secondary: "white", brand: " Gazelle ",
      features: ["rack", "fenders"], frame: "WGZ 4711 0815 K", confidence: 0.9));

    Assert.Equal(BikeType.Trekking, s.Type);
    Assert.Equal(("red", "white"), (s.ColorPrimary, s.ColorSecondary));
    Assert.Equal("Gazelle", s.BrandGuess);
    Assert.Equal(["rack", "fenders"], s.Features);
    Assert.Equal("WGZ 4711 0815 K", s.FrameNumberCandidate);
    Assert.Equal(0.9, s.Confidence);
  }

  [Fact]
  public void Empty_strings_and_none_become_null()
  {
    var s = VisionResponseMapper.Map(Output(brand: "  ", frame: "", secondary: "none"));

    Assert.Null(s.BrandGuess);
    Assert.Null(s.FrameNumberCandidate);
    Assert.Null(s.ColorSecondary);
  }

  [Fact]
  public void Unknown_keys_are_dropped_not_passed_on()
  {
    var s = VisionResponseMapper.Map(Output(type: "tandem", primary: "teal", features: ["rack", "jetpack", "RACK"]));

    Assert.Null(s.Type);
    Assert.Null(s.ColorPrimary);
    Assert.Equal(["rack"], s.Features);
  }

  [Fact]
  public void Secondary_colour_equal_to_primary_is_dropped()
  {
    Assert.Null(VisionResponseMapper.Map(Output(primary: "blue", secondary: "blue")).ColorSecondary);
  }

  [Theory]
  [InlineData("WGZ 4711 0815 K", "WGZ 4711 0815 K")]
  [InlineData(" AB1234 ", "AB1234")]
  [InlineData("a1", null)]          // too short
  [InlineData("----", null)]        // no letters/digits
  [InlineData("unreadable", null)]  // no digits: the model's way of saying "can't read it"
  [InlineData("UNKNOWN", null)]
  public void Frame_number_must_be_plausible(string frame, string? expected)
  {
    Assert.Equal(expected, VisionResponseMapper.Map(Output(frame: frame)).FrameNumberCandidate);
  }

  [Theory]
  [InlineData(1.7, 1)]
  [InlineData(-0.2, 0)]
  [InlineData(null, 0)]
  [InlineData(double.NaN, 0)]
  public void Confidence_is_clamped(double? raw, double expected)
  {
    Assert.Equal(expected, VisionResponseMapper.Map(Output(confidence: raw)).Confidence);
  }

  [Fact]
  public void Schema_enums_are_exactly_the_catalog_keys()
  {
    var schema = VisionPrompt.Schema();
    var types = schema["properties"]!["type"]!["enum"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
    var features = schema["properties"]!["features"]!["items"]!["enum"]!.AsArray().Select(n => n!.GetValue<string>());

    Assert.Contains("city", types);
    Assert.Equal(Enum.GetValues<BikeType>().Length, types.Count);
    Assert.Equal(BikeCatalog.Features.Order(), features);
  }
}
