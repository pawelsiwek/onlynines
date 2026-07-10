using System.Diagnostics.Metrics;

namespace OnlyNines.Web.Services;

/// <summary>
/// OTEL instruments. Vendor-neutral on purpose: exported to Azure Monitor today,
/// switchable to any OTLP backend (say, Dynatrace) by swapping the exporter only.
/// </summary>
public static class Telemetry
{
    public const string MeterName = "OnlyNines";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> AssessmentsRun =
        Meter.CreateCounter<long>("onlynines.assessments_run", description: "Inventories parsed and scored");

    public static readonly Counter<long> StacksSaved =
        Meter.CreateCounter<long>("onlynines.stacks_saved", description: "Assessments saved to permalinks");

    public static readonly Counter<long> BadgesServed =
        Meter.CreateCounter<long>("onlynines.badges_served", description: "Live badge SVGs served");
}
