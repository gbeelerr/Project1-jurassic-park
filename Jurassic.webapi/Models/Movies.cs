using System.Text.Json.Serialization;

namespace BlazorApp1.Models
{
    public class Movie
    {
        [JsonPropertyName("movie_id")]
        public Guid MovieId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("rating")]
        public string Rating { get; set; } = "";

        [JsonPropertyName("duration_mins")]
        public int DurationMins { get; set; }

        [JsonPropertyName("poster_url")]
        public string PosterUrl { get; set; } = "";

        [JsonPropertyName("screen_id")]
        public Guid ScreenId { get; set; }

        [JsonPropertyName("screen_name")]
        public string ScreenName { get; set; } = "";

        [JsonPropertyName("screen_type")]
        public string ScreenType { get; set; } = "";

        [JsonPropertyName("showtimes")]
        public List<ShowtimeDetail> Showtimes { get; set; } = new();
    }

    public class ShowtimeDetail
    {
        [JsonPropertyName("showtime_id")]
        public Guid ShowtimeId { get; set; }

        [JsonPropertyName("starts_at")]
        public DateTime StartsAt { get; set; }

        [JsonPropertyName("base_price")]
        public decimal BasePrice { get; set; }
    }
}