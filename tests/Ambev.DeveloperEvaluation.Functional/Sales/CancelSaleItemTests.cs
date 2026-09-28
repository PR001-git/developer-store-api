using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for cancelling one line with <c>PATCH /api/sales/{id}/items/{itemId}/cancel</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CancelSaleItemTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleItemTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public CancelSaleItemTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests the response for one line of two: 200, the documented envelope, the line cancelled, the total over the
    /// other line, and the sale still open with <c>updatedAt</c> set.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active items When cancelling one Then returns 200 with that item cancelled, the total dropped and the sale open")]
    public async Task Given_SaleWithTwoActiveItems_When_CancellingOne_Then_Returns200WithTotalDroppedAndSaleOpen()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));
        var cancelledItem = created.Items[0];
        var activeItem = created.Items[1];

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{cancelledItem.Id}/cancel", null);

        // Then (16.20 leaves; 12 × 3.10 at 20% = 29.76 stays)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale item cancelled successfully");
        var sale = await response.ReadSaleAsync();
        sale.IsCancelled.Should().BeFalse();
        sale.TotalAmount.Should().Be(29.76m);
        sale.UpdatedAt.Should().BeAfter(created.CreatedAt);
        sale.Items.Should().BeEquivalentTo(new[] { cancelledItem with { IsCancelled = true }, activeItem });
    }

    /// <summary>
    /// Tests rule R6 over HTTP: cancelling the last active line cancels the sale with total 0, and a read shows the same.
    /// </summary>
    [Fact(DisplayName = "Given a sale with one active item left When cancelling it Then returns 200 with the sale cancelled and total 0, and a read shows the same")]
    public async Task Given_SaleWithOneActiveItemLeft_When_CancellingIt_Then_Returns200WithSaleCancelledAndZeroTotal()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));
        await client.CancelSaleItemAsync(created.Id, created.Items[0].Id);

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{created.Items[1].Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sale = await response.ReadSaleAsync();
        sale.IsCancelled.Should().BeTrue();
        sale.TotalAmount.Should().Be(0m);
        sale.Items.Should().OnlyContain(item => item.IsCancelled);

        using var read = await client.GetAsync($"/api/sales/{created.Id}");
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await read.ReadSaleAsync();
        saved.IsCancelled.Should().BeTrue();
        saved.TotalAmount.Should().Be(0m);
    }

    /// <summary>
    /// Tests rule R9 over HTTP: cancelling the same line again is a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled item When cancelling it again Then returns 409 BusinessRuleViolation")]
    public async Task Given_CancelledItem_When_CancellingAgain_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));
        var item = created.Items[0];
        await client.CancelSaleItemAsync(created.Id, item.Id);

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{item.Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Item {{item.Id}} of sale {{created.SaleNumber}} is already cancelled"}""");
    }

    /// <summary>
    /// Tests rule R9 over HTTP: an item that isn't in the sale is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an item id that isn't in the sale When cancelling it Then returns 404 ResourceNotFound")]
    public async Task Given_ItemIdNotInSale_When_CancellingIt_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        var itemId = Guid.NewGuid();

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{itemId}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The item with ID {{itemId}} does not exist in the sale with ID {{created.Id}}"}""");
    }

    /// <summary>
    /// Tests that an unknown sale is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an unknown sale id When cancelling an item Then returns 404 ResourceNotFound")]
    public async Task Given_UnknownSaleId_When_CancellingItem_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var saleId = Guid.NewGuid();

        // When
        using var response = await client.PatchAsync($"/api/sales/{saleId}/items/{Guid.NewGuid()}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{saleId}} does not exist"}""");
    }

    /// <summary>
    /// Tests rule R7 over HTTP: an item of a cancelled sale can't be cancelled, a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When cancelling one of its items Then returns 409 BusinessRuleViolation")]
    public async Task Given_CancelledSale_When_CancellingItem_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await client.CancelSaleAsync(created.Id);

        // When
        using var response = await client.PatchAsync($"/api/sales/{created.Id}/items/{created.Items[0].Id}/cancel", null);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale {{created.SaleNumber}} is cancelled and cannot be modified"}""");
    }
}
