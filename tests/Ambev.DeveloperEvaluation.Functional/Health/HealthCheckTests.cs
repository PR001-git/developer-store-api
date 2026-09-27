using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Health;

/// <summary>
/// Contains functional tests for the health check endpoint.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class HealthCheckTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public HealthCheckTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that the running API reports itself healthy.
    /// </summary>
    [Fact(DisplayName = "Given the API is running When GET /health Then returns 200 OK")]
    public async Task Given_RunningApi_When_GetHealth_Then_ReturnsOk()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.GetAsync("/health");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
