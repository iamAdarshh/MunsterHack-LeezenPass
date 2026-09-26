using LeezenPass.Api.Configurations;
using LeezenPass.Api.Infrastructure.Captcha;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Email;
using LeezenPass.Api.Infrastructure.Images;
using LeezenPass.Api.Infrastructure.Pdf;
using LeezenPass.Api.Infrastructure.Seed;
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
    services.AddScoped<DemoSeeder>();

    // Local disk until a MinIO implementation is needed.
    services.AddSingleton<IFileStorage, LocalFileStorage>();

    var useFakes = config.GetSection(FeaturesOptions.Section).Get<FeaturesOptions>()?.UseFakes ?? false;

    // Vision can be real even with UseFakes: a local model (LM Studio) works offline too.
    var vision = config.GetSection(VisionOptions.Section).Get<VisionOptions>() ?? new VisionOptions();
    var realVision = vision.Provider == VisionProvider.OpenAiCompatible || (vision.Provider == VisionProvider.Auto && !useFakes);
    if (realVision)
    {
      services.AddHttpClient<IVisionExtractor, OpenAiCompatibleVisionExtractor>();
      services.AddHostedService<VisionWarmupService>();
    }
    else
    {
      services.AddSingleton<IVisionExtractor, FakeVisionExtractor>();
    }
    if (useFakes)
    {
      services.AddSingleton<IEmailSender, FakeEmailSender>();
    }
    else
    {
      services.AddScoped<IEmailSender, SmtpEmailSender>();
    }

    // Captcha has its own switch so real Turnstile can be shown while everything else stays fake.
    var captcha = config.GetSection(CaptchaOptions.Section).Get<CaptchaOptions>() ?? new CaptchaOptions();
    var realCaptcha = captcha.Provider == CaptchaProvider.Turnstile || (captcha.Provider == CaptchaProvider.Auto && !useFakes);
    if (realCaptcha)
    {
      services.AddHttpClient<ICaptchaVerifier, TurnstileCaptchaVerifier>(c => c.Timeout = TimeSpan.FromSeconds(5));
      if (string.IsNullOrWhiteSpace(captcha.SecretKey))
      {
        logger.LogWarning("Captcha:SecretKey is empty: every /api/check will fail with captchaFailed. Set it or use Captcha:Provider=Fake.");
      }
    }
    else
    {
      services.AddSingleton<ICaptchaVerifier, FakeCaptchaVerifier>();
    }

    logger.LogInformation("Infrastructure registered (UseFakes={UseFakes}, Captcha={Captcha}, Vision={Vision})",
      useFakes, realCaptcha ? "turnstile" : "fake", realVision ? $"{vision.Model} @ {vision.BaseUrl}" : "fake");

    return services;
  }
}
