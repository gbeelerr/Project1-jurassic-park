using System.Security.Cryptography;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Options;
using Jurassic.movieserviceapi.Repositories;
using Jurassic.movieserviceapi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
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
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<PasswordHasher<WebUser>>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddSingleton<WebAuthSeeder>();

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

if (builder.Configuration.GetValue("WebAuthSeed:Enabled", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var webAuthSeeder = scope.ServiceProvider.GetRequiredService<WebAuthSeeder>();
    await webAuthSeeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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

app.MapGet("/movies/posters", async (IMovieRepository repo) =>
{
    return await repo.GetMoviePostersAsync();
});

app.MapGet("/movies/now-playing", async (DateOnly? date, IMovieRepository repo) =>
{
    var dateUtc = date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null;
    return await repo.GetNowPlayingAsync(dateUtc);
});

app.MapGet("/showtimes/{showtimeId:guid}", async (Guid showtimeId, IMovieRepository repo) =>
{
    var showtimeDetails = await repo.GetShowtimeDetailsAsync(showtimeId);
    return showtimeDetails is null ? Results.NotFound() : Results.Ok(showtimeDetails);
});

app.MapPost("/auth/register", async (
        RegisterRequest body,
        IAuthRepository authRepository,
        PasswordHasher<WebUser> passwordHasher,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
        {
            return Results.BadRequest();
        }

        if (body.Password.Length < 8)
        {
            return Results.BadRequest();
        }

        var email = body.Email.Trim();
        if (await authRepository.EmailExistsAsync(email, cancellationToken))
        {
            return Results.Conflict();
        }

        var userId = Guid.NewGuid();
        var principal = new WebUser
        {
            Id = userId,
            Email = email,
            PasswordHash = "",
            Role = "customer",
            IsActive = true
        };
        var hash = passwordHasher.HashPassword(principal, body.Password);
        var displayName = string.IsNullOrWhiteSpace(body.DisplayName) ? null : body.DisplayName.Trim();

        await authRepository.InsertUserAsync(userId, email, displayName, hash, cancellationToken);

        return Results.Created($"/auth/me", new RegisterResponse { UserId = userId, Email = email });
    })
    .WithName("AuthRegister");

app.MapPost("/auth/login", async (
        LoginRequest body,
        HttpContext http,
        IAuthRepository authRepository,
        PasswordHasher<WebUser> passwordHasher,
        IJwtTokenService jwtTokenService,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
        {
            return Results.Unauthorized();
        }

        var user = await authRepository.GetUserByEmailAsync(body.Email.Trim(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, body.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        var refreshBytes = new byte[32];
        RandomNumberGenerator.Fill(refreshBytes);
        var refreshToken = Convert.ToHexString(refreshBytes);

        var opts = authOptions.Value;
        var refreshExpires = DateTimeOffset.UtcNow.AddDays(Math.Clamp(opts.RefreshTokenDays, 1, 365));
        var ip = http.Connection.RemoteIpAddress?.ToString();
        var userAgent = http.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrEmpty(userAgent))
        {
            userAgent = null;
        }

        await authRepository.InsertSessionAsync(
            user.Id,
            refreshToken,
            refreshExpires,
            ip,
            userAgent,
            cancellationToken);

        var (accessToken, accessExpires) = jwtTokenService.CreateAccessToken(user);
        var expiresIn = (int)Math.Max(1, (accessExpires - DateTimeOffset.UtcNow).TotalSeconds);

        return Results.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresInSeconds = expiresIn,
            RefreshToken = refreshToken
        });
    })
    .WithName("AuthLogin");

app.Run();

public partial class Program { }

record Movie(int Id, string Title, int Runtime);
