using System.Text.Json;
using Dapper;
using Jurassic.movieserviceapi.Models;
using Npgsql;

namespace Jurassic.movieserviceapi.Repositories;

public class MovieRepository : IMovieRepository
{
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

    public async Task<IEnumerable<NowPlayingMovieDto>> GetNowPlayingAsync()
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
              AND DATE(st.starts_at AT TIME ZONE 'UTC') >= CURRENT_DATE
            GROUP BY 
                m.id, m.title, m.description, m.rating, m.duration_mins, m.poster_url,
                sc.id, sc.name, sc.screen_type
            ORDER BY 
                m.title ASC, 
                sc.name ASC;";

        var rows = await connection.QueryAsync<NowPlayingMovieRow>(sql);

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
}
