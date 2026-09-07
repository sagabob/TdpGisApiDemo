using Azure.Monitor.OpenTelemetry.AspNetCore;

namespace TdpGis.Api.Hosting;

/// <summary>
///     Azure Monitor / Application Insights via OpenTelemetry.
///     Enabled only when APPLICATIONINSIGHTS_CONNECTION_STRING or
///     ApplicationInsights:ConnectionString is set (cloud / user-secrets).
///     Local runs without a connection string keep console logging only.
/// </summary>
public static class TelemetryExtensions
{
    public const string ConnectionStringEnvVar = "APPLICATIONINSIGHTS_CONNECTION_STRING";
    public const string ConnectionStringConfigKey = "ApplicationInsights:ConnectionString";

    public static WebApplicationBuilder AddTdpGisApiTelemetry(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration[ConnectionStringConfigKey];
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);

        if (string.IsNullOrWhiteSpace(connectionString))
            return builder;

        builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
        {
            options.ConnectionString = connectionString;
        });

        return builder;
    }
}
