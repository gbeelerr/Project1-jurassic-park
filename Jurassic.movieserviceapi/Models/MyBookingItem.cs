using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

/// <summary>One confirmed booking for the authenticated user.</summary>
public sealed class MyBookingItem
{
    [JsonPropertyName("booking_id")]
    public Guid BookingId { get; set; }

    [JsonPropertyName("movie_title")]
    public string MovieTitle { get; set; } = "";

    [JsonPropertyName("starts_at")]
    public DateTime StartsAt { get; set; }

    [JsonPropertyName("seat_labels")]
    public List<string> SeatLabels { get; set; } = new();
}
