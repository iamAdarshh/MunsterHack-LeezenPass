using LeezenPass.Api.Domain.Transfers;
using LeezenPass.Api.Infrastructure.Time;

namespace LeezenPass.Api.Tests.Domain;

public class TransferCodeTests
{
  [Fact]
  public void Alphabet_has_no_look_alike_characters()
  {
    foreach (var c in "01ILO")
    {
      Assert.DoesNotContain(c, TransferCode.Alphabet);
    }
  }

  [Fact]
  public void Generated_codes_have_8_chars_from_the_alphabet()
  {
    for (var i = 0; i < 500; i++)
    {
      var code = TransferCode.Generate().Value;

      Assert.Equal(TransferCode.Length, code.Length);
      Assert.All(code, c => Assert.Contains(c, TransferCode.Alphabet));
    }
  }

  [Fact]
  public void Generated_codes_are_random()
  {
    var codes = Enumerable.Range(0, 1000).Select(_ => TransferCode.Generate().Value).ToHashSet();

    Assert.Equal(1000, codes.Count);
  }

  [Theory]
  [InlineData("abcd-efgh", "ABCDEFGH")]
  [InlineData("ABCD EFGH", "ABCDEFGH")]
  [InlineData("23456789", "23456789")]
  public void Parses_user_input(string input, string expected)
  {
    Assert.True(TransferCode.TryParse(input, out var code));
    Assert.Equal(expected, code.Value);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("ABCDEFG")]
  [InlineData("ABCDEFGHJ")]
  [InlineData("ABCDEFG0")]
  [InlineData("ABCDEFGI")]
  public void Rejects_wrong_length_or_characters(string? input)
  {
    Assert.False(TransferCode.TryParse(input, out _));
  }

  [Fact]
  public void Hash_is_stable_sha256_hex()
  {
    Assert.True(TransferCode.TryParse("ABCD-EFGH", out var a));
    Assert.True(TransferCode.TryParse("abcdefgh", out var b));

    Assert.Matches("^[0-9a-f]{64}$", a.ComputeHash());
    Assert.Equal(a.ComputeHash(), b.ComputeHash());
  }

  [Fact]
  public void ToString_never_reveals_the_code()
  {
    var code = TransferCode.Generate();

    Assert.DoesNotContain(code.Value, code.ToString());
  }

  [Fact]
  public void Transfer_stores_hash_not_code()
  {
    var code = TransferCode.Generate();
    var transfer = new OwnershipTransfer(Guid.NewGuid(), Guid.NewGuid(), code, DateTimeOffset.UtcNow);

    Assert.Equal(code.ComputeHash(), transfer.CodeHash);
    Assert.NotEqual(code.Value, transfer.CodeHash);
  }

  [Fact]
  public void Transfer_is_open_for_48_hours_then_expires()
  {
    var clock = new FakeClock(new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero));
    var transfer = new OwnershipTransfer(Guid.NewGuid(), Guid.NewGuid(), TransferCode.Generate(), clock.UtcNow);

    Assert.Equal(clock.UtcNow.AddHours(48), transfer.ExpiresAt);
    Assert.True(transfer.IsOpenAt(clock.UtcNow));

    clock.Advance(TimeSpan.FromHours(48) - TimeSpan.FromSeconds(1));
    Assert.True(transfer.IsOpenAt(clock.UtcNow));

    clock.Advance(TimeSpan.FromSeconds(1));
    Assert.False(transfer.IsOpenAt(clock.UtcNow));
  }
}
