using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public sealed class CheckoutConfirmRequest
{
    [JsonPropertyName("showtime_id")]
    public Guid ShowtimeId { get; set; }

    [JsonPropertyName("seat_labels")]
    public List<string> SeatLabels { get; set; } = new();
}
