using System.Reflection;

namespace OnlyNines.Web.Services;

/// <summary>Serves the KQL queries embedded from the repo's kql/ directory.</summary>
public sealed class KqlProvider
{
    public string Default { get; }
    public string Paranoid { get; }

    public KqlProvider()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var names = assembly.GetManifestResourceNames();

        Paranoid = Read(assembly, names.Single(n => n.Contains("paranoid", StringComparison.OrdinalIgnoreCase)));
        Default = Read(assembly, names.Single(n =>
            n.EndsWith(".kql", StringComparison.OrdinalIgnoreCase) &&
            !n.Contains("paranoid", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// One-liner for Azure Cloud Shell / az CLI: comments stripped, collapsed to a single line
    /// so it survives pasting into bash and PowerShell alike. Output format matches our parser.
    /// </summary>
    public string CloudShellCommand => _cloudShellCommand ??= BuildCloudShellCommand(Default);
    private string? _cloudShellCommand;

    private static string BuildCloudShellCommand(string kql)
    {
        // Multi-line inside the quoted query — reads cleanly and never needs horizontal scroll.
        // Bash and PowerShell both accept newlines within a double-quoted argument.
        var body = string.Join("\n",
            kql.Split('\n')
               .Select(l => l.TrimEnd())
               .Where(l => l.Trim().Length > 0 && !l.TrimStart().StartsWith("//")));
        // `download` is a Cloud Shell (bash) built-in: pushes the file straight to the browser,
        // so the user never selects/copies anything in the terminal.
        return $"az graph query --first 1000 -o json -q \"\n{body}\n\" > onlynines.json && download onlynines.json";
    }

    private static string Read(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing embedded resource '{name}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
