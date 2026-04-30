using System.Security.Cryptography;
using System.Text;
using Jurassic.movieserviceapi.Diagnostics;
using Jurassic.movieserviceapi.HealthChecks;
using Jurassic.movieserviceapi.Http;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Options;
using Jurassic.movieserviceapi.Repositories;
using Jurassic.movieserviceapi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresPrimaryHealthCheck>("postgres-primary");

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

var jwtKey = builder.Configuration["Auth:JwtSigningKey"] ?? "";
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("Auth:JwtSigningKey must be set and at least 32 characters for JWT.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Auth:JwtIssuer"] ?? "jurassic-movieservice",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Auth:JwtAudience"] ?? "jurassic-clients",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddSingleton<DatabaseBootstrapper>();
builder.Services.AddSingleton<WebAuthSeeder>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IPasswordHasher<WebUser>, PasswordHasher<WebUser>>();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/health");

app.MapGet("/weatherforecast", () => new { message = "API is running" });

app.MapGet("/movies", async (IMovieRepository repo) => await repo.GetMoviesAsync());

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

app.MapGet("/movies/{movieId:guid}/showtimes/{showtimeId:guid}/seats",
        SeatReservationHandlers.GetSeatsForMovieShowtimeAsync)
    .WithName("GetSeatsForMovieShowtime");

app.MapGet("/showtimes/{showtimeId:guid}/seats", SeatReservationHandlers.GetSeatsForShowtimeAnonymousAsync)
    .WithName("GetSeatsForShowtime");

app.MapGet("/showtimes/{showtimeId:guid}/seat-availability", async (
        Guid showtimeId,
        IMovieRepository movieRepository,
        IBookingRepository bookingRepository,
        CancellationToken cancellationToken) =>
    {
        if (await movieRepository.GetShowtimeDetailsAsync(showtimeId) is null)
        {
            return Results.NotFound();
        }

        var labels = await bookingRepository.GetUnavailableSeatLabelsAsync(showtimeId, cancellationToken);
        return Results.Ok(new SeatAvailabilityResponse { ShowtimeId = showtimeId, UnavailableSeatLabels = labels });
    })
    .WithName("ShowtimeSeatAvailability");

app.MapPost("/auth/register", async (
        RegisterRequest body,
        IAuthRepository authRepository,
        IPasswordHasher<WebUser> passwordHasher,
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

        var email = body.Email.Trim().ToLowerInvariant();
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

        return Results.Created("/auth/me", new RegisterResponse { UserId = userId, Email = email });
    })
    .WithName("AuthRegister");

// POST /auth/login: validate email/password against jurassic_web.users; on success issue JWT + refresh token
// and insert jurassic_web.sessions; invalid credentials -> 401 Unauthorized.
app.MapPost("/auth/login", async (
        LoginRequest body,
        HttpContext http,
        IAuthRepository authRepository,
        IPasswordHasher<WebUser> passwordHasher,
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

app.MapGet("/me/bookings", UserBookingsHandlers.GetMyBookingsAsync)
    .RequireAuthorization()
    .WithName("GetMyBookings");

app.MapPost("/movies/{movieId:guid}/showtimes/{showtimeId:guid}/reserve", SeatReservationHandlers.ReserveSeatsForMovieShowtimeAsync)
    .RequireAuthorization()
    .WithName("ReserveSeatsForMovieShowtime");

app.MapPost("/checkout/confirm", SeatReservationHandlers.ReserveSeatsCheckoutStyleAsync)
    .RequireAuthorization()
    .WithName("CheckoutConfirm");

app.Run();

public partial class Program { }
