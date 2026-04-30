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

    public class ShowtimeDetailsResponse
    {
        [JsonPropertyName("showtime_id")]
        public Guid ShowtimeId { get; set; }

        [JsonPropertyName("movie_id")]
        public Guid MovieId { get; set; }

        [JsonPropertyName("movie_title")]
        public string MovieTitle { get; set; } = "";

        [JsonPropertyName("screen_name")]
        public string ScreenName { get; set; } = "";

        [JsonPropertyName("starts_at")]
        public DateTime StartsAt { get; set; }

        [JsonPropertyName("base_price")]
        public decimal BasePrice { get; set; }
    }

    public sealed class SeatAvailabilityDto
    {
        [JsonPropertyName("showtime_id")]
        public Guid ShowtimeId { get; set; }

        [JsonPropertyName("unavailable_seat_labels")]
        public List<string> UnavailableSeatLabels { get; set; } = new();
    }

    public sealed class CheckoutConfirmApiResponse
    {
        [JsonPropertyName("booking_id")]
        public Guid BookingId { get; set; }

        [JsonPropertyName("total_cost")]
        public decimal TotalCost { get; set; }

        [JsonPropertyName("seat_labels")]
        public List<string> SeatLabels { get; set; } = new();
    }

    public sealed class ShowtimeSeatItemDto
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("available")]
        public bool Available { get; set; }
    }

    public sealed class ShowtimeSeatsApiDto
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
        public List<ShowtimeSeatItemDto> Seats { get; set; } = new();
    }

    public sealed class MyBookingItemDto
    {
        [JsonPropertyName("booking_id")]
        public Guid BookingId { get; set; }

        [JsonPropertyName("movie_title")]
        public string MovieTitle { get; set; } = "";

        [JsonPropertyName("starts_at")]
        public DateTime StartsAt { get; set; }

        [JsonPropertyName("seat_labels")]
        public List<string> SeatLabels { get; set; } = new();
    }
}