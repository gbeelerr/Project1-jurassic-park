using System.Text.Json;
using Dapper;
using Jurassic.movieserviceapi.Models;
using Npgsql;

namespace Jurassic.movieserviceapi.Repositories;

public class MovieRepository : IMovieRepository
{
    private sealed class ShowtimeDetailsRow
    {
        public Guid ShowtimeId { get; init; }
        public Guid MovieId { get; init; }
        public string MovieTitle { get; init; } = "";
        public string ScreenName { get; init; } = "";
        public DateTime StartsAt { get; init; }
        public decimal BasePrice { get; init; }
    }

    private sealed class NowPlayingMovieRow
    {
        public Guid MovieId { get; init; }
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string Rating { get; init; } = "";
        public int DurationMins { get; init; }
        public string PosterUrl { get; init; } = "";
        public Guid ScreenId { get; init; }
        public string ScreenName { get; init; } = "";
        public string ScreenType { get; init; } = "";
        public string ShowtimesJson { get; init; } = "[]";
    }

    private readonly string _connectionString;

    public MovieRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IEnumerable<NowPlayingMovieDto>> GetNowPlayingAsync(DateTime? date = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        // The query you provided
        const string sql = @"
            SELECT 
                m.id AS MovieId,
                m.title AS Title,
                m.description AS Description,
                m.rating AS Rating,
                m.duration_mins AS DurationMins,
                m.poster_url AS PosterUrl,
                sc.id AS ScreenId,
                sc.name AS ScreenName,
                sc.screen_type::text AS ScreenType,
                COALESCE(
                    jsonb_agg(
                    jsonb_build_object(
                        'showtime_id', st.id,
                        'starts_at', st.starts_at,
                        'base_price', st.base_price
                    ) ORDER BY st.starts_at ASC
                    )::text,
                    '[]'
                ) AS ShowtimesJson
            FROM movies m
            JOIN showtimes st ON m.id = st.movie_id
            JOIN screens sc ON st.screen_id = sc.id
            WHERE m.status = 'now_showing'
              AND st.is_cancelled = false
              AND (
                    (@Date IS NULL AND DATE(st.starts_at AT TIME ZONE 'UTC') >= CURRENT_DATE)
                 OR (@Date IS NOT NULL AND DATE(st.starts_at AT TIME ZONE 'UTC') = (@Date AT TIME ZONE 'UTC')::date)
              )
            GROUP BY 
                m.id, m.title, m.description, m.rating, m.duration_mins, m.poster_url,
                sc.id, sc.name, sc.screen_type
            ORDER BY 
                m.title ASC, 
                sc.name ASC;";

        var rows = await connection.QueryAsync<NowPlayingMovieRow>(sql, new { Date = date });

        return rows.Select(row =>
        {
            var showtimes = !string.IsNullOrWhiteSpace(row.ShowtimesJson)
                ? JsonSerializer.Deserialize<List<ShowtimeDetail>>(row.ShowtimesJson) ?? new List<ShowtimeDetail>()
                : new List<ShowtimeDetail>();

            return new NowPlayingMovieDto(
                row.MovieId,
                row.Title,
                row.Description,
                row.Rating,
                row.DurationMins,
                row.PosterUrl,
                row.ScreenId,
                row.ScreenName,
                row.ScreenType,
                showtimes
            );
        });
    }

    public async Task<ShowtimeDetailsDto?> GetShowtimeDetailsAsync(Guid showtimeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        const string sql = @"
            SELECT
                st.id AS ShowtimeId,
                m.id AS MovieId,
                m.title AS MovieTitle,
                sc.name AS ScreenName,
                st.starts_at AS StartsAt,
                st.base_price AS BasePrice
            FROM showtimes st
            JOIN movies m ON m.id = st.movie_id
            JOIN screens sc ON sc.id = st.screen_id
            WHERE st.id = @ShowtimeId
              AND st.is_cancelled = false
            LIMIT 1;";

        var row = await connection.QuerySingleOrDefaultAsync<ShowtimeDetailsRow>(sql, new { ShowtimeId = showtimeId });
        if (row is null)
        {
            return null;
        }

        return new ShowtimeDetailsDto(
            row.ShowtimeId,
            row.MovieId,
            row.MovieTitle,
            row.ScreenName,
            row.StartsAt,
            row.BasePrice
        );
    }
}
