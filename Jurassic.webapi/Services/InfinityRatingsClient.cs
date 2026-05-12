using System.Net.Http.Json;
using BlazorApp1.Models;

namespace BlazorApp1.Services;

public sealed class InfinityRatingsClient(HttpClient http, UserSession session)
{
    private readonly HttpClient _http = http;
    private readonly UserSession _session = session;

    public async Task<List<MovieRatingSummaryItem>> GetRatingsSummaryAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<MovieRatingSummaryItem>>("movies/ratings-summary") ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }
    }

    public async Task<InfinityMovieRating> GetUserRatingAsync(Guid movieId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"movies/{movieId}/rating");
            AddInfinityBearer(request);
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new InfinityMovieRating { Stars = 0, Exists = false };
            }

            return await response.Content.ReadFromJsonAsync<InfinityMovieRating>() ?? new InfinityMovieRating { Stars = 0, Exists = false };
        }
        catch (HttpRequestException)
        {
            return new InfinityMovieRating { Stars = 0, Exists = false };
        }
        catch (TaskCanceledException)
        {
            return new InfinityMovieRating { Stars = 0, Exists = false };
        }
    }

    public async Task<bool> UpsertRatingAsync(Guid movieId, int stars)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"movies/{movieId}/rating");
            AddInfinityBearer(request);
            request.Content = JsonContent.Create(new InfinityMovieRating
            {
                Stars = stars
            });

            using var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<List<InfinityReviewItem>> GetReviewsAsync(Guid movieId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"movies/{movieId}/reviews");
            AddInfinityBearer(request);
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<InfinityReviewItem>>() ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    public async Task<bool> CreateReviewAsync(Guid movieId, string content)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"movies/{movieId}/reviews");
            AddInfinityBearer(request);
            request.Content = JsonContent.Create(new InfinityReviewUpsertRequest
            {
                Stars = 0,
                Content = content
            });

            using var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<bool> UpdateReviewAsync(Guid movieId, Guid reviewId, string content)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"movies/{movieId}/reviews/{reviewId}");
            AddInfinityBearer(request);
            request.Content = JsonContent.Create(new InfinityReviewUpsertRequest
            {
                Stars = 0,
                Content = content
            });

            using var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteReviewAsync(Guid movieId, Guid reviewId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"movies/{movieId}/reviews/{reviewId}");
            AddInfinityBearer(request);
            using var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public async Task<InfinityAuthResponse?> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("infinity/auth/login", new InfinityAuthRequest
        {
            Username = username,
            Password = password
        });

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<InfinityAuthResponse>();
    }

    private void AddInfinityBearer(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_session.InfinityAccessToken))
        {
            request.Headers.Add("X-Infinity-Bearer", _session.InfinityAccessToken);
        }
    }
}
