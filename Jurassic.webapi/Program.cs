using BlazorApp1.Components;
using BlazorApp1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<UserSession>();

// Blazor Server: UserSession is scoped per circuit. IHttpClientFactory wires message handlers from the
// application root DI scope, so a delegating handler registered there gets a different UserSession than
// components—requests go out without a Bearer token while the UI still appears signed in. Build one
// HttpClient per scope so MovieApiAuthHeaderHandler shares the same UserSession instance.
builder.Services.AddScoped(static sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["MovieApi:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5080";
    var session = sp.GetRequiredService<UserSession>();

    var innerHandler = new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    };

    var authHandler = new MovieApiAuthHeaderHandler(session) { InnerHandler = innerHandler };

    return new HttpClient(authHandler, disposeHandler: true)
    {
        BaseAddress = new Uri($"{baseUrl}/", UriKind.Absolute),
    };
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

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
