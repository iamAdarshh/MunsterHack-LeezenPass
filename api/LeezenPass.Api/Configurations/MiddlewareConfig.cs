using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace LeezenPass.Api.Configurations;

public static class MiddlewareConfig
{
  public static async Task<WebApplication> UseAppMiddlewareAndMigrateDatabase(this WebApplication app)
  {
    // Real client IP for rate limits and the hashed lookup log. Only trusted from loopback
    // (the Vite dev proxy / a reverse proxy on the same machine); default KnownProxies = loopback.
    app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor });

    // Generic message only, in every environment (the demo runs as Development): details go to the log.
    app.UseDefaultExceptionHandler(useGenericReason: true);
    if (!app.Environment.IsDevelopment())
    {
      app.UseHsts();
    }

    // Two requests changed the same row (e.g. claim vs. new code): tell the client, don't 500.
    app.Use(async (ctx, next) =>
    {
      try
      {
        await next(ctx);
      }
      catch (DbUpdateConcurrencyException) when (!ctx.Response.HasStarted)
      {
        ctx.Response.StatusCode = StatusCodes.Status409Conflict;
        await ctx.Response.WriteAsJsonAsync(new
        {
          status = StatusCodes.Status409Conflict,
          title = "Conflict",
          errors = new[] { new { name = "generalErrors", reason = "errors.concurrentChange" } },
        });
      }
    });

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.UseFastEndpoints(c =>
    {
      c.Endpoints.RoutePrefix = "api";
      // Enums as snake_case strings, matching SPEC ("frame_no", "verified_transfer", ...).
      c.Serializer.Options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
      c.Errors.UseProblemDetails();
    });

    // register, login (?useCookies=true), manage/info, ... Logout is Features/Auth/Logout.
    app.MapGroup("/api/auth").MapIdentityApi<AppUser>();

    if (app.Environment.IsDevelopment())
    {
      app.UseSwaggerGen(options => options.Path = "/openapi/{documentName}.json");
      app.MapScalarApiReference();
    }

    WarnIfPublicUrlIsLocal(app);
    await MigrateDatabase(app);

    return app;
  }

  /// <summary>QR codes on the pass must open on a phone; localhost won't.</summary>
  private static void WarnIfPublicUrlIsLocal(WebApplication app)
  {
    var url = app.Configuration[$"{AppOptions.Section}:{nameof(AppOptions.PublicBaseUrl)}"] ?? string.Empty;
    if (url.Contains("localhost", StringComparison.OrdinalIgnoreCase) || url.Contains("127.0.0.1", StringComparison.Ordinal))
    {
      app.Logger.LogWarning(
        "App:PublicBaseUrl is {Url}: QR codes on the bike pass won't open on a phone. For demos set App__PublicBaseUrl to the LAN/tunnel URL.",
        url);
    }
  }

  private static async Task MigrateDatabase(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    logger.LogInformation("Applying database migrations...");
    await context.Database.MigrateAsync();
  }
}
