using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Repositories;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Jurassic.movieserviceapi.Tests;

/// <summary>
/// Acceptance: POST /auth/login validates against jurassic_web.users (via <see cref="IAuthRepository"/>),
/// returns JWT + refresh token, persists session to jurassic_web.sessions, and returns 401 on failure.
/// </summary>
[Collection("AuthLoginSequential")]
public sealed class AuthLoginApiTests : IClassFixture<AuthLoginApiFixture>, IAsyncLifetime
{
    private readonly AuthLoginApiFixture _fixture;

    public AuthLoginApiTests(AuthLoginApiFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _fixture.AuthRepositoryMock.Reset();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostLogin_UnknownUser_Returns401_AndDoesNotInsertSession()
    {
        _fixture.AuthRepositoryMock
            .Setup(r => r.GetUserByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebUser?)null);

        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "any-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _fixture.AuthRepositoryMock.Verify(
            r => r.InsertSessionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PostLogin_InactiveUser_Returns401()
    {
        var user = new WebUser
        {
            Id = Guid.NewGuid(),
            Email = "inactive@example.com",
            PasswordHash = "unused",
            Role = "customer",
            IsActive = false
        };
        _fixture.AuthRepositoryMock
            .Setup(r => r.GetUserByEmailAsync("inactive@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _fixture.AuthRepositoryMock.Verify(
            r => r.InsertSessionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PostLogin_WrongPassword_Returns401_AndDoesNotInsertSession()
    {
        var hasher = new PasswordHasher<WebUser>();
        var userId = Guid.NewGuid();
        var stub = new WebUser
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = "",
            Role = "customer",
            IsActive = true
        };
        var hash = hasher.HashPassword(stub, "Correct-Horse-1!");
        var user = new WebUser
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = hash,
            Role = "customer",
            IsActive = true
        };
        _fixture.AuthRepositoryMock
            .Setup(r => r.GetUserByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _fixture.AuthRepositoryMock.Verify(
            r => r.InsertSessionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PostLogin_ValidCredentials_Returns200WithTokens_AndInsertsSession()
    {
        var hasher = new PasswordHasher<WebUser>();
        var userId = Guid.NewGuid();
        var stub = new WebUser
        {
            Id = userId,
            Email = "ok@example.com",
            PasswordHash = "",
            Role = "customer",
            IsActive = true
        };
        var hash = hasher.HashPassword(stub, "ValidPass-99!");
        var user = new WebUser
        {
            Id = userId,
            Email = "ok@example.com",
            PasswordHash = hash,
            Role = "customer",
            IsActive = true
        };
        _fixture.AuthRepositoryMock
            .Setup(r => r.GetUserByEmailAsync("ok@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = "ok@example.com",
            Password = "ValidPass-99!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("access_token").GetString().Should().NotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("refresh_token").GetString().Should().NotBeNullOrWhiteSpace();

        _fixture.AuthRepositoryMock.Verify(
            r => r.InsertSessionAsync(
                userId,
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostLogin_MissingCredentials_Returns401()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = " ",
            Password = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _fixture.AuthRepositoryMock.Verify(
            r => r.GetUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
