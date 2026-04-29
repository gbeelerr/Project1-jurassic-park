namespace Jurassic.movieserviceapi.Models;

public sealed class WebUser
{
    public Guid Id { get; init; }
    public string Email { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public string Role { get; init; } = "customer";
    public bool IsActive { get; init; }
}
