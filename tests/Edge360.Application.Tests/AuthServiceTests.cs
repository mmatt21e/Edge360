using Edge360.Application.Auth;
using Edge360.Application.Auth.Dtos;
using Edge360.Application.Common.Exceptions;
using Edge360.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edge360.Application.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    private AuthService CreateSut() =>
        new(_factory.Create(), new Pbkdf2PasswordHasher(), new FakeTokenService(), new NullAuditWriter());

    [Fact]
    public async Task Register_CreatesUser_AndReturnsTokens()
    {
        var sut = CreateSut();

        var response = await sut.RegisterAsync(new RegisterRequest("a@example.com", "password123", "Alice"));

        response.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.User.Email.Should().Be("a@example.com");

        await using var verify = _factory.Create();
        (await verify.Users.CountAsync()).Should().Be(1);
        (await verify.RefreshTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        await CreateSut().RegisterAsync(new RegisterRequest("dupe@example.com", "password123", "A"));

        var act = async () => await CreateSut().RegisterAsync(new RegisterRequest("DUPE@example.com", "password123", "B"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Login_WrongPassword_Throws()
    {
        await CreateSut().RegisterAsync(new RegisterRequest("b@example.com", "password123", "Bob"));

        var act = async () => await CreateSut().LoginAsync(new LoginRequest("b@example.com", "wrongpass"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_CorrectPassword_Succeeds()
    {
        await CreateSut().RegisterAsync(new RegisterRequest("c@example.com", "password123", "Carol"));

        var response = await CreateSut().LoginAsync(new LoginRequest("c@example.com", "password123"));

        response.User.DisplayName.Should().Be("Carol");
    }

    [Fact]
    public async Task Refresh_RotatesToken_OldTokenRevoked()
    {
        var registration = await CreateSut().RegisterAsync(new RegisterRequest("d@example.com", "password123", "Dan"));

        var refreshed = await CreateSut().RefreshAsync(new RefreshRequest(registration.RefreshToken));

        refreshed.RefreshToken.Should().NotBe(registration.RefreshToken);

        // Re-using the original (now-rotated) token must fail.
        var act = async () => await CreateSut().RefreshAsync(new RefreshRequest(registration.RefreshToken));
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    public void Dispose() => _factory.Dispose();
}
