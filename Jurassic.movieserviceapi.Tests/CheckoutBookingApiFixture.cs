using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace Jurassic.movieserviceapi.Tests;

public sealed class CheckoutBookingApiFixture : IAsyncLifetime
{
    private sealed class AlwaysHealthyCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }

    public Mock<IMovieRepository> MovieRepositoryMock { get; } = new();
    public Mock<IBookingRepository> BookingRepositoryMock { get; } = new();

    public WebApplicationFactory<Program> Factory { get; }

    public CheckoutBookingApiFixture()
    {
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DatabaseBootstrap:Enabled", "false");
            builder.UseSetting("WebAuthSeed:Enabled", "false");
            builder.UseSetting("Auth:JwtSigningKey", "test-signing-key-must-be-at-least-32-chars!");
            builder.UseSetting(
                "ConnectionStrings:WebConnection",
                "Host=127.0.0.1;Database=jurassic_web;Username=jurassic;Password=jurassic_dev");

            builder.ConfigureServices(services =>
            {
                var movieDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IMovieRepository));
                if (movieDesc is not null)
                {
                    services.Remove(movieDesc);
                }

                services.AddScoped(_ => MovieRepositoryMock.Object);

                var bookingDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IBookingRepository));
                if (bookingDesc is not null)
                {
                    services.Remove(bookingDesc);
                }

                services.AddScoped(_ => BookingRepositoryMock.Object);

                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    options.Registrations.Clear();
                    options.Registrations.Add(new HealthCheckRegistration(
                        "postgres-primary",
                        _ => new AlwaysHealthyCheck(),
                        failureStatus: null,
                        tags: null));
                });
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await Factory.DisposeAsync();
}
