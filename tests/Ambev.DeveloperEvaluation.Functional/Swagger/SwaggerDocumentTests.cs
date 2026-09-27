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
    /// Tests that the document declares the JWT Bearer scheme and applies it to every operation,
    /// which is what makes Swagger UI's Authorize button send the token.
    /// </summary>
    [Fact(DisplayName = "Given the API in Development When reading the Swagger document Then it declares a JWT Bearer scheme that every operation uses")]
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
        JsonSerializer.Serialize(root.GetProperty("security")).Should().Be("""[{"Bearer":[]}]""");
    }
}
