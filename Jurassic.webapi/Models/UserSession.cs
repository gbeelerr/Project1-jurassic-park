namespace BlazorApp1.Models;

public sealed class UserSession
{
    public string? Email { get; private set; }

    public string? AccessToken { get; private set; }

    public bool IsSignedIn => !string.IsNullOrEmpty(Email);

    public void SetSignedIn(string email, string? accessToken = null)
    {
        Email = email;
        AccessToken = accessToken;
    }

    public void SignOut()
    {
        Email = null;
        AccessToken = null;
    }
}
