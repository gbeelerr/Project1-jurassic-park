using System.Text.Json;
using Dapper;
using Jurassic.movieserviceapi.Models;
using Npgsql;

namespace Jurassic.movieserviceapi.Repositories;

public class MovieRepository : IMovieRepository
{
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
                sc.screen_type AS ScreenType,
                jsonb_agg(
                    jsonb_build_object(
                        'showtime_id', st.id,
                        'starts_at', st.starts_at,
                        'base_price', st.base_price
                    ) ORDER BY st.starts_at ASC
                ) AS ShowtimesJson
            FROM movies m
            JOIN showtimes st ON m.id = st.movie_id
            JOIN screens sc ON st.screen_id = sc.id
            WHERE m.status = 'now_showing'
              AND st.is_cancelled = false
              AND DATE(st.starts_at AT TIME ZONE 'UTC') = '2026-04-05' 
            GROUP BY 
                m.id, 
                sc.id
            ORDER BY 
                m.title ASC, 
                sc.name ASC;";

        // We fetch as dynamic to easily handle the JSON string from jsonb_agg
        var rows = await connection.QueryAsync<dynamic>(sql);

        return rows.Select(row => new NowPlayingMovieDto(
            (Guid)row.movieid,
            (string)row.title,
            (string)row.description,
            (string)row.rating,
            (int)row.durationmins,
            (string)row.posterurl,
            (Guid)row.screenid,
            (string)row.screenname,
            (string)row.screentype,
            row.showtimesjson != null 
                ? JsonSerializer.Deserialize<List<ShowtimeDetail>>(row.showtimesjson.ToString()) ?? new List<ShowtimeDetail>() 
                : new List<ShowtimeDetail>()
        ));
    }
}
