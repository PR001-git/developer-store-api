using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Auth;

/// <summary>
/// Contains functional tests for logging in with <c>POST /api/auth</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class LogInTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogInTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public LogInTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that an active user gets a JWT for themselves at <c>data.token</c>, in an envelope built once.
    /// </summary>
    [Fact(DisplayName = "Given an active user When logging in with the right password Then returns 200 with the user's token at data.token")]
    public async Task Given_ActiveUser_When_LoggingInWithRightPassword_Then_Returns200WithTokenAtDataToken()
    {
        // Given
        using var client = _api.CreateClient();
        var user = SignUpRequestTestData.GenerateValid();
        var id = await client.SignUpAsync(user);

        // When
        using var response = await client.PostAsJsonAsync("/api/auth", new { user.Email, user.Password });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await response.ReadDataAsync()).GetProperty("token").GetString();
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$$"""{"success":true,"message":"User authenticated successfully","data":{"token":"{{{token}}}","email":"{{{user.Email}}}","name":"{{{user.Username}}}","role":"Admin"}}""");
        new JwtSecurityTokenHandler().ReadJwtToken(token!).Claims
            .Should().Contain(claim => claim.Type == "nameid" && claim.Value == id.ToString());
    }

    /// <summary>
    /// Tests that a wrong password is an authentication failure with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an active user When logging in with a wrong password Then returns 401 AuthenticationError with Invalid credentials")]
    public async Task Given_ActiveUser_When_LoggingInWithWrongPassword_Then_Returns401InvalidCredentials()
    {
        // Given
        using var client = _api.CreateClient();
        var user = SignUpRequestTestData.GenerateValid();
        await client.SignUpAsync(user);

        // When
        using var response = await client.PostAsJsonAsync("/api/auth", new { user.Email, Password = "Wrong@123" });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Authentication failed","detail":"Invalid credentials"}""");
    }

    /// <summary>
    /// Tests that an inactive user can't log in, even with the right password.
    /// </summary>
    [Fact(DisplayName = "Given an inactive user When logging in with the right password Then returns 401 AuthenticationError with User is not active")]
    public async Task Given_InactiveUser_When_LoggingInWithRightPassword_Then_Returns401UserIsNotActive()
    {
        // Given
        using var client = _api.CreateClient();
        var user = SignUpRequestTestData.GenerateValid(status: "Inactive");
        await client.SignUpAsync(user);

        // When
        using var response = await client.PostAsJsonAsync("/api/auth", new { user.Email, user.Password });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Authentication failed","detail":"User is not active"}""");
    }
}
