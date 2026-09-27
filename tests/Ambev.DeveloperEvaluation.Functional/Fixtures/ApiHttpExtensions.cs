using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Helpers for calling the API the way a client does.
/// </summary>
public static class ApiHttpExtensions
{
    /// <summary>
    /// Signs up a user with <c>POST /api/users</c>, for tests whose subject is a later step, and fails the test if that doesn't return 201.
    /// </summary>
    /// <param name="client">The client of the API under test.</param>
    /// <param name="request">The sign-up request.</param>
    /// <returns>The new user's id.</returns>
    public static async Task<Guid> SignUpAsync(this HttpClient client, SignUpRequest request)
    {
        using var response = await client.PostAsJsonAsync("/api/users", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.ReadDataAsync()).GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Reads the <c>data</c> property of a success envelope <c>{success, message, data}</c>.
    /// </summary>
    /// <param name="response">A success response of the API.</param>
    /// <returns>A copy of the <c>data</c> element that outlives the parsed document.</returns>
    public static async Task<JsonElement> ReadDataAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }
}
