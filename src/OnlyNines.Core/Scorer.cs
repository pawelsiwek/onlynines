namespace OnlyNines.Core;

public sealed class Scorer
{
    private readonly IReadOnlyDictionary<string, SlaService> _byType;

    public Scorer(IEnumerable<SlaService> dataset) =>
        _byType = dataset.ToDictionary(
            s => s.ResourceType.ToLowerInvariant(),
            StringComparer.OrdinalIgnoreCase);

    public ScoredResource Score(AzureResource resource)
    {
        if (!_byType.TryGetValue(resource.Type.ToLowerInvariant(), out var service))
            return new ScoredResource(resource, null, null);

        if (service.Ignore)
            return new ScoredResource(resource, service, null);

        // Ordered rules, first match wins. Catch-all variants (empty match) go last in YAML.
        var variant = service.Variants.FirstOrDefault(v => Matches(v, resource));
        return new ScoredResource(resource, service, variant);
    }

    /// <summary>Default grouping: one resource group = one application (worst-case serial).</summary>
    public IReadOnlyList<GroupScore> ScoreEnvironment(IEnumerable<AzureResource> resources) =>
        resources
            .GroupBy(r => r.ResourceGroup ?? "(no group)", StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupScore { Group = g.Key, Members = g.Select(Score).ToList() })
            .OrderBy(g => g.CompositeSla)
            .ToList();

    internal static bool Matches(SlaVariant variant, AzureResource resource)
    {
        foreach (var (key, expected) in variant.Match)
        {
            resource.Attributes.TryGetValue(key, out var actual);
            if (!ValueMatches(expected, actual)) return false;
        }
        return true;
    }

    private static bool ValueMatches(object? expected, string? actual) => expected switch
    {
        null => actual is null,
        bool b => bool.TryParse(actual, out var ab) && ab == b,
        // YAML sequences arrive as List<object>: any-of semantics.
        System.Collections.IEnumerable list and not string =>
            list.Cast<object?>().Any(o => ValueMatches(o, actual)),
        _ => string.Equals(expected.ToString(), actual, StringComparison.OrdinalIgnoreCase),
    };
}
