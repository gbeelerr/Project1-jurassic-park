using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public record ShowtimeDetailsDto(
    [property: JsonPropertyName("showtime_id")] Guid ShowtimeId,
    [property: JsonPropertyName("movie_id")] Guid MovieId,
    [property: JsonPropertyName("movie_title")] string MovieTitle,
    [property: JsonPropertyName("screen_name")] string ScreenName,
    [property: JsonPropertyName("starts_at")] DateTime StartsAt,
    [property: JsonPropertyName("base_price")] decimal BasePrice
);
