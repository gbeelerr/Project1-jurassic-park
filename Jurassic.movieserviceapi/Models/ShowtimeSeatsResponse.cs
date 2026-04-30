using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

/// <summary>Seat manifest for one showtime: every seat plus availability from DB + holds.</summary>
public sealed class ShowtimeSeatsResponse
{
    [JsonPropertyName("movie_id")]
    public Guid MovieId { get; set; }

    [JsonPropertyName("showtime_id")]
    public Guid ShowtimeId { get; set; }

    [JsonPropertyName("movie_title")]
    public string MovieTitle { get; set; } = "";

    [JsonPropertyName("screen_name")]
    public string ScreenName { get; set; } = "";

    [JsonPropertyName("starts_at")]
    public DateTime StartsAt { get; set; }

    [JsonPropertyName("base_price")]
    public decimal BasePrice { get; set; }

    [JsonPropertyName("seats")]
    public List<ShowtimeSeatItem> Seats { get; set; } = new();

    /// <summary>Labels that cannot be booked (sold or held)—denormalized for simple clients.</summary>
    [JsonPropertyName("unavailable_seat_labels")]
    public List<string> UnavailableSeatLabels { get; set; } = new();
}
