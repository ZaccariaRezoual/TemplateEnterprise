namespace EnterpriseFramework.Api.Observability;

/// <summary>
/// Typed configuration for OpenTelemetry (section "Observability").
///
/// Off by default. Telemetry is opt-in because an exporter that cannot reach
/// its collector adds latency and noise to every request, and a template must
/// not impose an operational dependency on projects that do not have one yet.
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Observability";

    /// <summary>Whether tracing and metrics are collected and exported.</summary>
    public bool Enabled { get; init; }

    /// <summary>Service name reported to the collector.</summary>
    public string ServiceName { get; init; } = "enterprise-api";

    /// <summary>
    /// OTLP collector endpoint (Grafana Alloy, Tempo, Jaeger, …).
    /// </summary>
    public string OtlpEndpoint { get; init; } = "http://localhost:4317";

    /// <summary>
    /// Fraction of requests traced, 0–1. Full sampling is fine in
    /// development and expensive under load, so production usually lowers it.
    /// </summary>
    public double SamplingRatio { get; init; } = 1.0;
}
