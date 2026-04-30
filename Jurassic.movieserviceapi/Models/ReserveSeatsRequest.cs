using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public sealed class ReserveSeatsRequest
{
    [JsonPropertyName("seat_labels")]
    public List<string> SeatLabels { get; set; } = new();
}
