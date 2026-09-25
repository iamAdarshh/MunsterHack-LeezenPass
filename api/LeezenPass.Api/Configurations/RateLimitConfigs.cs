using System.Threading.RateLimiting;
using LeezenPass.Api.Infrastructure.Identity;

namespace LeezenPass.Api.Configurations;

public static class RateLimitPolicies
{
  /// <summary>Photo uploads: 30 per hour per user.</summary>
  public const string Upload = "upload";

  /// <summary>Register/update bike: 30 per hour per user. The 409 on a taken frame number must not become a free lookup.</summary>
  public const string BikeWrite = "bike-write";

  /// <summary>AI photo analysis: 30 per hour per user (each call keeps the model busy for seconds).</summary>
  public const string Ai = "ai";

  /// <summary>Claiming transfer codes: 10 per 10 minutes per user (guessing 8-char codes must be hopeless).</summary>
  public const string Claim = "claim";
}

/// <summary>Limits are in memory: restarting the API resets them (handy after demo rehearsals).</summary>
public static class RateLimitConfigs
{
  public static IServiceCollection AddRateLimitConfigs(this IServiceCollection services)
  {
    services.AddRateLimiter(o =>
    {
      o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
      o.AddPolicy(RateLimitPolicies.Upload, ctx => PerUser(ctx, 30, TimeSpan.FromHours(1)));
      o.AddPolicy(RateLimitPolicies.BikeWrite, ctx => PerUser(ctx, 30, TimeSpan.FromHours(1)));
      o.AddPolicy(RateLimitPolicies.Ai, ctx => PerUser(ctx, 30, TimeSpan.FromHours(1)));
      o.AddPolicy(RateLimitPolicies.Claim, ctx => PerUser(ctx, 10, TimeSpan.FromMinutes(10)));
    });

    return services;
  }

  private static RateLimitPartition<string> PerUser(HttpContext ctx, int permits, TimeSpan window) =>
    RateLimitPartition.GetFixedWindowLimiter(
      ctx.User.Identity?.IsAuthenticated == true ? ctx.User.GetUserId().ToString() : "anonymous",
      _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window });
}
