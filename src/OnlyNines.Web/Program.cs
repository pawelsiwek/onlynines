using OnlyNines.Web.Components;
using OnlyNines.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<DatasetProvider>();
builder.Services.AddSingleton<KqlProvider>();
builder.Services.AddSingleton<StackStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// SEO plumbing: robots + sitemap over the static routes and the generated corpus.
app.MapGet("/robots.txt", () => Results.Content(
    "User-agent: *\nAllow: /\nSitemap: https://onlynines.app/sitemap.xml\n", "text/plain"));

app.MapGet("/sitemap.xml", (OnlyNines.Web.Services.DatasetProvider dataset) =>
{
    var urls = new List<string> { "", "assess", "calculator", "sla", "methodology", "report/sample", "status" };
    urls.AddRange(dataset.Services.Where(s => !s.Ignore)
        .Select(s => $"sla/{OnlyNines.Web.Services.DatasetProvider.SlugFor(s)}"));
    urls.AddRange(OnlyNines.Web.Components.Pages.NinesPage.AllValues.Select(v => $"nines/{v}"));

    var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n" +
        string.Join("\n", urls.Select(u => $"  <url><loc>https://onlynines.app/{u}</loc></url>")) +
        "\n</urlset>";
    return Results.Content(xml, "application/xml");
});

// README badge: the 24/7 traffic + backlink engine. Recomputed per request so it
// tracks dataset updates; cached at the edge for an hour.
app.MapGet("/badge/{slug}.svg", async (string slug, StackStore store, OnlyNines.Web.Services.DatasetProvider dataset, HttpContext http) =>
{
    var payload = await store.GetAsync(slug);
    if (payload is null) return Results.NotFound();

    double sla;
    try
    {
        var resources = OnlyNines.Core.ResourceGraphParser.Parse(
            OnlyNines.Web.Services.StackPayload.Unwrap(payload).Input);
        var scored = dataset.Scorer.ScoreEnvironment(resources)
            .SelectMany(g => g.Members).Where(m => m.IsScored).Select(m => m.Variant!.Sla);
        sla = OnlyNines.Core.Availability.Serial(scored);
    }
    catch
    {
        return Results.NotFound();
    }

    http.Response.Headers.CacheControl = "public, max-age=3600";
    return Results.Content(OnlyNines.Web.Services.BadgeSvg.Render(sla), "image/svg+xml");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
