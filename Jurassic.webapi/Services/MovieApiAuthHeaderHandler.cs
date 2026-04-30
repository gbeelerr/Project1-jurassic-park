using System.Net.Http.Headers;

namespace BlazorApp1.Services;

/// <summary>Adds Bearer token from Blazor circuit session to outgoing movie API requests when present.</summary>
public sealed class MovieApiAuthHeaderHandler(UserSession session) : DelegatingHandler
{
    private readonly UserSession _session = session;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_session.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }
        else
        {
            request.Headers.Authorization = null;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
