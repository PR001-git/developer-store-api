using System.Net;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Swagger;

/// <summary>
/// Contains functional tests for the Swagger document that the API serves in Development.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SwaggerDocumentTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerDocumentTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public SwaggerDocumentTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that the document declares the JWT Bearer scheme, which is what makes Swagger UI's Authorize
    /// button send the token.
    /// </summary>
    [Fact(DisplayName = "Given the API in Development When reading the Swagger document Then it declares a JWT Bearer scheme")]
    public async Task Given_ApiInDevelopment_When_ReadingSwaggerDocument_Then_DeclaresJwtBearerScheme()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("components").TryGetProperty("securitySchemes", out var schemes)
            .Should().BeTrue("the document must declare the Bearer scheme");
        JsonSerializer.Serialize(schemes).Should().Be(
            """{"Bearer":{"type":"http","description":"Paste the token from POST /api/auth (data.token), without the Bearer prefix.","scheme":"bearer","bearerFormat":"JWT"}}""");
    }

    /// <summary>
    /// Tests that only the operations of a controller with [Authorize] are marked Bearer-protected. The scheme is
    /// declared so Swagger UI's Authorize button exists, but only a protected endpoint should show the lock icon.
    /// </summary>
    [Fact(DisplayName = "Given only Sales has [Authorize] When reading the Swagger document Then only its operations require the Bearer scheme")]
    public async Task Given_OnlySalesHasAuthorize_When_ReadingSwaggerDocument_Then_OnlyItsOperationsRequireBearerScheme()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Then
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.TryGetProperty("security", out _).Should().BeFalse("no global security requirement should be declared");

        foreach (var path in root.GetProperty("paths").EnumerateObject())
        {
            var requiresBearer = path.Name.StartsWith("/api/sales", StringComparison.Ordinal);
            foreach (var operation in path.Value.EnumerateObject())
                operation.Value.TryGetProperty("security", out _).Should().Be(requiresBearer,
                    $"{path.Name} {operation.Name} {(requiresBearer ? "has [Authorize]" : "has no [Authorize]")}");
        }
    }
}
