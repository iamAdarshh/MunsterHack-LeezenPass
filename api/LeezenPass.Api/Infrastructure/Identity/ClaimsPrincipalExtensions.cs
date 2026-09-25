using System.Security.Claims;

namespace LeezenPass.Api.Infrastructure.Identity;

public static class ClaimsPrincipalExtensions
{
  /// <summary>Id of the signed-in user. Only call on endpoints that require auth.</summary>
  public static Guid GetUserId(this ClaimsPrincipal user) =>
    Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
      ?? throw new InvalidOperationException("No authenticated user."));
}
