using LeezenPass.Api.Infrastructure;

namespace LeezenPass.Api.Configurations;

public static class ServiceConfigs
{
  public static IServiceCollection AddServiceConfigs(this IServiceCollection services, ILogger logger, WebApplicationBuilder builder)
  {
    services.AddInfrastructureServices(builder.Configuration, logger)
            .AddAuthConfigs(builder.Environment)
            .AddRateLimitConfigs();

    return services;
  }
}
