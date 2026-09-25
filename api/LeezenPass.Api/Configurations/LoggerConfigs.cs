using Serilog;

namespace LeezenPass.Api.Configurations;

public static class LoggerConfigs
{
  public static WebApplicationBuilder AddLoggerConfigs(this WebApplicationBuilder builder)
  {
    // Sinks and levels come from the "Serilog" config section. No request logging: it would record IPs.
    builder.Services.AddSerilog(config => config
      .ReadFrom.Configuration(builder.Configuration)
      .Enrich.FromLogContext());

    return builder;
  }
}
