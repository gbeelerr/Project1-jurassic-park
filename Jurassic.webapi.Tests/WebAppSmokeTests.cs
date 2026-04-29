using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jurassic.webapi.Tests;

public class WebAppSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebAppSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("MovieApi:BaseUrl", "http://127.0.0.1:9");
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
        });
    }

    [Fact]
    public async Task GetHome_ReturnsOk_AndContainsBranding()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Jurassic Park Cinema");
    }

    [Fact]
    public async Task GetAccount_ReturnsOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Sign in or create an account");
    }

    [Fact]
    public async Task GetUnknownRoute_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/this-route-does-not-exist-xyz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
