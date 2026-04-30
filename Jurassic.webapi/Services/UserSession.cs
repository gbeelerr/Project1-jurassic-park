namespace BlazorApp1.Services;

/// <summary>Holds auth tokens for the current Blazor Server circuit (in-memory).</summary>
public sealed class UserSession
{
    public event Action? Changed;

    public string? Email { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    /// <summary>Optional app URL captured before navigating to sign in (checkout flow).</summary>
    public string? ReturnAfterSignIn { get; set; }

    public bool IsSignedIn => !string.IsNullOrEmpty(AccessToken);

    /// <summary>Lets layout/nav redraw after tokens change — mutating this object does not re-render siblings by default.</summary>
    public void NotifyChanged() => Changed?.Invoke();
}
