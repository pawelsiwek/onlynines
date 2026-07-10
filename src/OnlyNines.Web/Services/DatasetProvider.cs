using OnlyNines.Core;

namespace OnlyNines.Web.Services;

/// <summary>Loads the SLA dataset once (walking up from the content root to find data/sla).</summary>
public sealed class DatasetProvider
{
    public IReadOnlyList<SlaService> Services { get; }
    public Scorer Scorer { get; }

    public DatasetProvider(IWebHostEnvironment env)
    {
        var path = Find(env.ContentRootPath)
            ?? Find(AppContext.BaseDirectory)
            ?? throw new DirectoryNotFoundException(
                "Could not locate data/sla walking up from the content root. " +
                "Run from the repo, or copy data/sla next to the app.");
        Services = DatasetLoader.LoadDirectory(path);
        Scorer = new Scorer(Services);
    }

    /// <summary>URL slug for a service's SEO page, e.g. "azure-app-service".</summary>
    public static string SlugFor(OnlyNines.Core.SlaService s) =>
        string.Join("-", s.Service.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ')
            .Aggregate("", (acc, c) => acc + c)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string? Find(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "sla");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent!;
        }
        return null;
    }
}
