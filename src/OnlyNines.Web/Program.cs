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

// README badge: the 24/7 traffic + backlink engine. Recomputed per request so it
// tracks dataset updates; cached at the edge for an hour.
app.MapGet("/badge/{slug}.svg", async (string slug, StackStore store, OnlyNines.Web.Services.DatasetProvider dataset, HttpContext http) =>
{
    var payload = await store.GetAsync(slug);
    if (payload is null) return Results.NotFound();

    double sla;
    try
    {
        var resources = OnlyNines.Core.ResourceGraphParser.Parse(payload);
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
