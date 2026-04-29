namespace BlazorApp1.Services;

/// <summary>Holds auth tokens for the current Blazor Server circuit (in-memory).</summary>
public sealed class UserSession
{
    public string? Email { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public bool IsSignedIn => !string.IsNullOrEmpty(AccessToken);
}
