using System.Net;
using System.Net.Http.Headers;
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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
    /// Signs up a new active user and logs in, for tests whose subject is a later step. The client then sends
    /// the JWT with every request, as <c>Authorization: Bearer &lt;token&gt;</c>. Fails the test if either call fails.
    /// </summary>
    /// <param name="client">The client of the API under test.</param>
    /// <returns>A task that completes when the client holds the token.</returns>
    public static async Task LogInAsNewUserAsync(this HttpClient client)
    {
        var user = SignUpRequestTestData.GenerateValid();
        await client.SignUpAsync(user);

        using var response = await client.PostAsJsonAsync("/api/auth", new { user.Email, user.Password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await response.ReadDataAsync()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Creates a sale with <c>POST /api/sales</c>, for tests whose subject is a later step, and fails the test if that doesn't return 201.
    /// </summary>
    /// <param name="client">A client that holds a token.</param>
    /// <param name="sale">The sale to create.</param>
    /// <returns>The created sale, as the response returned it.</returns>
    public static async Task<SaleResponseBody> CreateSaleAsync(this HttpClient client, SaleRequestBody sale)
    {
        using var response = await client.PostAsJsonAsync("/api/sales", sale);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.ReadSaleAsync();
    }

    /// <summary>
    /// Cancels a sale with <c>PATCH /api/sales/{id}/cancel</c>, for tests whose subject is a later step, and fails the test if that doesn't return 200.
    /// </summary>
    /// <param name="client">A client that holds a token.</param>
    /// <param name="saleId">The id of the sale to cancel.</param>
    /// <returns>The cancelled sale, as the response returned it.</returns>
    public static async Task<SaleResponseBody> CancelSaleAsync(this HttpClient client, Guid saleId)
    {
        using var response = await client.PatchAsync($"/api/sales/{saleId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.ReadSaleAsync();
    }

    /// <summary>
    /// Cancels one line with <c>PATCH /api/sales/{id}/items/{itemId}/cancel</c>, for tests whose subject is a later step,
    /// and fails the test if that doesn't return 200.
    /// </summary>
    /// <param name="client">A client that holds a token.</param>
    /// <param name="saleId">The id of the sale.</param>
    /// <param name="itemId">The id of the line to cancel.</param>
    /// <returns>The sale after the change, as the response returned it.</returns>
    public static async Task<SaleResponseBody> CancelSaleItemAsync(this HttpClient client, Guid saleId, Guid itemId)
    {
        using var response = await client.PatchAsync($"/api/sales/{saleId}/items/{itemId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.ReadSaleAsync();
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

    /// <summary>
    /// Reads the sale in the <c>data</c> property of a Sales success envelope.
    /// </summary>
    /// <param name="response">A success response of the Sales API.</param>
    /// <returns>The sale.</returns>
    public static async Task<SaleResponseBody> ReadSaleAsync(this HttpResponseMessage response) =>
        (await response.ReadDataAsync()).Deserialize<SaleResponseBody>(JsonOptions)!;
}
