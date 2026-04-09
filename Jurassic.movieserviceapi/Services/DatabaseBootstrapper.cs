using Dapper;
using Npgsql;

namespace Jurassic.movieserviceapi.Services;

public sealed class DatabaseBootstrapper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseBootstrapper> _logger;

    public DatabaseBootstrapper(
        IConfiguration configuration,
        ILogger<DatabaseBootstrapper> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var sqlPath = Path.Combine(AppContext.BaseDirectory, "Data", "bootstrap.sql");
        if (!File.Exists(sqlPath))
        {
            throw new FileNotFoundException("Database bootstrap script was not found.", sqlPath);
        }

        var bootstrapSql = await File.ReadAllTextAsync(sqlPath, cancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(bootstrapSql, cancellationToken: cancellationToken));

        _logger.LogInformation("Database bootstrap completed successfully.");
    }
}
