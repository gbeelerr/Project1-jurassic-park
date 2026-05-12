using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Options;
using Jurassic.movieserviceapi.Repositories;
using Microsoft.Extensions.Options;

namespace Jurassic.movieserviceapi.Services;

public sealed class InfinityGateway(
    HttpClient httpClient,
    IOptions<InfinityOptions> options,
    IMovieRepository movieRepository,
    IMovieAttractionMapRepository mapRepository)
{
    private static readonly SemaphoreSlim MappingRefreshGate = new(1, 1);
    private static DateTime _lastMappingRefresh = DateTime.MinValue;
    private static IReadOnlyList<MovieRatingSummaryItem> _cachedSummary = [];

    public static void ResetCache()
    {
        _lastMappingRefresh = DateTime.MinValue;
        _cachedSummary = [];
    }

    private static readonly JsonSerializerOptions JsonReadRelaxed = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly InfinityOptions _options = options.Value;
    private readonly IMovieRepository _movieRepository = movieRepository;
    private readonly IMovieAttractionMapRepository _mapRepository = mapRepository;

    public async Task<InfinityAuthResponse?> LoginAsync(InfinityAuthRequest request, CancellationToken cancellationToken = default)
    {
        var uri = BuildWebAppUri("api/auth/login");
        using var response = await _httpClient.PostAsJsonAsync(uri, request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<InfinityAuthResponse>(
            JsonReadRelaxed,
            cancellationToken: cancellationToken);
    }

    public async Task<bool> RegisterAsync(InfinityRegisterRequest request, CancellationToken cancellationToken = default)
    {
        var uri = BuildWebAppUri("api/auth/register");
        using var response = await _httpClient.PostAsJsonAsync(uri, request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<InfinityMovieRating> GetRatingFromWebAppAsync(Guid attractionId, string? bearer, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildWebAppUri($"api/ratings/{attractionId}/mine"));
        if (!string.IsNullOrWhiteSpace(bearer))
        {
            AddBearer(request, bearer);
        }
        try
        {
            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<InfinityAttractionRatingResponse>(JsonReadRelaxed, ct).ConfigureAwait(false);
                return new InfinityMovieRating
                {
                    Stars = data?.Value ?? 0,
                    Exists = data?.Value.HasValue == true,
                    Average = data?.Average ?? 0,
                    Count = data?.Count ?? 0
                };
            }
        }
        catch { }
        return new InfinityMovieRating();
    }

    public async Task<InfinityMovieRating> GetUserRatingAsync(Guid movieId, string? bearer, CancellationToken cancellationToken = default)
    {
        var attractionId = await ResolveAttractionIdAsync(movieId, cancellationToken).ConfigureAwait(false);
        if (attractionId is null) return new InfinityMovieRating { Stars = 0, Exists = false };

        return await GetRatingFromWebAppAsync(attractionId.Value, bearer, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpsertRatingAsync(Guid movieId, int stars, string bearer, CancellationToken cancellationToken = default)
    {
        var attractionId = await ResolveAttractionIdAsync(movieId, cancellationToken).ConfigureAwait(false);
        if (attractionId is null)
        {
            return false;
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, BuildWebAppUri("api/ratings"));
        AddBearer(message, bearer);
        message.Content = JsonContent.Create(new InfinityRateRequest
        {
            AttractionId = attractionId.Value,
            Value = stars
        });

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                // Force a refresh of the summary so the UI sees the new average
                _ = RefreshMappingsAsync(cancellationToken, force: true);
                return true;
            }
        }
        catch (HttpRequestException) { }
        catch (TaskCanceledException) { }

        return false;
    }

    public async Task<IReadOnlyList<InfinityReviewItem>> GetMovieReviewsAsync(Guid movieId, string? bearer, CancellationToken cancellationToken = default)
    {
        var attractionId = await ResolveAttractionIdAsync(movieId, cancellationToken);
        if (attractionId is null)
        {
            return [];
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildWebAppUri($"api/reviews/{attractionId.Value}"));
        AddBearer(request, bearer);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (TaskCanceledException)
        {
            return [];
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var reviews = await response.Content.ReadFromJsonAsync<List<InfinityReviewResponse>>(
                JsonReadRelaxed,
                cancellationToken: cancellationToken) ?? [];
            return reviews
                .Select(MapReviewItem)
                .ToList();
        }
    }

    public async Task<bool> CreateReviewAsync(Guid movieId, string content, string bearer, CancellationToken cancellationToken = default)
    {
        var attractionId = await ResolveAttractionIdAsync(movieId, cancellationToken);
        if (attractionId is null)
        {
            return false;
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, BuildWebAppUri("api/reviews"));
        AddBearer(message, bearer);
        message.Content = JsonContent.Create(new InfinitySubmitReviewRequest 
        { 
            AttractionId = attractionId.Value, 
            Content = content
        });

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
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

    public async Task<bool> UpdateReviewAsync(Guid reviewId, string content, string bearer, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, BuildWebAppUri($"api/reviews/{reviewId}"));
        AddBearer(message, bearer);
        message.Content = JsonContent.Create(new InfinityEditReviewRequest 
        { 
            Content = content
        });

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
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

    public async Task<bool> DeleteReviewAsync(Guid reviewId, string bearer, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, BuildWebAppUri($"api/reviews/{reviewId}"));
        AddBearer(message, bearer);
        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
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

    public async Task<IReadOnlyList<MovieRatingSummaryItem>> GetRatingsSummaryAsync(CancellationToken cancellationToken = default)
    {
        return await RefreshMappingsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Guid?> ResolveAttractionIdAsync(Guid movieId, CancellationToken cancellationToken)
    {
        var mapped = await _mapRepository.GetAttractionIdAsync(movieId, cancellationToken);
        if (mapped.HasValue)
        {
            return mapped;
        }

        await RefreshMappingsAsync(cancellationToken, force: true);
        return await _mapRepository.GetAttractionIdAsync(movieId, cancellationToken);
    }

    private async Task<IReadOnlyList<MovieRatingSummaryItem>> RefreshMappingsAsync(CancellationToken cancellationToken, bool force = false)
    {
        if (!force && DateTime.UtcNow - _lastMappingRefresh < TimeSpan.FromMinutes(10) && _cachedSummary.Count > 0)
        {
            return _cachedSummary;
        }

        await MappingRefreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!force && DateTime.UtcNow - _lastMappingRefresh < TimeSpan.FromMinutes(10) && _cachedSummary.Count > 0)
            {
                return _cachedSummary;
            }

            var movies = (await _movieRepository.GetMoviesAsync())
                .ToDictionary(movie => Normalize(movie.Title), movie => movie.MovieId);

            var token = await GetServiceTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
            {
                return [];
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildWebApiUri("api/Attractions"));
            AddBearer(request, token);
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                return [];
            }
            catch (TaskCanceledException)
            {
                return [];
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    return [];
                }

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }

                var results = new List<MovieRatingSummaryItem>();
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    if (!TryParseAttraction(item, out var attractionId, out var title, out var parkId, out _))
                    {
                        continue;
                    }

                    if (!string.Equals(parkId, _options.JurassicParkId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var key = Normalize(title);
                    if (!movies.TryGetValue(key, out var movieId))
                    {
                        continue;
                    }

                    await _mapRepository.UpsertAsync(movieId, attractionId, cancellationToken).ConfigureAwait(false);

                    // Extract rating data directly from the attraction if present
                    // Enrich with real-time data from WebApp (8082) for more accurate averages
                    var realTimeRating = await GetRatingFromWebAppAsync(attractionId, null, cancellationToken).ConfigureAwait(false);

                    results.Add(new MovieRatingSummaryItem
                    {
                        MovieId = movieId,
                        AvgStars = (decimal)realTimeRating.Average,
                        ReviewCount = realTimeRating.Count
                    });
                }

                _cachedSummary = results;
                _lastMappingRefresh = DateTime.UtcNow;
                return results;
            }
        }
        finally
        {
            MappingRefreshGate.Release();
        }
    }

    private async Task<string?> GetServiceTokenAsync(CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(BuildWebApiUri("dev/token"), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            if (content.StartsWith("{", StringComparison.Ordinal))
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                {
                    return tokenProp.GetString();
                }
            }

            return content.Trim('"');
        }
    }

    private static bool TryParseAttraction(
        JsonElement item,
        out Guid attractionId,
        out string title,
        out string parkId,
        out string category)
    {
        attractionId = Guid.Empty;
        title = "";
        parkId = "";
        category = "";

        var idString = ReadStringInsensitive(item, "id");
        if (!Guid.TryParse(idString, out attractionId))
        {
            return false;
        }

        title = ReadStringInsensitive(item, "name");
        parkId = ReadStringInsensitive(item, "parkId");
        category = ReadStringInsensitive(item, "category");
        return !string.IsNullOrWhiteSpace(title);
    }

    private static string ReadStringInsensitive(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                var value = prop.Value;
                return value.ValueKind == JsonValueKind.String ? (value.GetString() ?? "") : value.ToString();
            }
        }

        return "";
    }

    private static decimal ReadDecimalInsensitive(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.TryGetDecimal(out var val)) return val;
                if (double.TryParse(prop.Value.ToString(), out var d)) return (decimal)d;
            }
        }

        return 0;
    }

    private static void AddBearer(HttpRequestMessage request, string? bearer)
    {
        if (string.IsNullOrWhiteSpace(bearer))
        {
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim());
    }

    private Uri BuildWebApiUri(string relative) => new($"{_options.WebApiBaseUrl.TrimEnd('/')}/{relative.TrimStart('/')}");
    private Uri BuildWebAppUri(string relative) => new($"{_options.WebAppBaseUrl.TrimEnd('/')}/{relative.TrimStart('/')}");

    private static string Normalize(string value)
    {
        return string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    }

    private static InfinityReviewItem MapReviewItem(InfinityReviewResponse review)
    {
        var comment = review.Comment ?? "";
        (int stars, string content) decoded = (0, comment);
        try
        {
            decoded = InfinityReviewContentCodec.Decode(comment);
        }
        catch
        {
            decoded = (0, comment);
        }

        return new InfinityReviewItem
        {
            Id = review.Id,
            Author = review.Author ?? "",
            Date = review.Date ?? "",
            Content = decoded.content,
            Stars = 0, // Stars are not attached to individual reviews per user request
            IsOwner = review.IsOwner
        };
    }
}
