using OnlyNines.Core;

if (args.Length is 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("""
        OnlyNines — we only count nines.

        Usage:
          onlynines <report.json> [--data <path-to-data/sla>]

        <report.json>  JSON exported from Azure Resource Graph (kql/inventory.kql)
        --data         SLA dataset directory (default: ./data/sla, walking up from cwd)
        """);
    return 0;
}

var reportPath = args[0];
var dataPath = GetOption(args, "--data") ?? FindDataDir()
    ?? Fail("Could not locate data/sla. Pass --data <path>.");

List<SlaService> dataset;
List<AzureResource> resources;
try
{
    dataset = DatasetLoader.LoadDirectory(dataPath);
    resources = ResourceGraphParser.Parse(File.ReadAllText(reportPath));
}
catch (Exception ex)
{
    return Fail(ex.Message) is null ? 1 : 1;
}

var groups = new Scorer(dataset).ScoreEnvironment(resources);

Console.WriteLine($"\nOnlyNines assessment — {resources.Count} resources, {groups.Count} group(s). Worst-case serial model.\n");

foreach (var g in groups)
{
    Console.WriteLine($"── {g.Group}");
    Console.WriteLine($"   composite: {g.CompositeSla * 100:F3}%  ({Availability.Nines(g.CompositeSla):F1} nines)  ·  downtime budget: {g.DowntimeHoursPerYear:F1} h/yr");

    var weakest = g.WeakestLinks.Take(5).ToList();
    if (weakest.Count > 0)
    {
        Console.WriteLine("   weakest links:");
        foreach (var w in weakest)
        {
            var upgrade = w.NextRung is { } next
                ? $"  → next rung: {next.Id} ({next.Sla * 100:F2}%)"
                : "";
            Console.WriteLine(
                $"     {w.Resource.Name ?? w.Resource.Id,-30} {w.Variant!.Id,-24} -{w.DowntimeHoursPerYear,6:F1} h/yr{upgrade}");
        }
    }

    var unknown = g.Unknown.ToList();
    if (unknown.Count > 0)
    {
        Console.WriteLine($"   unknown (not scored — we don't score what we can't verify): {unknown.Count}");
        foreach (var u in unknown.Take(5))
            Console.WriteLine($"     {u.Resource.Name ?? u.Resource.Id} ({u.Resource.Type})");
    }
    Console.WriteLine();
}

Console.WriteLine("Designed availability, not a guarantee. Financially-backed SLA != observed uptime.");
Console.WriteLine("Details: https://onlynines.app");
return 0;

static string? GetOption(string[] args, string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string? FindDataDir()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, "data", "sla");
        if (Directory.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    return null;
}

static string? Fail(string message)
{
    Console.Error.WriteLine($"error: {message}");
    Environment.Exit(1);
    return null;
}
