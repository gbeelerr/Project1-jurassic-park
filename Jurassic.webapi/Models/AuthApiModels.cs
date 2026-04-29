using System.Text.Json.Serialization;

namespace BlazorApp1.Models;

public sealed class AuthLoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}

public sealed class AuthLoginResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresInSeconds { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}

public sealed class AuthRegisterRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}
