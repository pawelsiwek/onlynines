using System.Text.Json;
using OnlyNines.Core;

namespace OnlyNines.Web.Services;

/// <summary>
/// Strips identifying data before a stack is saved to a public permalink.
/// Keeps exactly what scoring needs (type, tier, zones, HA); replaces names,
/// resource groups and ids with stable aliases. A public report should show
/// WHAT is weak, never WHERE it lives.
/// </summary>
public static class InventoryAnonymizer
{
    public static string Anonymize(IEnumerable<AzureResource> resources)
    {
        var groupAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var typeCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<Dictionary<string, object?>>();

        foreach (var r in resources)
        {
            var shortType = r.Type[(r.Type.LastIndexOf('/') + 1)..];
            var n = typeCounters[shortType] = typeCounters.GetValueOrDefault(shortType) + 1;

            string? groupAlias = null;
            if (r.ResourceGroup is not null && !groupAliases.TryGetValue(r.ResourceGroup, out groupAlias))
            {
                groupAlias = $"group-{groupAliases.Count + 1}";
                groupAliases[r.ResourceGroup] = groupAlias;
            }

            var row = new Dictionary<string, object?>
            {
                ["id"] = $"anon-{shortType}-{n}",
                ["name"] = $"{shortType}-{n:00}",
                ["resourceGroup"] = groupAlias,
                ["type"] = r.Type,
                ["location"] = r.Location,
            };
            foreach (var (key, value) in r.Attributes)
                row[key] = value;

            rows.Add(row);
        }

        return JsonSerializer.Serialize(rows);
    }
}
