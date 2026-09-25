using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Tests.Domain;

public class FrameNumberTests
{
  [Theory]
  [InlineData("wgz 1234-5678", "WGZ12345678")]
  [InlineData("  ab.cd/12 34 ", "ABCD1234")]
  [InlineData("WGZ12345678", "WGZ12345678")]
  public void Normalizes_to_uppercase_ascii_letters_and_digits(string raw, string expected)
  {
    var frameNumber = FrameNumber.Create(raw);

    Assert.Equal(expected, frameNumber.Normalized);
  }

  [Fact]
  public void Keeps_trimmed_raw_input()
  {
    Assert.Equal("wgz 1234-5678", FrameNumber.Create("  wgz 1234-5678 ").Raw);
  }

  [Fact]
  public void Drops_non_ascii_letters()
  {
    Assert.Equal("AB1234", FrameNumber.Create("Äab1234ß").Normalized);
  }

  [Theory]
  [InlineData("OILSBZ", "011582")]
  [InlineData("WGZ12345678", "WG212345678")]
  [InlineData("ACDE7", "ACDE7")]
  public void Loose_key_maps_confusable_letters_to_digits(string normalized, string expected)
  {
    Assert.Equal(expected, FrameNumber.ToLoose(normalized));
  }

  [Fact]
  public void Typos_between_O_and_0_share_the_loose_key()
  {
    var typed = FrameNumber.Create("SO12 3B");
    var printed = FrameNumber.Create("50123 8");

    Assert.NotEqual(typed.Normalized, printed.Normalized);
    Assert.Equal(typed.Loose, printed.Loose);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("a-b-c")]
  [InlineData("----")]
  [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567")]
  public void Rejects_too_short_too_long_or_empty(string? raw)
  {
    Assert.False(FrameNumber.TryCreate(raw, out _));
  }

  [Fact]
  public void Equality_uses_normalized_key()
  {
    Assert.Equal(FrameNumber.Create("ab-12 34"), FrameNumber.Create("AB1234"));
  }
}
