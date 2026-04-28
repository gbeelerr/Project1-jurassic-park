using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public record MoviePosterDto(
    [property: JsonPropertyName("movie_id")] Guid MovieId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("poster_url")] string PosterUrl
);
