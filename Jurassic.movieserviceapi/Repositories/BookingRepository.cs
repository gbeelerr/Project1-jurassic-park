using System.Globalization;
using Dapper;
using Jurassic.movieserviceapi.Utilities;
using Jurassic.movieserviceapi.Models;
using Npgsql;

namespace Jurassic.movieserviceapi.Repositories;

/// <summary>Bookings recorded in jurassic_api (bookings/tickets tables). Mirrors school seat-map labels like A1.</summary>
public sealed class BookingRepository : IBookingRepository
{
    private static readonly HashSet<string> LayoutHeldSeatLabels =
    [
        "B4", "C6", "E2"
    ];

    private readonly string _connectionString;

    public BookingRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IReadOnlyList<string>> GetUnavailableSeatLabelsAsync(
        Guid showtimeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = """
                             SELECT DISTINCT (upper(trim(s.row_label)) || s.seat_number::text)
                             FROM tickets t
                             INNER JOIN bookings b ON b.id = t.booking_id
                             INNER JOIN seats s ON s.id = t.seat_id
                             WHERE b.showtime_id = @ShowtimeId
                               AND b.status = 'confirmed'::booking_status;
                             """;

        var sold = await connection.QueryAsync<string>(
            new CommandDefinition(sql, new { ShowtimeId = showtimeId }, cancellationToken: cancellationToken));
        var set = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var label in LayoutHeldSeatLabels)
        {
            set.Add(SeatLabelNormalizer.Normalize(label));
        }

