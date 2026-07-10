using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OnlyNines.Core;

public static class DatasetLoader
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static List<SlaService> LoadDirectory(string path) =>
        Directory.EnumerateFiles(path, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(path, "*.yml", SearchOption.AllDirectories))
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => Load(File.ReadAllText(f), f))
            .ToList();

    public static SlaService Load(string yaml, string? sourceHint = null)
    {
        try
        {
            var service = Yaml.Deserialize<SlaService>(yaml);
            Validate(service, sourceHint);
            return service;
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException($"Failed to parse SLA dataset file '{sourceHint}': {ex.Message}", ex);
        }
    }

    private static void Validate(SlaService s, string? sourceHint)
    {
        if (string.IsNullOrWhiteSpace(s.ResourceType))
            throw new InvalidDataException($"'{sourceHint}': resourceType is required.");
        if (s.Ignore)
        {
            if (string.IsNullOrWhiteSpace(s.IgnoreReason))
                throw new InvalidDataException($"'{sourceHint}': ignored types must state ignoreReason.");
            return;
        }
        if (s.Variants.Count == 0)
            throw new InvalidDataException($"'{sourceHint}': at least one variant is required.");
        foreach (var v in s.Variants)
            if (v.Sla is <= 0 or > 1)
                throw new InvalidDataException($"'{sourceHint}': variant '{v.Id}' has SLA {v.Sla}, expected (0,1].");
        foreach (var id in s.Ladder)
            if (s.Variants.All(v => v.Id != id))
                throw new InvalidDataException($"'{sourceHint}': ladder references unknown variant '{id}'.");
    }
}
