using Dapper;
using Npgsql;

namespace Jurassic.movieserviceapi.Repositories;

public sealed class MovieAttractionMapRepository(IConfiguration configuration) : IMovieAttractionMapRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
                                                 ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    public async Task<Guid?> GetAttractionIdAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT attraction_id
                           FROM movie_attraction_map
                           WHERE movie_id = @MovieId
                           LIMIT 1;
                           """;

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Guid?>(
            new CommandDefinition(sql, new { MovieId = movieId }, cancellationToken: cancellationToken));
    }

    public async Task UpsertAsync(Guid movieId, Guid attractionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO movie_attraction_map(movie_id, attraction_id, synced_at)
                           VALUES (@MovieId, @AttractionId, now())
                           ON CONFLICT (movie_id)
                           DO UPDATE SET attraction_id = EXCLUDED.attraction_id, synced_at = now();
                           """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { MovieId = movieId, AttractionId = attractionId },
            cancellationToken: cancellationToken));
    }
}
