using Jurassic.movieserviceapi.Models;

namespace Jurassic.movieserviceapi.Services;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(WebUser user);
}
