using System.Text;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Captcha;
using LeezenPass.Api.Infrastructure.Email;
using LeezenPass.Api.Infrastructure.Storage;

namespace LeezenPass.Api.Configurations;

public static class OptionConfigs
{
  public static IServiceCollection AddOptionConfigs(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<FeaturesOptions>(configuration.GetSection(FeaturesOptions.Section))
            .Configure<AppOptions>(configuration.GetSection(AppOptions.Section))
            .Configure<StorageOptions>(configuration.GetSection(StorageOptions.Section))
            .Configure<EmailOptions>(configuration.GetSection(EmailOptions.Section))
            .Configure<CaptchaOptions>(configuration.GetSection(CaptchaOptions.Section));

    services.AddOptions<FeinOptions>()
            .Bind(configuration.GetSection(FeinOptions.Section))
            .Validate(o => Encoding.UTF8.GetByteCount(o.HmacSecret) >= FeinCode.MinSecretBytes,
              $"Fein:HmacSecret must be at least {FeinCode.MinSecretBytes} bytes.")
            .ValidateOnStart();

    return services;
  }
}
