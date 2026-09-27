using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Users;

/// <summary>
/// Contains functional tests for reading a user with <c>GET /api/users/{id}</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class GetUserTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public GetUserTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that a signed-up user reads back with every field, and the username as the name.
    /// </summary>
    [Fact(DisplayName = "Given a signed-up user When getting it by id Then returns 200 with the user, with the username as name")]
    public async Task Given_SignedUpUser_When_GettingById_Then_Returns200WithUsernameAsName()
    {
        // Given
        using var client = _api.CreateClient();
        var request = SignUpRequestTestData.GenerateValid();
        var id = await client.SignUpAsync(request);

        // When
        using var response = await client.GetAsync($"/api/users/{id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$$"""{"success":true,"message":"User retrieved successfully","data":{"id":"{{{id}}}","name":"{{{request.Username}}}","email":"{{{request.Email}}}","phone":"{{{request.Phone}}}","role":"Customer","status":"Active"}}""");
    }
}
