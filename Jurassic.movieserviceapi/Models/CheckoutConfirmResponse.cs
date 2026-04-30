using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public sealed class CheckoutConfirmResponse
{
    [JsonPropertyName("booking_id")]
    public Guid BookingId { get; set; }

    [JsonPropertyName("total_cost")]
    public decimal TotalCost { get; set; }

    [JsonPropertyName("seat_labels")]
    public IReadOnlyList<string> SeatLabels { get; set; } = Array.Empty<string>();
}
