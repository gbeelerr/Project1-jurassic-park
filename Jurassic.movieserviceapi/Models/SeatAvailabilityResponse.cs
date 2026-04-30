using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public sealed class SeatAvailabilityResponse
{
    [JsonPropertyName("showtime_id")]
    public Guid ShowtimeId { get; set; }

    /// <summary>Labels that cannot be selected: already sold + layout-held seats.</summary>
    [JsonPropertyName("unavailable_seat_labels")]
    public IReadOnlyList<string> UnavailableSeatLabels { get; set; } = Array.Empty<string>();
}
