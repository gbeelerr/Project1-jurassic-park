using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace Jurassic.movieserviceapi.Tests;

public class MovieApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed class AlwaysHealthyCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }

    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IMovieRepository> _movieRepoMock = new();

    public MovieApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DatabaseBootstrap:Enabled", "false");
            builder.UseSetting("WebAuthSeed:Enabled", "false");
            builder.UseSetting("Auth:JwtSigningKey", "test-signing-key-must-be-at-least-32-chars!");
            builder.UseSetting(
                "ConnectionStrings:WebConnection",
                "Host=127.0.0.1;Database=jurassic_web;Username=jurassic;Password=jurassic_dev");
            builder.ConfigureServices(services =>
            {
                // Remove the existing registration of IMovieRepository
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMovieRepository));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add the mock repository
                services.AddScoped(_ => _movieRepoMock.Object);
                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    options.Registrations.Clear();
                    options.Registrations.Add(new HealthCheckRegistration(
                        "postgres-primary",
                        _ => new AlwaysHealthyCheck(),
                        failureStatus: null,
                        tags: null));
                });
            });
        });
    }

    [Fact]
    public async Task GetNowPlaying_ShouldReturnMovies_FromRepository()
    {
        // Arrange
        var expectedMovies = new List<NowPlayingMovieDto>
        {
            new NowPlayingMovieDto(
                Guid.NewGuid(), 
                "Test Movie", 
                "Description", 
                "PG-13", 
                120, 
                "url", 
                Guid.NewGuid(), 
                "Screen 1", 
                "Standard", 
                new List<ShowtimeDetail>
                {
                    new ShowtimeDetail(Guid.NewGuid(), DateTime.Now.AddHours(2), 10.50m)
                }
            )
        };

        _movieRepoMock.Setup(repo => repo.GetNowPlayingAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(expectedMovies);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/movies/now-playing");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualMovies = await response.Content.ReadFromJsonAsync<List<NowPlayingMovieDto>>();
        actualMovies.Should().NotBeNull();
        actualMovies.Should().HaveCount(1);
        actualMovies![0].Title.Should().Be("Test Movie");
    }

    [Fact]
    public async Task GetMoviePosters_ShouldReturnPosters_FromRepository()
    {
        var movieId = Guid.NewGuid();
        var expected = new List<MoviePosterDto>
        {
            new(movieId, "Test Movie", "https://example.com/poster.jpg")
        };

        _movieRepoMock.Setup(repo => repo.GetMoviePostersAsync()).ReturnsAsync(expected);

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/movies/posters");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<MoviePosterDto>>();
        body.Should().NotBeNull();
        var posters = body!;
        posters.Should().HaveCount(1);
        posters[0].MovieId.Should().Be(movieId);
        posters[0].Title.Should().Be("Test Movie");
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMovies_ShouldReturnMovies_FromRepository()
    {
        var movieId = Guid.NewGuid();
        var expected = new List<MovieListItemDto>
        {
            new(movieId, "Jurassic Park", 127, "PG-13", "now_showing")
        };
        _movieRepoMock.Setup(repo => repo.GetMoviesAsync()).ReturnsAsync(expected);

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/movies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<MovieListItemDto>>();
        body.Should().NotBeNull();
        var movies = body!;
        movies.Should().HaveCount(1);
        movies[0].MovieId.Should().Be(movieId);
        movies[0].Title.Should().Be("Jurassic Park");
    }

    [Fact]
    public async Task GetShowtimeById_ShouldReturnShowtimeDetails_WhenFound()
    {
        // Arrange
        var showtimeId = Guid.NewGuid();
        var details = new ShowtimeDetailsDto(
            showtimeId,
            Guid.NewGuid(),
            "Jurassic Park",
            "IMAX",
            DateTime.UtcNow.AddHours(2),
            18.50m
        );

        _movieRepoMock.Setup(repo => repo.GetShowtimeDetailsAsync(showtimeId))
            .ReturnsAsync(details);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/showtimes/{showtimeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ShowtimeDetailsDto>();
        body.Should().NotBeNull();
        body!.ShowtimeId.Should().Be(showtimeId);
        body.MovieTitle.Should().Be("Jurassic Park");
    }

    [Fact]
    public async Task GetShowtimeById_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var showtimeId = Guid.NewGuid();
        _movieRepoMock.Setup(repo => repo.GetShowtimeDetailsAsync(showtimeId))
            .ReturnsAsync((ShowtimeDetailsDto?)null);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/showtimes/{showtimeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // TDD / Future Tests (Expected to fail until implemented)
    // These serve as a specification for what the API should provide in the future.

    [Fact]
    [Trait("Category", "TDD")]
    public async Task GetMovieById_ShouldReturnMovie_WhenFound()
    {
        // Arrange
        var movieId = Guid.NewGuid();
        // This endpoint doesn't exist yet, but it's part of the TDD approach
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/movies/{movieId}");

        // Assert
        // This will currently fail with 404 until the endpoint is implemented in Program.cs
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "TDD")]
    public async Task CreateMovie_ShouldReturnCreated()
    {
        // Arrange
        var newMovie = new { Title = "New Movie", Runtime = 100 };
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/movies", newMovie);

        // Assert
        // This will currently fail with 405 (Method Not Allowed) until POST /movies is implemented
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    [Trait("Category", "TDD")]
    public async Task GetShowtimesForMovie_ShouldReturnShowtimes()
    {
        // Arrange
        var movieId = Guid.NewGuid();
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/movies/{movieId}/showtimes");

        // Assert
        // This will currently fail with 404
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
