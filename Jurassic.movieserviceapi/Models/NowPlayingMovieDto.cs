namespace Jurassic.movieserviceapi.Models;

using System.Text.Json.Serialization;

public record NowPlayingMovieDto(
    [property: JsonPropertyName("movie_id")] Guid MovieId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("rating")] string Rating,
    [property: JsonPropertyName("duration_mins")] int DurationMins,
    [property: JsonPropertyName("poster_url")] string PosterUrl,
    [property: JsonPropertyName("screen_id")] Guid ScreenId,
    [property: JsonPropertyName("screen_name")] string ScreenName,
    [property: JsonPropertyName("screen_type")] string ScreenType,
    // This will map from the jsonb_agg result
    [property: JsonPropertyName("showtimes")] List<ShowtimeDetail> Showtimes
);

public record ShowtimeDetail(
    [property: JsonPropertyName("showtime_id")] Guid ShowtimeId,
    [property: JsonPropertyName("starts_at")] DateTime StartsAt,
    [property: JsonPropertyName("base_price")] decimal BasePrice
);
