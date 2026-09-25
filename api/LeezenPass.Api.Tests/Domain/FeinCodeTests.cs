using System.Text;
using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Tests.Domain;

public class FeinCodeTests
{
  private static readonly byte[] Secret = Encoding.UTF8.GetBytes("test-secret-test-secret-test-secret-!");
  private static readonly byte[] OtherSecret = Encoding.UTF8.GetBytes("another-secret-another-secret-another");

  [Fact]
  public void Hash_is_64_char_lowercase_hex()
  {
    var hash = Create("MS 123 456 789 ABC").ComputeHash(Secret);

    Assert.Matches("^[0-9a-f]{64}$", hash);
  }

  [Fact]
  public void Same_code_in_different_formatting_gives_same_hash()
  {
    Assert.Equal(
      Create("MS 123 456 789 ABC").ComputeHash(Secret),
      Create("ms-123456789-abc").ComputeHash(Secret));
  }

  [Fact]
  public void Hash_depends_on_secret()
  {
    var code = Create("MS123456789ABC");

    Assert.NotEqual(code.ComputeHash(Secret), code.ComputeHash(OtherSecret));
  }

  [Fact]
  public void Hash_matches_hmac_sha256_of_normalized_code()
  {
    var expected = Convert.ToHexStringLower(
      System.Security.Cryptography.HMACSHA256.HashData(Secret, Encoding.UTF8.GetBytes("MS123456789ABC")));

    Assert.Equal(expected, Create("ms 123 456 789 abc").ComputeHash(Secret));
  }

  [Fact]
  public void Rejects_short_secret()
  {
    Assert.Throws<ArgumentException>(() => Create("MS123456789ABC").ComputeHash("short"u8));
  }

  [Fact]
  public void ToString_never_reveals_the_code()
  {
    Assert.DoesNotContain("123456", Create("MS123456789ABC").ToString());
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("MS 12")]
  public void Rejects_invalid_input(string? raw)
  {
    Assert.False(FeinCode.TryCreate(raw, out _));
  }

  private static FeinCode Create(string raw)
  {
    Assert.True(FeinCode.TryCreate(raw, out var code));
    return code;
  }
}
