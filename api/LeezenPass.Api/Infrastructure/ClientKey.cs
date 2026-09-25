using System.Net;
using System.Net.Sockets;

namespace LeezenPass.Api.Infrastructure;

/// <summary>
/// Client identity for per-IP rate limits and the hashed lookup log. IPv6 is keyed on its /64 prefix:
/// one device can pick new addresses inside its /64 at will, which would otherwise mean a fresh limit each time.
/// </summary>
public static class ClientKey
{
  public static string For(IPAddress? address)
  {
    if (address is null)
    {
      return "unknown";
    }

    if (address.IsIPv4MappedToIPv6)
    {
      address = address.MapToIPv4();
    }

    if (address.AddressFamily != AddressFamily.InterNetworkV6)
    {
      return address.ToString();
    }

    var bytes = address.GetAddressBytes();
    Array.Clear(bytes, 8, 8);
    return new IPAddress(bytes) + "/64";
  }
}
