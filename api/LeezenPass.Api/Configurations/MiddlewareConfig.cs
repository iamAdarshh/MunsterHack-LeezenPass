using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace LeezenPass.Api.Configurations;

public static class MiddlewareConfig
{
  public static async Task<WebApplication> UseAppMiddlewareAndMigrateDatabase(this WebApplication app)
  {
    // Generic message only, in every environment (the demo runs as Development): details go to the log.
    app.UseDefaultExceptionHandler(useGenericReason: true);
    if (!app.Environment.IsDevelopment())
    {
      app.UseHsts();
    }

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

    await MigrateDatabase(app);

    return app;
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
