using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace LeezenPass.Api.Configurations;

public static class AuthConfigs
{
  public static IServiceCollection AddAuthConfigs(this IServiceCollection services, IWebHostEnvironment env)
  {
    services.AddAuthorization();

    services.AddIdentityApiEndpoints<AppUser>(o =>
            {
              o.User.RequireUniqueEmail = true;
              o.Password.RequiredLength = 8;
              // Upper, lower and digit are still required; symbols are a pain on a phone keyboard.
              o.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

    services.ConfigureApplicationCookie(o =>
    {
      o.Cookie.Name = "leezenpass.auth";
      o.Cookie.HttpOnly = true;
      o.Cookie.SameSite = SameSiteMode.Lax;
      // Dev runs over plain http (Vite proxy, phone on LAN); everywhere else cookies are https-only.
      o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
      o.ExpireTimeSpan = TimeSpan.FromDays(14);
      o.SlidingExpiration = true;

      // API: answer with status codes instead of redirecting to a login page.
      o.Events.OnRedirectToLogin = ctx =>
      {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
      };
      o.Events.OnRedirectToAccessDenied = ctx =>
      {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
      };
    });

    return services;
  }
}
