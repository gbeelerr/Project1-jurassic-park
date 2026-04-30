using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Jurassic.movieserviceapi.Models;
using Jurassic.movieserviceapi.Repositories;
using Moq;
using Npgsql;

namespace Jurassic.movieserviceapi.Tests;

/// <summary>
/// Acceptance: POST /auth/register creates a jurassic_web user with hashed password, 409 when email exists
/// (including Postgres unique violation from races), and 400 for weak/missing credentials.
/// </summary>
[Collection("AuthLoginSequential")]
public sealed class AuthRegisterApiTests : IClassFixture<AuthLoginApiFixture>, IAsyncLifetime
{
    private readonly AuthLoginApiFixture _fixture;

    public AuthRegisterApiTests(AuthLoginApiFixture fixture)
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
    public async Task PostRegister_NewUser_Returns201_AndPassesHashedPasswordToRepository()
    {
        string? capturedHash = null;
        Guid capturedId = default;
        string? capturedEmail = null;

        _fixture.AuthRepositoryMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _fixture.AuthRepositoryMock
            .Setup(r => r.InsertUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string?, string, CancellationToken>(
                (id, email, _, hash, _) =>
                {
                    capturedId = id;
                    capturedEmail = email;
                    capturedHash = hash;
                })
            .Returns(Task.CompletedTask);

        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "  New.User@Example.COM  ",
            Password = "ValidPass-99!",
            DisplayName = " Pat "
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be("new.user@example.com");
        body.UserId.Should().Be(capturedId);

        capturedEmail.Should().Be("new.user@example.com");
        capturedHash.Should().NotBeNullOrWhiteSpace();
        capturedHash.Should().NotBe("ValidPass-99!");
        capturedHash!.Length.Should().BeGreaterThan(40);
    }

    [Fact]
    public async Task PostRegister_DuplicateEmail_Returns409_AndDoesNotInsert()
    {
        _fixture.AuthRepositoryMock
            .Setup(r => r.EmailExistsAsync("taken@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "taken@example.com",
            Password = "ValidPass-99!",
            DisplayName = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _fixture.AuthRepositoryMock.Verify(
            r => r.InsertUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PostRegister_PostgresUniqueViolation_Returns409()
    {
        _fixture.AuthRepositoryMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var race = new PostgresException(
            "duplicate key value violates unique constraint \"users_email_unique\"",
            "ERROR",
            "ERROR",
            "23505");
        _fixture.AuthRepositoryMock
            .Setup(r => r.InsertUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(race);

        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "collision@example.com",
            Password = "ValidPass-99!",
            DisplayName = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostRegister_ShortPassword_Returns400_AndDoesNotInsert()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = "short@example.com",
            Password = "short",
            DisplayName = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _fixture.AuthRepositoryMock.Verify(
            r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _fixture.AuthRepositoryMock.Verify(
            r => r.InsertUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
