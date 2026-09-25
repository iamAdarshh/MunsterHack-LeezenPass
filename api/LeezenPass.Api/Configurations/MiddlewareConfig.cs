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
    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
    }
    else
    {
      // Generic message only: never leak exception details to clients.
      app.UseDefaultExceptionHandler(useGenericReason: true);
      app.UseHsts();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseFastEndpoints(c =>
    {
      c.Endpoints.RoutePrefix = "api";
      c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
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
