using FastEndpoints;
using FastEndpoints.Swagger;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Infrastructure.Seed;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.AddLoggerConfigs();

using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
var startupLogger = loggerFactory.CreateLogger<Program>();

builder.Services.AddOptionConfigs(builder.Configuration);
builder.Services.AddServiceConfigs(startupLogger, builder);

builder.Services.AddFastEndpoints()
                .SwaggerDocument(o => o.ShortSchemaNames = true);

var app = builder.Build();

// `dotnet run --project api/LeezenPass.Api -- seed`: migrate, replace the demo data, exit (see seed/README.md).
if (args.Contains("seed"))
{
  await app.SeedDemoDataAsync();
  return;
}

await app.UseAppMiddlewareAndMigrateDatabase();

app.Run();

// Make the implicit Program class public so tests can reference the assembly.
public partial class Program { }
