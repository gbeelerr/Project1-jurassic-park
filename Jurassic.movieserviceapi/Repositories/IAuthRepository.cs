using Jurassic.movieserviceapi.Models;

namespace Jurassic.movieserviceapi.Repositories;

/// <summary>Data access for jurassic_web: <c>users</c> (credentials) and <c>sessions</c> (refresh tokens).</summary>
public interface IAuthRepository
{
    Task<WebUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task InsertSessionAsync(
        Guid userId,
        string refreshToken,
        DateTimeOffset expiresAtUtc,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task InsertUserAsync(
        Guid id,
        string email,
        string? displayName,
        string passwordHash,
        CancellationToken cancellationToken = default);
}
