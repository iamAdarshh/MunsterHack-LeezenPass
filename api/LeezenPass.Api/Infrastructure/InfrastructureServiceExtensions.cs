using LeezenPass.Api.Configurations;
using LeezenPass.Api.Infrastructure.Captcha;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Email;
using LeezenPass.Api.Infrastructure.Images;
using LeezenPass.Api.Infrastructure.Pdf;
using LeezenPass.Api.Infrastructure.Storage;
using LeezenPass.Api.Infrastructure.Time;
using LeezenPass.Api.Infrastructure.Vision;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration config,
    ILogger logger)
  {
    var connectionString = config.GetConnectionString("AppDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
      throw new InvalidOperationException("ConnectionStrings:AppDb is required. See infra/.env.example.");
    }

    services.AddDbContext<AppDbContext>(options => options
      .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
      .UseSnakeCaseNamingConvention());

    services.AddSingleton<IClock, SystemClock>();
    services.AddSingleton<IPdfRenderer, QuestPdfRenderer>();
    services.AddSingleton<ImageProcessor>();

    // Local disk until a MinIO implementation is needed.
    services.AddSingleton<IFileStorage, LocalFileStorage>();

    // Only a fake so far: the vision provider is decided at the event.
    services.AddSingleton<IVisionExtractor, FakeVisionExtractor>();

    var useFakes = config.GetSection(FeaturesOptions.Section).Get<FeaturesOptions>()?.UseFakes ?? false;
    if (useFakes)
    {
      services.AddSingleton<IEmailSender, FakeEmailSender>();
      services.AddSingleton<ICaptchaVerifier, FakeCaptchaVerifier>();
    }
    else
    {
      services.AddScoped<IEmailSender, SmtpEmailSender>();
      services.AddHttpClient<ICaptchaVerifier, TurnstileCaptchaVerifier>(c => c.Timeout = TimeSpan.FromSeconds(5));
    }

    logger.LogInformation("Infrastructure registered (UseFakes={UseFakes})", useFakes);

    return services;
  }
}
