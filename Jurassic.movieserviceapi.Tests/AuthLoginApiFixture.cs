using Jurassic.movieserviceapi.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace Jurassic.movieserviceapi.Tests;

public sealed class AuthLoginApiFixture : IAsyncLifetime
{
    private sealed class AlwaysHealthyCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }

    public Mock<IAuthRepository> AuthRepositoryMock { get; } = new();

    public WebApplicationFactory<Program> Factory { get; }

    public AuthLoginApiFixture()
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
                var authDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthRepository));
                if (authDesc != null)
                {
                    services.Remove(authDesc);
                }

                services.AddScoped(_ => AuthRepositoryMock.Object);

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
