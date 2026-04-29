using System.Security.Cryptography;
using Jurassic.movieserviceapi.Diagnostics;
using Jurassic.movieserviceapi.HealthChecks;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Options;
using Jurassic.movieserviceapi.Repositories;
using Jurassic.movieserviceapi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresPrimaryHealthCheck>("postgres-primary");

builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IPasswordHasher<WebUser>, PasswordHasher<WebUser>>();
builder.Services.AddSingleton<DatabaseBootstrapper>();
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

await using (var seedScope = app.Services.CreateAsyncScope())
{
    await seedScope.ServiceProvider.GetRequiredService<WebAuthSeeder>().SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
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

app.MapGet("/weatherforecast", () => new { message = "API is running" });

app.MapGet("/movies", () => movies);

app.MapGet("/movies/posters", async (IMovieRepository repo) => await repo.GetMoviePostersAsync());

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

app.MapPost("/auth/login", async (
    HttpContext http,
    LoginRequest body,
    IAuthRepository authRepo,
    IJwtTokenService jwt,
    IPasswordHasher<WebUser> passwordHasher,
    IOptions<AuthOptions> authOptions,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
    {
        return Results.Json(
            new ProblemDetails { Title = "Invalid request", Status = StatusCodes.Status400BadRequest, Detail = "Email and password are required." },
            statusCode: StatusCodes.Status400BadRequest);
    }

    var user = await authRepo.GetUserByEmailAsync(body.Email, cancellationToken);
    if (user is null || !user.IsActive)
    {
        return Results.Json(
            new ProblemDetails { Title = "Unauthorized", Status = StatusCodes.Status401Unauthorized, Detail = "Invalid email or password." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var verify = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, body.Password);
    if (verify == PasswordVerificationResult.Failed)
    {
        return Results.Json(
            new ProblemDetails { Title = "Unauthorized", Status = StatusCodes.Status401Unauthorized, Detail = "Invalid email or password." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var (accessToken, accessExpires) = jwt.CreateAccessToken(user);
    var refreshBytes = RandomNumberGenerator.GetBytes(32);
    var refreshToken = Convert.ToBase64String(refreshBytes);
    var refreshExpires = DateTimeOffset.UtcNow.AddDays(Math.Clamp(authOptions.Value.RefreshTokenDays, 1, 365));

    await authRepo.InsertSessionAsync(
        user.Id,
        refreshToken,
        refreshExpires,
        http.Connection.RemoteIpAddress?.ToString(),
        http.Request.Headers.UserAgent.ToString(),
        cancellationToken);

    var expiresIn = (int)Math.Max(1, (accessExpires - DateTimeOffset.UtcNow).TotalSeconds);
    return Results.Ok(new LoginResponse
    {
        AccessToken = accessToken,
        TokenType = "Bearer",
        ExpiresInSeconds = expiresIn,
        RefreshToken = refreshToken
    });
});

app.MapPost("/auth/register", async (
    RegisterRequest body,
    IAuthRepository authRepo,
    IPasswordHasher<WebUser> passwordHasher,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
    {
        return Results.Json(
            new ProblemDetails { Title = "Invalid request", Status = StatusCodes.Status400BadRequest, Detail = "Email and password are required." },
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (body.Password.Length < 8)
    {
        return Results.Json(
            new ProblemDetails { Title = "Invalid request", Status = StatusCodes.Status400BadRequest, Detail = "Password must be at least 8 characters." },
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (await authRepo.EmailExistsAsync(body.Email, cancellationToken))
    {
        return Results.Json(
            new ProblemDetails { Title = "Conflict", Status = StatusCodes.Status409Conflict, Detail = "An account with this email already exists." },
            statusCode: StatusCodes.Status409Conflict);
    }

    var id = Guid.NewGuid();
    var stub = new WebUser { Id = id, Email = body.Email.Trim(), PasswordHash = "", Role = "customer", IsActive = true };
    var hash = passwordHasher.HashPassword(stub, body.Password);
    await authRepo.InsertUserAsync(id, body.Email.Trim(), body.DisplayName?.Trim(), hash, cancellationToken);

    return Results.Created($"/auth/register/{id}", new RegisterResponse { UserId = id, Email = body.Email.Trim() });
});

app.Run();

public partial class Program { }

record Movie(int Id, string Title, int Runtime);
