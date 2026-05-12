using System.Text.Json.Serialization;

namespace BlazorApp1.Models;

public sealed class InfinityAuthRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}

public sealed class InfinityAuthResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
}

public sealed class MovieRatingSummaryItem
{
    [JsonPropertyName("movie_id")]
    public Guid MovieId { get; set; }

    [JsonPropertyName("avg_stars")]
    public decimal AvgStars { get; set; }

    [JsonPropertyName("review_count")]
    public int ReviewCount { get; set; }
}

public sealed class InfinityReviewItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("date")]
    public string Date { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("stars")]
    public int Stars { get; set; }

    [JsonPropertyName("is_owner")]
    public bool IsOwner { get; set; }
}

public sealed class InfinityMovieRating
{
    [JsonPropertyName("stars")]
    public int Stars { get; set; }

    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    [JsonPropertyName("average")]
    public double Average { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class InfinityReviewUpsertRequest
{
    [JsonPropertyName("stars")]
    public int Stars { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}
