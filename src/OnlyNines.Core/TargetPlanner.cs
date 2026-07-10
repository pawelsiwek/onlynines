namespace OnlyNines.Core;

/// <summary>
/// Workload criticality classes, Azure Well-Architected style: the availability target
/// is a business decision, and it changes what "good" means for the same architecture.
/// </summary>
public sealed record CriticalityClass(string Id, string Label, double TargetSla, string Blurb);

public static class Criticality
{
    public static readonly IReadOnlyList<CriticalityClass> Classes = new List<CriticalityClass>
    {
        new("blog", "It's a blog", 0.99, "Nobody pages anyone. Downtime is an inconvenience."),
        new("internal", "Internal / dev-test", 0.995, "People wait; business doesn't stop."),
        new("production", "Production", 0.999, "Customers notice. The default for revenue-adjacent workloads."),
        new("business-critical", "Business-critical", 0.9995, "Revenue stops when this stops."),
        new("mission-critical", "Mission-critical", 0.9999, "Safety, compliance, or the whole company."),
    };

    public static CriticalityClass Get(string? id) =>
        Classes.FirstOrDefault(c => c.Id == id) ?? Classes[2]; // default: production
}

public enum TargetVerdict { Under, OnTarget, Over, Unreachable }

public sealed record TargetPlan(
    double CurrentSla,
    double TargetSla,
    TargetVerdict Verdict,
    /// <summary>Upgrades (by impact) that are actually needed to reach the target — and nothing more.</summary>
    IReadOnlyList<ScoredResource> NeededUpgrades,
    /// <summary>Composite after applying only the needed upgrades.</summary>
    double SlaAfterNeeded,
    /// <summary>Possible upgrades the target does NOT justify.</summary>
    IReadOnlyList<ScoredResource> BeyondTarget,
    /// <summary>Resources that can step DOWN a rung while still meeting the target.</summary>
    IReadOnlyList<ScoredResource> DowngradeCandidates);

public static class TargetPlanner
{
    public static TargetPlan Plan(IEnumerable<ScoredResource> members, double target)
    {
        var scored = members.Where(m => m.IsScored).ToList();
        var current = Availability.Serial(scored.Select(m => m.Variant!.Sla));

        var upgradeable = scored
            .Where(m => m.NextRung is not null && m.NextRung.Sla > m.Variant!.Sla)
            .OrderByDescending(m => (m.NextRung!.Sla - m.Variant!.Sla))
            .ToList();

        // Greedy: apply highest-impact upgrades until the target is met. Everything else
        // is beyond the target — the tool's job is to tell you where to STOP.
        var needed = new List<ScoredResource>();
        var running = current;
        if (current < target)
        {
            foreach (var m in upgradeable)
            {
                if (running >= target) break;
                running = running / m.Variant!.Sla * m.NextRung!.Sla;
                needed.Add(m);
            }
        }

        var beyond = upgradeable.Except(needed).ToList();

        // Downgrades: each independently keeps the composite at/above target.
        var downgrades = new List<ScoredResource>();
        if (current >= target)
        {
            foreach (var m in scored)
            {
                if (m.PrevRung is not { } prev || prev.Sla >= m.Variant!.Sla) continue;
                if (current / m.Variant!.Sla * prev.Sla >= target)
                    downgrades.Add(m);
            }
        }

        var verdict =
            current >= target
                ? (downgrades.Count > 0 ? TargetVerdict.Over : TargetVerdict.OnTarget)
                : (running >= target ? TargetVerdict.Under : TargetVerdict.Unreachable);

        return new TargetPlan(current, target, verdict, needed, running, beyond, downgrades);
    }
}
