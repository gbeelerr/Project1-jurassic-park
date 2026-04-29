using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public sealed class RegisterRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}
