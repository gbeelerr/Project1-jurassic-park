using Dapper;
using Jurassic.movieserviceapi.Models;
using Npgsql;

namespace Jurassic.movieserviceapi.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly string _connectionString;

    public AuthRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WebConnection")
                            ?? throw new InvalidOperationException(
                                "Connection string 'WebConnection' not found (jurassic_web database).");
    }

    public async Task<WebUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT
                               u.id AS Id,
                               u.email AS Email,
                               u.password_hash AS PasswordHash,
                               u.role::text AS Role,
                               u.is_active AS IsActive
                           FROM users u
                           WHERE lower(trim(u.email)) = lower(trim(@Email))
                           LIMIT 1;
                           """;

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<WebUser>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task InsertSessionAsync(
        Guid userId,
        string refreshToken,
        DateTimeOffset expiresAtUtc,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO sessions (user_id, refresh_token, ip_address, user_agent, expires_at)
                           VALUES (@UserId, @RefreshToken, @IpAddress, @UserAgent, @ExpiresAt);
                           """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                UserId = userId,
                RefreshToken = refreshToken,
                IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? (object?)DBNull.Value : ipAddress,
                UserAgent = userAgent,
                ExpiresAt = expiresAtUtc.UtcDateTime
            },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT EXISTS(
                               SELECT 1 FROM users u WHERE lower(trim(u.email)) = lower(trim(@Email))
                           );
                           """;

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task InsertUserAsync(
        Guid id,
        string email,
        string? displayName,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO users (id, email, display_name, password_hash, role, is_active)
                           VALUES (@Id, @Email, @DisplayName, @PasswordHash, 'customer'::user_role, true);
                           """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, Email = email, DisplayName = displayName, PasswordHash = passwordHash },
            cancellationToken: cancellationToken));
    }
}
