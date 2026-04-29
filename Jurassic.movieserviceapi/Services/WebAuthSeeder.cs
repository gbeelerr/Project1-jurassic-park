using Dapper;
using Jurassic.movieserviceapi.Models;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Jurassic.movieserviceapi.Services;

/// <summary>Ensures at least one user exists in jurassic_web for local/demo login (only when enabled).</summary>
public sealed class WebAuthSeeder
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebAuthSeeder> _logger;
    private readonly PasswordHasher<WebUser> _passwordHasher = new();

    public WebAuthSeeder(IConfiguration configuration, ILogger<WebAuthSeeder> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("WebAuthSeed:Enabled", true))
        {
            return;
        }

        var connectionString = _configuration.GetConnectionString("WebConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("WebConnection not configured; skipping web auth seed.");
            return;
        }

        var email = _configuration["WebAuthSeed:DemoEmail"] ?? "demo@jurassic.test";
        var password = _configuration["WebAuthSeed:DemoPassword"] ?? "Password123!";
        var displayName = _configuration["WebAuthSeed:DemoDisplayName"] ?? "Demo User";

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var count = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT COUNT(*)::int FROM users;", cancellationToken: cancellationToken));

            if (count > 0)
            {
                return;
            }

            var userId = Guid.NewGuid();
            var principal = new WebUser
            {
                Id = userId,
                Email = email,
                PasswordHash = "",
                Role = "customer",
                IsActive = true
            };
            var hash = _passwordHasher.HashPassword(principal, password);

            const string insertSql = """
                                     INSERT INTO users (id, email, display_name, password_hash, role, is_active)
                                     VALUES (@Id, @Email, @DisplayName, @PasswordHash, 'customer'::user_role, true);
                                     """;

            await connection.ExecuteAsync(new CommandDefinition(
                insertSql,
                new { Id = userId, Email = email, DisplayName = displayName, PasswordHash = hash },
                cancellationToken: cancellationToken));

            _logger.LogInformation("Seeded demo web user {Email} (WebAuthSeed).", email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Web auth seed skipped (cannot connect or initialize jurassic_web).");
        }
    }
}
