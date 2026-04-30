using Jurassic.movieserviceapi.Models;

namespace Jurassic.movieserviceapi.Repositories;

public interface IBookingRepository
{
    Task<IReadOnlyList<string>> GetUnavailableSeatLabelsAsync(Guid showtimeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MyBookingItem>> ListConfirmedBookingsForUserAsync(
        Guid webUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a confirmed booking and tickets. Returns null booking id on failure.
    /// <paramref name="failureReason"/>: <c>conflict</c> (seat taken), <c>invalid</c> (unknown/hold seat), <c>not_found</c> (showtime).
    /// </summary>
    Task<(Guid? BookingId, decimal TotalCost, string? FailureReason)> TryCreateConfirmedBookingAsync(
        Guid webUserId,
        Guid showtimeId,
        IReadOnlyList<string> seatLabels,
        CancellationToken cancellationToken = default);
}
