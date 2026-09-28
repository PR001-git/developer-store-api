using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for cancelling a sale with <c>PATCH /api/sales/{id}/cancel</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CancelSaleTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public CancelSaleTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests the cancel response: 200, the envelope with the documented fields, and the created sale with
    /// <c>isCancelled</c> true and <c>updatedAt</c> set, its items and total unchanged (rule R8).
    /// </summary>
    [Fact(DisplayName = "Given an open sale When cancelling it Then returns 200 with the sale cancelled, updatedAt set, and its items and total unchanged")]
    public async Task Given_OpenSale_When_Cancelling_Then_Returns200WithSaleCancelled()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale cancelled successfully");
        var cancelled = await response.ReadSaleAsync();
        cancelled.UpdatedAt.Should().BeAfter(created.CreatedAt);
        cancelled.Should().BeEquivalentTo(created with { IsCancelled = true, UpdatedAt = cancelled.UpdatedAt }, options => options
            .ComparingRecordsByMembers()
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMicroseconds(1)))
            .WhenTypeIs<DateTime>());
    }

    /// <summary>
    /// Tests rule R7 over HTTP: a cancelled sale is read-only, so a second cancel is a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When cancelling it again Then returns 409 BusinessRuleViolation")]
    public async Task Given_CancelledSale_When_CancellingAgain_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await client.CancelSaleAsync(created.Id);

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale {{created.SaleNumber}} is cancelled and cannot be modified"}""");
    }

    /// <summary>
    /// Tests that an unknown id is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an unknown id When cancelling the sale Then returns 404 ResourceNotFound")]
    public async Task Given_UnknownId_When_CancellingSale_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var id = Guid.NewGuid();

        // When
        using var response = await client.PatchAsync($"/api/sales/{id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{id}} does not exist"}""");
    }

    /// <summary>
    /// Tests that the endpoint requires a JWT and answers without one with the documented 401 body.
    /// </summary>
    [Fact(DisplayName = "Given no token When cancelling a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_NoToken_When_CancellingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.PatchAsync($"/api/sales/{Guid.NewGuid()}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Be("Bearer");
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}""");
    }
}
