using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public sealed class InfinityAuthRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}

public sealed class InfinityRegisterRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

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

public sealed class InfinityReviewResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("date")]
    public string Date { get; set; } = "";

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = "";

    /// <summary>Infinity Web App (8082) often exposes star rating as a separate field, not only inside <c>comment</c>.</summary>
    [JsonPropertyName("stars")]
    public int? Stars { get; set; }

    [JsonPropertyName("starRating")]
    public int? StarRating { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("isOwner")]
    public bool IsOwner { get; set; }
}

public sealed class InfinityReviewUpsertRequest
{
    [JsonPropertyName("stars")]
    public int Stars { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
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

public sealed class MovieRatingSummaryItem
{
    [JsonPropertyName("movie_id")]
    public Guid MovieId { get; set; }

    [JsonPropertyName("avg_stars")]
    public decimal AvgStars { get; set; }

    [JsonPropertyName("review_count")]
    public int ReviewCount { get; set; }
}

public sealed class InfinitySubmitReviewRequest
{
    [JsonPropertyName("attractionId")]
    public Guid AttractionId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public sealed class InfinityEditReviewRequest
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public sealed class InfinityAttractionRatingResponse
{
    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("average")]
    public double? Average { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class InfinityRateRequest
{
    [JsonPropertyName("attractionId")]
    public Guid AttractionId { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }
}