        foreach (var s in sold)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                set.Add(s.Trim());
            }
        }

        return set.ToArray();
    }

    public async Task<IReadOnlyList<MyBookingItem>> ListConfirmedBookingsForUserAsync(
        Guid webUserId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string bookingsSql = """
                                   SELECT b.id AS BookingId,
                                          m.title AS MovieTitle,
                                          st.starts_at AS StartsAt
                                   FROM bookings b
                                   INNER JOIN showtimes st ON st.id = b.showtime_id
                                   INNER JOIN movies m ON m.id = st.movie_id
                                   WHERE b.user_id = @UserId
                                     AND b.status = 'confirmed'::booking_status
                                   ORDER BY st.starts_at DESC NULLS LAST, b.created_at DESC;
                                   """;

        var bookings = (
            await connection.QueryAsync<MyBookingSummaryRow>(
                new CommandDefinition(
                    bookingsSql,
                    new { UserId = webUserId },
                    cancellationToken: cancellationToken))).ToList();

        if (bookings.Count == 0)
        {
            return [];
        }

        var bookingIds = bookings.Select(static b => b.BookingId).ToArray();

        const string seatsSql = """
                                SELECT t.booking_id AS BookingId,
                                       (upper(trim(s.row_label)) || s.seat_number::text) AS Label
                                FROM tickets t
                                INNER JOIN seats s ON s.id = t.seat_id
                                WHERE t.booking_id = ANY(@BookingIds::uuid[])
                                ORDER BY Label;
                                """;

        var seatRows = (
            await connection.QueryAsync<MyBookingSeatRow>(
                new CommandDefinition(
                    seatsSql,
                    new { BookingIds = bookingIds },
                    cancellationToken: cancellationToken))).ToList();

        var seatsByBooking = seatRows
            .GroupBy(r => r.BookingId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => SeatLabelNormalizer.Normalize(x.Label))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal)
                    .ToList());

        List<MyBookingItem> items = [];
        foreach (var b in bookings)
        {
            seatsByBooking.TryGetValue(b.BookingId, out var seats);
            items.Add(new MyBookingItem
            {
                BookingId = b.BookingId,
                MovieTitle = b.MovieTitle,
                StartsAt = b.StartsAt,
                SeatLabels = seats ?? [],
            });
        }

        return items;
    }

    public async Task<(Guid? BookingId, decimal TotalCost, string? FailureReason)> TryCreateConfirmedBookingAsync(
        Guid webUserId,
        Guid showtimeId,
        IReadOnlyList<string> seatLabels,
        CancellationToken cancellationToken = default)
    {
        var normalized = seatLabels
            .Select(SeatLabelNormalizer.Normalize)
            .Where(static l => l.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            return (null, 0m, "invalid");
        }

        foreach (var l in normalized)
        {
            if (LayoutHeldSeatLabels.Contains(l, StringComparer.Ordinal))
            {
                return (null, 0m, "held");
            }
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        var lockPair = StableAdvisoryPair(showtimeId);
        await connection.ExecuteAsync(
            new CommandDefinition(
                "SELECT pg_advisory_xact_lock(@A, @B);",
                new { A = lockPair.A, B = lockPair.B },
                transaction: tx,
                cancellationToken: cancellationToken));

        const string showtimeSql = """
                                   SELECT screen_id AS ScreenId, base_price AS BasePrice
                                   FROM showtimes
                                   WHERE id = @Sid AND NOT is_cancelled;
                                   """;

        var showtimeRow = await connection.QuerySingleOrDefaultAsync<ShowtimeRow>(
            new CommandDefinition(showtimeSql, new { Sid = showtimeId }, transaction: tx, cancellationToken: cancellationToken));

        if (showtimeRow is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return (null, 0m, "not_found");
        }

        const string seatsSql = """
                                SELECT id AS SeatId,
                                       (upper(trim(row_label)) || seat_number::text) AS Label
                                FROM seats
                                WHERE screen_id = @ScreenId
                                  AND is_active;
                                """;

        var seatRows = (await connection.QueryAsync<SeatIdRow>(
            new CommandDefinition(
                seatsSql,
                new { showtimeRow.ScreenId },
                transaction: tx,
                cancellationToken: cancellationToken))).ToList();

        var byLabel = seatRows.GroupBy(r => r.Label, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().SeatId, StringComparer.Ordinal);
        var seatIds = new List<Guid>(normalized.Length);
        foreach (var label in normalized)
        {
            if (!byLabel.TryGetValue(label, out var sid))
            {
                await tx.RollbackAsync(cancellationToken);
                return (null, 0m, "invalid");
            }

            seatIds.Add(sid);
        }

        const string soldOverlapSql = """
                                      SELECT COUNT(*)::int
                                      FROM tickets t
                                      INNER JOIN bookings b ON b.id = t.booking_id
                                      WHERE b.showtime_id = @ShowtimeId
                                        AND b.status = 'confirmed'::booking_status
                                        AND t.seat_id = ANY(@SeatIds::uuid[]);
                                      """;

        var overlapCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                soldOverlapSql,
                new { ShowtimeId = showtimeId, SeatIds = seatIds.ToArray() },
                transaction: tx,
                cancellationToken: cancellationToken));

        if (overlapCount > 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return (null, 0m, "conflict");
        }

        var unitPrice = showtimeRow.BasePrice;
        var ticketsCost = decimal.Multiply(unitPrice, normalized.Length);
        var bookingId = Guid.NewGuid();

        const string bookingSql = """
                                  INSERT INTO bookings (id, user_id, showtime_id, status, tickets_cost, addons_cost, total_cost, confirmed_at, updated_at)
                                  VALUES (@Id, @UserId, @ShowtimeId, 'confirmed'::booking_status, @TicketsCost, 0, @TotalCost, NOW(), NOW());
                                  """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                bookingSql,
                new
                {
                    Id = bookingId,
                    UserId = webUserId,
                    ShowtimeId = showtimeId,
                    TicketsCost = ticketsCost,
                    TotalCost = ticketsCost
                },
                transaction: tx,
                cancellationToken: cancellationToken));

        foreach (var seatId in seatIds)
        {
            const string ticketSql = """
                                     INSERT INTO tickets (booking_id, seat_id, ticket_type, unit_price, qr_code)
                                     VALUES (@BookingId, @SeatId, 'adult'::ticket_type, @UnitPrice, @Qr);
                                     """;
            await connection.ExecuteAsync(
                new CommandDefinition(
                    ticketSql,
                    new
                    {
                        BookingId = bookingId,
                        SeatId = seatId,
                        UnitPrice = unitPrice,
                        Qr = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));
        }

        await tx.CommitAsync(cancellationToken);
        return (bookingId, ticketsCost, null);
    }

    private sealed record ShowtimeRow(Guid ScreenId, decimal BasePrice);

    private sealed record SeatIdRow(Guid SeatId, string Label);

    private sealed record MyBookingSummaryRow(Guid BookingId, string MovieTitle, DateTime StartsAt);

    private sealed record MyBookingSeatRow(Guid BookingId, string Label);

    /// <summary>Two-int advisory key derived from GUID (fine for school-scale locking).</summary>
    private static (int A, int B) StableAdvisoryPair(Guid g)
    {
        Span<byte> b = stackalloc byte[16];
        g.TryWriteBytes(b);
        var hi = BitConverter.ToInt32(b[..4]);
        var lo = BitConverter.ToInt32(b.Slice(8, 4));
        return (hi, lo);
    }
}
