using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public sealed class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}
