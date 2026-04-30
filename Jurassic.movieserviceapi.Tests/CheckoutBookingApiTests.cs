using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Repositories;
using Jurassic.movieserviceapi.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jurassic.movieserviceapi.Tests;

public sealed class CheckoutBookingApiTests : IClassFixture<CheckoutBookingApiFixture>, IAsyncLifetime
{
    private readonly CheckoutBookingApiFixture _fixture;

    public CheckoutBookingApiTests(CheckoutBookingApiFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _fixture.BookingRepositoryMock.Reset();
        _fixture.MovieRepositoryMock.Reset();
        _fixture.BookingRepositoryMock
            .Setup(b => b.ListConfirmedBookingsForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MyBookingItem>());
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetSeatAvailability_UnknownShowtime_Returns404()
    {
        _fixture.MovieRepositoryMock.Setup(m => m.GetShowtimeDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ShowtimeDetailsDto?)null);

        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/showtimes/" + Guid.NewGuid() + "/seat-availability");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSeatAvailability_Known_ReturnsUnavailableJson()
    {
        var sid = Guid.NewGuid();
        _fixture.MovieRepositoryMock.Setup(m => m.GetShowtimeDetailsAsync(sid))
            .ReturnsAsync(new ShowtimeDetailsDto(
                sid,
                Guid.NewGuid(),
                "Film",
                "Hall",
                DateTime.UtcNow,
                9.99m));

        var labels = new[] { "D2", "A7" };
        _fixture.BookingRepositoryMock.Setup(b => b.GetUnavailableSeatLabelsAsync(sid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        var client = _fixture.Factory.CreateClient();
        using var resp = await client.GetAsync("/showtimes/" + sid + "/seat-availability");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<SeatAvailabilityResponse>();
        dto.Should().NotBeNull();
        dto!.UnavailableSeatLabels.Should().BeEquivalentTo(labels);
    }

    [Fact]
    public async Task PostCheckout_NoBearer_Returns401()
    {
        var body = new CheckoutConfirmRequest { ShowtimeId = Guid.NewGuid(), SeatLabels = ["A1"] };

        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        using var response = await client.PostAsJsonAsync("/checkout/confirm", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostCheckout_ValidJwt_CreatesBooking()
    {
        var userId = Guid.NewGuid();
        var sid = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        _fixture.MovieRepositoryMock.Setup(m => m.GetShowtimeDetailsAsync(sid))
            .ReturnsAsync(new ShowtimeDetailsDto(
                sid,
                Guid.NewGuid(),
                "Film",
                "Hall",
                DateTime.UtcNow,
                11m));

        _fixture.BookingRepositoryMock.Setup(
                b => b.TryCreateConfirmedBookingAsync(
                    userId,
                    sid,
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((bookingId, 33m, (string?)null));

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var principal = new WebUser
        {
            Id = userId,
            Email = "checkout-test@example.com",
            PasswordHash = "",
            Role = "customer",
            IsActive = true
        };
        var (token, _) = jwt.CreateAccessToken(principal);

        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new CheckoutConfirmRequest { ShowtimeId = sid, SeatLabels = [" B2 ", "A1"] };

        using var response = await client.PostAsJsonAsync("/checkout/confirm", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<CheckoutConfirmResponse>();
        payload.Should().NotBeNull();
        payload!.BookingId.Should().Be(bookingId);
        payload.TotalCost.Should().Be(33m);
        payload.SeatLabels.Should().ContainInOrder(["A1", "B2"]);
    }

    [Fact]
    public async Task GetSeatsForMovie_ShowtimeMovieMismatch_Returns404()
    {
        var requestedMovieId = Guid.NewGuid();
        var actualMovieId = Guid.NewGuid();
        var sid = Guid.NewGuid();

        _fixture.MovieRepositoryMock.Setup(m => m.GetShowtimeDetailsAsync(sid))
            .ReturnsAsync(new ShowtimeDetailsDto(
                sid,
                actualMovieId,
                "Film",
                "Hall",
                DateTime.UtcNow,
                11m));

        var client = _fixture.Factory.CreateClient();
        using var resp = await client.GetAsync("/movies/" + requestedMovieId + "/showtimes/" + sid + "/seats");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSeatsForMovie_Valid_Returns48SeatManifestWithAvailability()
    {
        var movieId = Guid.NewGuid();
        var sid = Guid.NewGuid();

        _fixture.MovieRepositoryMock.Setup(m => m.GetShowtimeDetailsAsync(sid))
            .ReturnsAsync(new ShowtimeDetailsDto(
                sid,
                movieId,
                "Film",
                "Hall",
                DateTime.UtcNow,
                11m));

        _fixture.BookingRepositoryMock.Setup(b => b.GetUnavailableSeatLabelsAsync(sid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "D2" });

        var client = _fixture.Factory.CreateClient();
        using var resp = await client.GetAsync("/movies/" + movieId + "/showtimes/" + sid + "/seats");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<ShowtimeSeatsResponse>();
        dto.Should().NotBeNull();
        dto!.Seats.Should().HaveCount(48);
        dto.Seats.Single(s => s.Label == "D2").Available.Should().BeFalse();
        dto.Seats.Where(s => s.Label != "D2").Should().OnlyContain(s => s.Available);
    }

    [Fact]
    public async Task PostReserveMovieShowtime_NoBearer_Returns401()
    {
        var body = new ReserveSeatsRequest { SeatLabels = ["A1"] };
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        using var response = await client.PostAsJsonAsync(
            "/movies/" + Guid.NewGuid() + "/showtimes/" + Guid.NewGuid() + "/reserve",
            body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostReserveMovieShowtime_ValidJwt_CreatesBooking()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var sid = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        _fixture.MovieRepositoryMock.Setup(m => m.GetShowtimeDetailsAsync(sid))
            .ReturnsAsync(new ShowtimeDetailsDto(
                sid,
                movieId,
                "Film",
                "Hall",
                DateTime.UtcNow,
                11m));

        _fixture.BookingRepositoryMock.Setup(
                b => b.TryCreateConfirmedBookingAsync(
                    userId,
                    sid,
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((bookingId, 33m, (string?)null));

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var principal = new WebUser
        {
            Id = userId,
            Email = "reserve-movie-route@example.com",
            PasswordHash = "",
            Role = "customer",
            IsActive = true
        };
        var (token, _) = jwt.CreateAccessToken(principal);

        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new ReserveSeatsRequest { SeatLabels = [" B2 ", "A1"] };

        using var response = await client.PostAsJsonAsync(
            "/movies/" + movieId + "/showtimes/" + sid + "/reserve",
            body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<CheckoutConfirmResponse>();
        payload.Should().NotBeNull();
        payload!.BookingId.Should().Be(bookingId);
        payload.TotalCost.Should().Be(33m);
        payload.SeatLabels.Should().ContainInOrder(["A1", "B2"]);
    }

    [Fact]
    public async Task GetMyBookings_NoBearer_Returns401()
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        using var resp = await client.GetAsync("/me/bookings");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyBookings_ValidJwt_ReturnsBookingsPayload()
    {
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var starts = DateTime.UtcNow;

        _fixture.BookingRepositoryMock.Setup(
                b => b.ListConfirmedBookingsForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MyBookingItem>
            {
                new()
                {
                    BookingId = bookingId,
                    MovieTitle = "Test Film",
                    StartsAt = starts,
                    SeatLabels = ["A1", "B2"],
                },
            });

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var principal = new WebUser
        {
            Id = userId,
            Email = "bookings-me@example.com",
            PasswordHash = "",
            Role = "customer",
            IsActive = true
        };
        var (token, _) = jwt.CreateAccessToken(principal);

        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await client.GetAsync("/me/bookings");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<MyBookingItem>>();
        list.Should().NotBeNull();
        list!.Should().ContainSingle();
        list[0].BookingId.Should().Be(bookingId);
        list[0].MovieTitle.Should().Be("Test Film");
        list[0].SeatLabels.Should().BeEquivalentTo(new[] { "A1", "B2" });
    }
}
