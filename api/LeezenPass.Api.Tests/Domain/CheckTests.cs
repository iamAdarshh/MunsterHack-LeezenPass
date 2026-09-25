using System.Text;
using LeezenPass.Api.Domain.Lookups;

namespace LeezenPass.Api.Tests.Domain;

public class CheckTests
{
  private static readonly byte[] Secret = Encoding.UTF8.GetBytes("test-secret-test-secret-test-secret-!");

  private static CheckCandidate Bike(bool stolen = false, bool transfer = false) => new(Guid.NewGuid(), stolen, transfer);

  [Fact]
  public void Exact_stolen_match_is_stolen()
  {
    Assert.Equal(LookupResult.Stolen, CheckRules.Decide([Bike(stolen: true)], looseStolenMatch: false));
  }

  [Fact]
  public void Stolen_wins_over_open_transfer()
  {
    // FEIN codes are per household: several bikes can share one.
    Assert.Equal(LookupResult.Stolen, CheckRules.Decide([Bike(transfer: true), Bike(stolen: true)], false));
  }

  [Fact]
  public void Open_transfer_is_verified_transfer()
  {
    Assert.Equal(LookupResult.VerifiedTransfer, CheckRules.Decide([Bike(transfer: true)], false));
  }

  [Fact]
  public void Registered_clean_bike_is_reported_as_unknown()
  {
    Assert.Equal(LookupResult.Unknown, CheckRules.Decide([Bike()], looseStolenMatch: false));
  }

  [Fact]
  public void Clean_exact_match_does_not_hide_a_loose_stolen_match()
  {
    // Otherwise the answer would reveal which variants of a stolen frame number are registered.
    Assert.Equal(LookupResult.PossibleMatch, CheckRules.Decide([Bike()], looseStolenMatch: true));
  }

  [Fact]
  public void Exact_stolen_or_transfer_beats_a_loose_match()
  {
    Assert.Equal(LookupResult.Stolen, CheckRules.Decide([Bike(stolen: true)], looseStolenMatch: true));
    Assert.Equal(LookupResult.VerifiedTransfer, CheckRules.Decide([Bike(transfer: true)], looseStolenMatch: true));
  }

  [Fact]
  public void Loose_match_on_a_stolen_bike_is_possible_match()
  {
    Assert.Equal(LookupResult.PossibleMatch, CheckRules.Decide([], looseStolenMatch: true));
  }

  [Fact]
  public void Nothing_found_is_unknown()
  {
    Assert.Equal(LookupResult.Unknown, CheckRules.Decide([], looseStolenMatch: false));
  }

  [Fact]
  public void Ip_hash_changes_daily_and_hides_the_ip()
  {
    var day = new DateOnly(2026, 9, 26);
    var today = LookupHashing.IpHash("192.168.1.23", day, Secret);

    Assert.Matches("^[0-9a-f]{64}$", today);
    Assert.DoesNotContain("192", today);
    Assert.Equal(today, LookupHashing.IpHash("192.168.1.23", day, Secret));
    Assert.NotEqual(today, LookupHashing.IpHash("192.168.1.23", day.AddDays(1), Secret));
    Assert.NotEqual(today, LookupHashing.IpHash("192.168.1.24", day, Secret));
  }

  [Fact]
  public void Query_hash_is_keyed_and_separates_kinds()
  {
    var frame = LookupHashing.QueryHash("frame", "WGZ12345678", Secret);

    Assert.NotEqual(frame, LookupHashing.QueryHash("fein", "WGZ12345678", Secret));
    Assert.NotEqual(frame, LookupHashing.QueryHash("frame", "WGZ12345678", "another-secret-another-secret-12345"u8));
    Assert.NotEqual(
      Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData("frame:WGZ12345678"u8)),
      frame);
  }

  [Theory]
  [InlineData("203.0.113.7", "203.0.113.7")]
  [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
  [InlineData("2001:db8:abcd:12:1111:2222:3333:4444", "2001:db8:abcd:12::/64")]
  [InlineData("2001:db8:abcd:12:ffff:ffff:ffff:ffff", "2001:db8:abcd:12::/64")]
  public void Client_key_groups_ipv6_by_64_prefix(string address, string expected)
  {
    Assert.Equal(expected, LeezenPass.Api.Infrastructure.ClientKey.For(System.Net.IPAddress.Parse(address)));
  }
}
