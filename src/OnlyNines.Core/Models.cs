namespace OnlyNines.Core;

/// <summary>A single SLA tier of a service, matched against resource attributes.</summary>
public sealed record SlaVariant
{
    public required string Id { get; init; }

    /// <summary>Attribute conditions; all must match. Values: scalar (string/bool) or list (any-of).</summary>
    public Dictionary<string, object> Match { get; init; } = new();

    /// <summary>Fraction, e.g. 0.9995. Values are drafts until the file's lastVerified is current.</summary>
    public required double Sla { get; init; }

    public string? UpgradeNote { get; init; }
}

/// <summary>One Azure service definition loaded from data/sla/*.yaml.</summary>
public sealed record SlaService
{
    public required string Service { get; init; }
    public required string ResourceType { get; init; }
    public string? DocsUrl { get; init; }
    public string? LastVerified { get; init; }
    public List<SlaVariant> Variants { get; init; } = new();

    /// <summary>Variant ids from weakest to strongest — powers "next nine" upgrade suggestions.</summary>
    public List<string> Ladder { get; init; } = new();

    /// <summary>
    /// True for types that deliberately don't participate in the composite
    /// (management plane, no own SLA, or covered by another resource's SLA).
    /// </summary>
    public bool Ignore { get; init; }
    public string? IgnoreReason { get; init; }
}

/// <summary>A resource row parsed from Azure Resource Graph output.</summary>
public sealed record AzureResource
{
    public required string Id { get; init; }
    public string? Name { get; init; }
    public string? ResourceGroup { get; init; }
    public required string Type { get; init; }
    public string? Location { get; init; }

    /// <summary>Flattened attributes (tier, zr, ha, replication, zoneCount, ...) as strings.</summary>
    public Dictionary<string, string> Attributes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record ScoredResource(AzureResource Resource, SlaService? Service, SlaVariant? Variant)
{
    public bool IsScored => Variant is not null;

    /// <summary>Known type that deliberately doesn't count (see SlaService.Ignore).</summary>
    public bool IsNotApplicable => Service?.Ignore == true;

    public double DowntimeHoursPerYear =>
        Variant is null ? 0 : (1 - Variant.Sla) * Availability.HoursPerYear;

    /// <summary>Next rung on the service ladder, if any.</summary>
    public SlaVariant? NextRung
    {
        get
        {
            if (Service is null || Variant is null) return null;
            var idx = Service.Ladder.IndexOf(Variant.Id);
            if (idx < 0 || idx + 1 >= Service.Ladder.Count) return null;
            var nextId = Service.Ladder[idx + 1];
            return Service.Variants.FirstOrDefault(v => v.Id == nextId);
        }
    }
}

/// <summary>Scored group of resources (default grouping: resource group = one application).</summary>
public sealed record GroupScore
{
    public required string Group { get; init; }
    public required IReadOnlyList<ScoredResource> Members { get; init; }

    /// <summary>Worst-case serial composite: every scored member is a hard dependency.</summary>
    public double CompositeSla =>
        Availability.Serial(Members.Where(m => m.IsScored).Select(m => m.Variant!.Sla));

    public double DowntimeHoursPerYear => (1 - CompositeSla) * Availability.HoursPerYear;

    public IEnumerable<ScoredResource> Unknown => Members.Where(m => !m.IsScored && !m.IsNotApplicable);

    /// <summary>Known types that deliberately don't count (management plane etc.).</summary>
    public IEnumerable<ScoredResource> NotApplicable => Members.Where(m => m.IsNotApplicable);

    public IEnumerable<ScoredResource> WeakestLinks =>
        Members.Where(m => m.IsScored).OrderByDescending(m => m.DowntimeHoursPerYear);
}
