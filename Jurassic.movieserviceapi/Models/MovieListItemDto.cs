using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public record MovieListItemDto(
    [property: JsonPropertyName("movie_id")] Guid MovieId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("duration_mins")] int? DurationMins,
    [property: JsonPropertyName("rating")] string? Rating,
    [property: JsonPropertyName("status")] string Status
);
