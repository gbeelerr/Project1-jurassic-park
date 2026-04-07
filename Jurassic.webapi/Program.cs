using BlazorApp1.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["MovieApi:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5080";
    return new HttpClient { BaseAddress = new Uri(baseUrl + "/", UriKind.Absolute) };
});

var app = builder.Build();

var httpOnly = string.Equals(
    Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_ONLY"),
    "true",
    StringComparison.OrdinalIgnoreCase);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    if (!httpOnly)
    {
        app.UseHsts();
    }
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!httpOnly)
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();