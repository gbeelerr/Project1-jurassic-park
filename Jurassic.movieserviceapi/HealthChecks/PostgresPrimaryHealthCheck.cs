using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Jurassic.movieserviceapi.HealthChecks;

/// <summary>Verifies the API can reach Postgres using the DefaultConnection string (jurassic_api).</summary>
public sealed class PostgresPrimaryHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresPrimaryHealthCheck> _logger;

    public PostgresPrimaryHealthCheck(IConfiguration configuration, ILogger<PostgresPrimaryHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("Health check: DefaultConnection is not configured.");
            return HealthCheckResult.Unhealthy("Database connection string is missing.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand("SELECT 1;", connection);
            await cmd.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check: Postgres primary database is not reachable.");
            return HealthCheckResult.Unhealthy("Database connection failed.", ex);
        }
    }
}
