using System.Net;
using System.Text;
using FluentAssertions;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Options;
using Jurassic.movieserviceapi.Repositories;
using Jurassic.movieserviceapi.Services;
using Moq;

namespace Jurassic.movieserviceapi.Tests;

public sealed class InfinityGatewayTests
{
    private static InfinityGateway CreateGateway(
        HttpMessageHandler handler,
        Mock<IMovieRepository>? movieRepo = null,
        Mock<IMovieAttractionMapRepository>? mapRepo = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var movies = movieRepo ?? new Mock<IMovieRepository>();
        var maps = mapRepo ?? new Mock<IMovieAttractionMapRepository>();
        return new InfinityGateway(
            http,
            Microsoft.Extensions.Options.Options.Create(new InfinityOptions
            {
                WebApiBaseUrl = "http://localhost:8080",
                WebAppBaseUrl = "http://localhost:8082",
                JurassicParkId = "park_florida_usa",
                MovieCategoryName = "Movie"
            }),
            movies.Object,
            maps.Object);
    }

    public InfinityGatewayTests()
    {
        InfinityGateway.ResetCache();
    }

    [Fact]
    public async Task GetRatingsSummaryAsync_WhenInfinityUnreachable_ReturnsEmptyList()
    {
        var movieId = Guid.NewGuid();
        var movieRepo = new Mock<IMovieRepository>();
        movieRepo.Setup(r => r.GetMoviesAsync())
            .ReturnsAsync(new List<MovieListItemDto> { new(movieId, "Jurassic Park", 127, "PG-13", "now_showing") });

        var mapRepo = new Mock<IMovieAttractionMapRepository>();
        mapRepo.Setup(m => m.GetAttractionIdAsync(movieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var gateway = CreateGateway(new RefusedHttpHandler(), movieRepo, mapRepo);

        var summary = await gateway.GetRatingsSummaryAsync();

        summary.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRatingsSummaryAsync_ReturnsDataFromAttractionsApi()
    {
        var movieId = Guid.NewGuid();
        var attractionId = Guid.NewGuid();

        var movieRepo = new Mock<IMovieRepository>();
        movieRepo.Setup(r => r.GetMoviesAsync())
            .ReturnsAsync(new List<MovieListItemDto> { new(movieId, "Jurassic Park", 127, "PG-13", "now_showing") });

        var mapRepo = new Mock<IMovieAttractionMapRepository>();
        mapRepo.Setup(m => m.GetAttractionIdAsync(movieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null); // Force refresh

        var gateway = CreateGateway(new AttractionRatingHttpHandler(attractionId), movieRepo, mapRepo);

        var summary = await gateway.GetRatingsSummaryAsync();

        summary.Should().ContainSingle();
        summary[0].MovieId.Should().Be(movieId);
        summary[0].AvgStars.Should().Be(4.5m);
        summary[0].ReviewCount.Should().Be(10);
    }

    [Fact]
    public async Task GetUserRatingAsync_ReturnsStars_WhenRatingExists()
    {
        var movieId = Guid.NewGuid();
        var attractionId = Guid.NewGuid();

        var movieRepo = new Mock<IMovieRepository>();
        var mapRepo = new Mock<IMovieAttractionMapRepository>();
        mapRepo.Setup(m => m.GetAttractionIdAsync(movieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attractionId);

        var json = $$"""
            {
                "value": 4,
                "average": 4.2,
                "count": 5
            }
            """;
        var gateway = CreateGateway(new StaticJsonHandler(json), movieRepo, mapRepo);

        var rating = await gateway.GetUserRatingAsync(movieId, "bearer");

        rating.Stars.Should().Be(4);
        rating.Exists.Should().BeTrue();
    }

    private sealed class StaticJsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class AttractionRatingHttpHandler(Guid attractionId) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/dev/token"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("test-token") });
            }
            if (path.Contains("/api/Attractions"))
            {
                var json = $$"""
                    [
                        {
                            "id": "{{attractionId}}",
                            "name": "Jurassic Park",
                            "parkId": "park_florida_usa",
                            "avgRating": 1.2,
                            "reviewCount": 1
                        }
                    ]
                    """;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }
            if (path.Contains("/api/ratings/") && path.Contains("/mine"))
            {
                var json = $$"""
                    {
                        "value": null,
                        "average": 4.5,
                        "count": 10
                    }
                    """;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromException<HttpResponseMessage>(new HttpRequestException("Unexpected: " + path));
        }
    }

    private sealed class RefusedHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection refused"));
    }

    private sealed class PlainTextReviewHttpHandler : HttpMessageHandler
    {
        private static readonly string ReviewJson = """
            [{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","author":"API","date":"2026-01-01","comment":"Loved it direct from Infinity","isOwner":false}]
            """;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/api/reviews/", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ReviewJson.Trim(), Encoding.UTF8, "application/json")
                });
            }

            return Task.FromException<HttpResponseMessage>(
                new HttpRequestException("Unexpected request: " + request.RequestUri));
        }
    }

    private sealed class ApiStarsReviewHttpHandler : HttpMessageHandler
    {
        private static readonly string ReviewJson = """
            [{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","author":"API","date":"2026-01-02","comment":"Great","stars":4,"isOwner":false}]
            """;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/api/reviews/", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ReviewJson.Trim(), Encoding.UTF8, "application/json")
                });
            }

            return Task.FromException<HttpResponseMessage>(
                new HttpRequestException("Unexpected request: " + request.RequestUri));
        }
    }
}
