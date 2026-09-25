using Microsoft.AspNetCore.Identity;

namespace LeezenPass.Api.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
  public AppUser() => Id = Guid.CreateVersion7();

  public string? DisplayName { get; set; }
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
