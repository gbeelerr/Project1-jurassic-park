using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public sealed class RegisterResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
}
