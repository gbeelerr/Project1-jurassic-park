using System.Security.Claims;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Repositories;
using Jurassic.movieserviceapi.Utilities;
using Microsoft.AspNetCore.Authentication;

namespace Jurassic.movieserviceapi.Http;

internal static class SeatReservationHandlers
{
    /// <returns><see cref="Results"/> IResult variants.</returns>
    public static async Task<IResult> GetSeatsForMovieShowtimeAsync(
        Guid movieId,
        Guid showtimeId,
        IMovieRepository movieRepository,
        IBookingRepository bookingRepository,
        CancellationToken cancellationToken)
    {
        var details = await movieRepository.GetShowtimeDetailsAsync(showtimeId);
        if (details is null || details.MovieId != movieId)
        {
            return Results.NotFound();
        }

        var unavailableNormalized = await BuildUnavailableNormalizedSet(bookingRepository, showtimeId, cancellationToken);

        var seatItems = new List<ShowtimeSeatItem>();
        foreach (var label in TheatreSeatLayout.AllSeatLabels())
        {
            var normalized = SeatLabelNormalizer.Normalize(label);
            seatItems.Add(new ShowtimeSeatItem
            {
                Label = normalized,
                Available = !unavailableNormalized.Contains(normalized)
            });
        }

        var response = new ShowtimeSeatsResponse
        {
            MovieId = details.MovieId,
            ShowtimeId = details.ShowtimeId,
            MovieTitle = details.MovieTitle,
            ScreenName = details.ScreenName,
            StartsAt = details.StartsAt,
            BasePrice = details.BasePrice,
            Seats = seatItems,
            UnavailableSeatLabels = unavailableNormalized.OrderBy(s => s, StringComparer.Ordinal).ToList(),
        };

        return Results.Ok(response);
    }

    public static Task<IResult> GetSeatsForShowtimeAnonymousAsync(
        Guid showtimeId,
        IMovieRepository movieRepository,
        IBookingRepository bookingRepository,
        CancellationToken cancellationToken) =>
        GetSeatsWithoutMovieGateAsync(showtimeId, movieRepository, bookingRepository, cancellationToken);

    private static async Task<IResult> GetSeatsWithoutMovieGateAsync(
        Guid showtimeId,
        IMovieRepository movieRepository,
        IBookingRepository bookingRepository,
        CancellationToken cancellationToken)
    {
        var details = await movieRepository.GetShowtimeDetailsAsync(showtimeId);
        if (details is null)
        {
            return Results.NotFound();
        }

        return await BuildOkFromDetails(details, bookingRepository, showtimeId, cancellationToken);
    }

    private static async Task<IResult> BuildOkFromDetails(
        ShowtimeDetailsDto details,
        IBookingRepository bookingRepository,
        Guid showtimeId,
        CancellationToken cancellationToken)
    {
        var unavailableNormalized = await BuildUnavailableNormalizedSet(bookingRepository, showtimeId, cancellationToken);

        var seatItems = new List<ShowtimeSeatItem>();
        foreach (var label in TheatreSeatLayout.AllSeatLabels())
        {
            var normalized = SeatLabelNormalizer.Normalize(label);
            seatItems.Add(new ShowtimeSeatItem
            {
                Label = normalized,
                Available = !unavailableNormalized.Contains(normalized),
            });
        }

        var response = new ShowtimeSeatsResponse
        {
            MovieId = details.MovieId,
            ShowtimeId = details.ShowtimeId,
            MovieTitle = details.MovieTitle,
            ScreenName = details.ScreenName,
            StartsAt = details.StartsAt,
            BasePrice = details.BasePrice,
            Seats = seatItems,
            UnavailableSeatLabels = unavailableNormalized.OrderBy(s => s, StringComparer.Ordinal).ToList(),
        };

        return Results.Ok(response);
    }

    private static async Task<HashSet<string>> BuildUnavailableNormalizedSet(
        IBookingRepository bookingRepository,
        Guid showtimeId,
        CancellationToken cancellationToken)
    {
        var list = await bookingRepository.GetUnavailableSeatLabelsAsync(showtimeId, cancellationToken);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var l in list)
        {
            if (!string.IsNullOrWhiteSpace(l))
            {
                set.Add(SeatLabelNormalizer.Normalize(l));
            }
        }

        return set;
    }

    /// <remarks>Validated showtime belongs to route movie when movieId scoped route is used.</remarks>
    public static async Task<IResult> ReserveSeatsForMovieShowtimeAsync(
        ClaimsPrincipal principal,
        Guid movieId,
        Guid showtimeId,
        ReserveSeatsRequest body,
        IMovieRepository movieRepository,
        IBookingRepository bookingRepository,
        CancellationToken cancellationToken)
    {
        var userTxt = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (body is null || body.SeatLabels is null || body.SeatLabels.Count == 0)
        {
            return Results.BadRequest();
        }

        return await TryReserveInnerAsync(userTxt, movieId, showtimeId, body.SeatLabels, movieRepository,
            bookingRepository, cancellationToken);
    }

    public static async Task<IResult> ReserveSeatsCheckoutStyleAsync(
        ClaimsPrincipal principal,
        CheckoutConfirmRequest body,
        IMovieRepository movieRepository,
        IBookingRepository bookingRepository,
        CancellationToken cancellationToken)
    {
        var userTxt = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (body is null || body.SeatLabels is null || body.SeatLabels.Count == 0 || body.ShowtimeId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        return await TryReserveInnerAsync(userTxt, null, body.ShowtimeId, body.SeatLabels, movieRepository,
            bookingRepository, cancellationToken);
    }

    /// <returns>Ok with checkout-style response shape.</returns>
    private static async Task<IResult> TryReserveInnerAsync(
        string? userTxt,
        Guid? expectedMovieId,
        Guid showtimeId,
        List<string>? seatLabels,
        IMovieRepository movieRepository,
        IBookingRepository bookingRepository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userTxt) || !Guid.TryParse(userTxt, out var webUserId))
        {
            return Results.Unauthorized();
        }

        if (seatLabels is null || seatLabels.Count == 0 || showtimeId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var detail = await movieRepository.GetShowtimeDetailsAsync(showtimeId);
        if (detail is null)
        {
            return Results.NotFound();
        }

        if (expectedMovieId is not null && detail.MovieId != expectedMovieId)
        {
            return Results.NotFound();
        }

        var (bookingId, total, failure) =
            await bookingRepository.TryCreateConfirmedBookingAsync(webUserId, showtimeId, seatLabels, cancellationToken);

        if (bookingId is null)
        {
            return failure switch
            {
                "conflict" => Results.Conflict(),
                "held" or "invalid" => Results.BadRequest(),
                "not_found" => Results.NotFound(),
                _ => TypedResults.Problem(title: "Unable to complete booking", statusCode: StatusCodes.Status500InternalServerError),
            };
        }

        var orderedSeats = seatLabels
            .Where(static l => !string.IsNullOrWhiteSpace(l))
            .Select(SeatLabelNormalizer.Normalize)
            .Where(static l => l.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static l => l, StringComparer.Ordinal)
            .ToArray();

        return Results.Ok(new CheckoutConfirmResponse
        {
            BookingId = bookingId.Value,
            TotalCost = total,
            SeatLabels = orderedSeats,
        });
    }
}

