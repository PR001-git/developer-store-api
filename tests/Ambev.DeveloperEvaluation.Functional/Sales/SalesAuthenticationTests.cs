using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for the JWT that every Sales endpoint requires (spec §7.5).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SalesAuthenticationTests
{
    private const string InvalidTokenBody =
        """{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}""";

    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesAuthenticationTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public SalesAuthenticationTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that a request without a token gets the documented 401 body, not an empty response.
    /// </summary>
    [Fact(DisplayName = "Given no token When posting a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_NoToken_When_PostingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", SaleRequestBodyTestData.GenerateValid());

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Be("Bearer");
        (await response.Content.ReadAsStringAsync()).Should().Be(InvalidTokenBody);
    }

    /// <summary>
    /// Tests that a well-formed token signed with another key is rejected with the documented 401 body.
    /// </summary>
    [Fact(DisplayName = "Given a token signed with another key When getting a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_TokenSignedWithAnotherKey_When_GettingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            CreateToken("another-signing-key-that-is-at-least-32-bytes-long", DateTime.UtcNow.AddHours(1)));

        // When
        using var response = await client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Be(InvalidTokenBody);
    }

    /// <summary>
    /// Tests that a token signed with the API's own key but already expired is rejected with the documented 401 body.
    /// </summary>
    [Fact(DisplayName = "Given an expired token When getting a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_ExpiredToken_When_GettingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();
        var apiKey = _api.Services.GetRequiredService<IConfiguration>()["Jwt:SecretKey"]!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            CreateToken(apiKey, DateTime.UtcNow.AddMinutes(-5)));

        // When
        using var response = await client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Be(InvalidTokenBody);
    }

    private static string CreateToken(string signingKey, DateTime expires) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            notBefore: expires.AddHours(-2),
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)));
}
