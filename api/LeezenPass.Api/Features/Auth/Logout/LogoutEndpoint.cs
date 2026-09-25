using FastEndpoints;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace LeezenPass.Api.Features.Auth.Logout;

/// <summary>MapIdentityApi has no logout, so this clears the auth cookie.</summary>
public class LogoutEndpoint(SignInManager<AppUser> signInManager) : EndpointWithoutRequest
{
  public override void Configure()
  {
    Post("auth/logout");
    Summary(s => s.Summary = "Sign out (clears the auth cookie)");
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    await signInManager.SignOutAsync();
    await Send.NoContentAsync(ct);
  }
}
