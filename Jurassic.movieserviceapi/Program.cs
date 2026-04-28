using Jurassic.movieserviceapi.Repositories;
using Jurassic.movieserviceapi.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck("Database", () =>
    {
        try
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    });
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddSingleton<DatabaseBootstrapper>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy.WithOrigins("http://localhost:5044", "https://localhost:7022")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (builder.Configuration.GetValue("DatabaseBootstrap:Enabled", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var bootstrapper = scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>();
    await bootstrapper.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.UseCors("AllowWeb");

app.MapHealthChecks("/api/health");

var movies = new[]
{
    new Movie(1, "Jurassic Park", 127),
    new Movie(2, "The Lost World: Jurassic Park", 129),
    new Movie(3, "Jurassic Park III", 92),
    new Movie(4, "Jurassic World", 124),
    new Movie(5, "Jurassic World: Fallen Kingdom", 128),
    new Movie(6, "Jurassic World Dominion", 147)
};

app.MapGet("/weatherforecast", () =>
{
    return new { message = "API is running" };
});

app.MapGet("/movies", () => movies);

app.MapGet("/movies/now-playing", async (DateOnly? date, IMovieRepository repo) =>
{
    // Dapper doesn't support DateOnly params directly; pass as DateTime (UTC midnight)
    var dateUtc = date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null;
    return await repo.GetNowPlayingAsync(dateUtc);
});

app.MapGet("/showtimes/{showtimeId:guid}", async (Guid showtimeId, IMovieRepository repo) =>
{
    var showtimeDetails = await repo.GetShowtimeDetailsAsync(showtimeId);
    return showtimeDetails is null ? Results.NotFound() : Results.Ok(showtimeDetails);
});

app.Run();

public partial class Program { }

record Movie(int Id, string Title, int Runtime);
