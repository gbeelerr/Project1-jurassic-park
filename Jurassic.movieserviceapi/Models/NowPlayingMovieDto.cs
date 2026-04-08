namespace Jurassic.movieserviceapi.Models;

using System.Text.Json.Serialization;

public record NowPlayingMovieDto(
    Guid MovieId,
    string Title,
    string Description,
    string Rating,
    int DurationMins,
    string PosterUrl,
    Guid ScreenId,
    string ScreenName,
    string ScreenType,
    // This will map from the jsonb_agg result
    List<ShowtimeDetail> Showtimes
);

public record ShowtimeDetail(
    [property: JsonPropertyName("showtime_id")] Guid ShowtimeId,
    [property: JsonPropertyName("starts_at")] DateTime StartsAt,
    [property: JsonPropertyName("base_price")] decimal BasePrice
);
