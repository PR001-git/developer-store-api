using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Users;

/// <summary>
/// Contains functional tests for signing up with <c>POST /api/users</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SignUpTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignUpTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public SignUpTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that the API accepts the status and role written as strings, as the spec's JSON contract says.
    /// </summary>
    [Fact(DisplayName = "Given status Active and role Admin as strings When signing up Then returns 201 pointing at the new user")]
    public async Task Given_StatusAndRoleAsStrings_When_SigningUp_Then_Returns201AtNewUser()
    {
        // Given
        using var client = _api.CreateClient();
        var request = SignUpRequestTestData.GenerateValid(status: "Active", role: "Admin");

        // When
        using var response = await client.PostAsJsonAsync("/api/users", request);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.ReadDataAsync()).GetProperty("id").GetGuid();
        response.Headers.Location!.ToString().Should().EndWith($"/api/Users/{id}");
    }

    /// <summary>
    /// Tests that the sign-up response shows what was saved, not just the id.
    /// </summary>
    [Fact(DisplayName = "Given a valid sign-up request When signing up Then the response shows the saved user, with the username as name")]
    public async Task Given_ValidSignUpRequest_When_SigningUp_Then_ResponseShowsSavedUser()
    {
        // Given
        using var client = _api.CreateClient();
        var request = SignUpRequestTestData.GenerateValid();

        // When
        using var response = await client.PostAsJsonAsync("/api/users", request);

        // Then
        var id = (await response.ReadDataAsync()).GetProperty("id").GetGuid();
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$$"""{"success":true,"message":"User created successfully","data":{"id":"{{{id}}}","name":"{{{request.Username}}}","email":"{{{request.Email}}}","phone":"{{{request.Phone}}}","role":"Admin","status":"Active"}}""");
    }

    /// <summary>
    /// Tests that the numeric value behind an enum is rejected, closing the gap that let a client send <c>"role": 7</c>
    /// straight past the string-only contract the API documents.
    /// </summary>
    [Fact(DisplayName = "Given role sent as a raw JSON number When signing up Then returns 400 ValidationError")]
    public async Task Given_RoleSentAsRawNumber_When_SigningUp_Then_Returns400ValidationError()
    {
        // Given
        using var client = _api.CreateClient();
        var request = SignUpRequestTestData.GenerateValid();

        // When
        using var response = await client.PostAsJsonAsync("/api/users", new
        {
            request.Username,
            request.Password,
            request.Phone,
            request.Email,
            request.Status,
            Role = 7
        });

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("type").GetString().Should().Be("ValidationError");
    }

    /// <summary>
    /// Tests that an email can be signed up only once, and that the second attempt is a business rule violation, not a server error.
    /// </summary>
    [Fact(DisplayName = "Given an email that is already signed up When signing up again with it Then returns 409 BusinessRuleViolation")]
    public async Task Given_EmailAlreadySignedUp_When_SigningUpAgain_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        var first = SignUpRequestTestData.GenerateValid();
        await client.SignUpAsync(first);
        var second = SignUpRequestTestData.GenerateValid() with { Email = first.Email };

        // When
        using var response = await client.PostAsJsonAsync("/api/users", second);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"User with email {{first.Email}} already exists"}""");
    }
}
