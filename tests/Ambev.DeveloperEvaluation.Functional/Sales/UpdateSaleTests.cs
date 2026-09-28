using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for updating a sale with <c>PUT /api/sales/{id}</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class UpdateSaleTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public UpdateSaleTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests rule R10 over HTTP. One PUT moves a line across a discount tier (4 to 10 items, 10% to 20%), renames its
    /// product, adds a product and drops another. The response is 200 with the new header and the three lines,
    /// the dropped one cancelled, and a read shows the same sale.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two items When putting 10 of the first product, a new product and not the second Then returns 200 with the first at 20%, the new line, the second cancelled and total 52.00, and a read shows the same")]
    public async Task Given_SaleWithTwoItems_When_PuttingReconciledLines_Then_Returns200WithLinesReconciled()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));
        var changedItem = created.Items[0];
        var droppedItem = created.Items[1];
        var addedLine = SaleRequestBodyTestData.GenerateItem(2, 8.00m);
        var body = SaleRequestBodyTestData.GenerateValidUpdate(
            SaleRequestBodyTestData.GenerateItem(10, 4.50m) with { ProductId = changedItem.ProductId },
            addedLine);

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{created.Id}", body);

        // Then (10 × 4.50 at 20% = 36.00, plus 2 × 8.00 = 16.00; the dropped line keeps its 29.76 but doesn't count)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale updated successfully");
        var updated = await response.ReadSaleAsync();
        updated.Should().BeEquivalentTo(new
        {
            created.Id,
            created.SaleNumber,
            body.SaleDate,
            body.CustomerId,
            body.CustomerName,
            body.BranchId,
            body.BranchName,
            TotalAmount = 52.00m,
            IsCancelled = false
        });
        updated.UpdatedAt.Should().BeAfter(created.CreatedAt);
        var addedId = updated.Items.Should().ContainSingle(item => item.ProductId == addedLine.ProductId).Subject.Id;
        updated.Items.Should().BeEquivalentTo(new[]
        {
            changedItem with
            {
                ProductName = body.Items[0].ProductName, Quantity = 10,
                DiscountPercentage = 20m, DiscountAmount = 9.00m, TotalAmount = 36.00m
            },
            droppedItem with { IsCancelled = true },
            new SaleItemResponseBody(
                Id: addedId, ProductId: addedLine.ProductId, ProductName: addedLine.ProductName, Quantity: 2,
                UnitPrice: 8.00m, DiscountPercentage: 0m, DiscountAmount: 0.00m, TotalAmount: 16.00m, IsCancelled: false)
        });

        using var read = await client.GetAsync($"/api/sales/{created.Id}");
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await read.ReadSaleAsync();
        saved.Should().BeEquivalentTo(updated, options => options.ComparingRecordsByMembers().Excluding(sale => sale.UpdatedAt));
        saved.UpdatedAt.Should().BeCloseTo(updated.UpdatedAt!.Value, TimeSpan.FromMicroseconds(1));
    }

    /// <summary>
    /// Tests rule R7 over HTTP: a cancelled sale is read-only, so an update is a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When putting an update Then returns 409 BusinessRuleViolation")]
    public async Task Given_CancelledSale_When_PuttingUpdate_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await client.CancelSaleAsync(created.Id);

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{created.Id}", SaleRequestBodyTestData.GenerateValidUpdate());

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale {{created.SaleNumber}} is cancelled and cannot be modified"}""");
    }

    /// <summary>
    /// Tests rule R1's limit over HTTP: 21 identical items are a validation error with R1's message, as on create.
    /// </summary>
    [Fact(DisplayName = "Given a line with 21 identical items When putting the update Then returns 400 ValidationError with R1's message")]
    public async Task Given_LineWith21Items_When_PuttingUpdate_Then_Returns400WithR1Message()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        var body = SaleRequestBodyTestData.GenerateValidUpdate(SaleRequestBodyTestData.GenerateItem(21, 4.50m));

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{created.Id}", body);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Items[0].Quantity: It's not possible to sell above 20 identical items"}""");
    }

    /// <summary>
    /// Tests that an unknown id is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an unknown id When putting an update Then returns 404 ResourceNotFound")]
    public async Task Given_UnknownId_When_PuttingUpdate_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var id = Guid.NewGuid();

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{id}", SaleRequestBodyTestData.GenerateValidUpdate());

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{id}} does not exist"}""");
    }

    /// <summary>
    /// Tests rule R12 over HTTP: a <c>saleNumber</c> sent on PUT is ignored, and the sale keeps its number.
    /// </summary>
    [Fact(DisplayName = "Given a body with a saleNumber When putting the update Then returns 200 and the sale keeps its number, and a read shows the same")]
    public async Task Given_BodyWithSaleNumber_When_PuttingUpdate_Then_Returns200AndSaleKeepsItsNumber()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        var body = JsonSerializer.SerializeToNode(
            SaleRequestBodyTestData.GenerateValidUpdate(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        body["saleNumber"] = $"S-{Guid.NewGuid():N}";

        // When
        using var response = await client.PutAsJsonAsync($"/api/sales/{created.Id}", body);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadSaleAsync()).SaleNumber.Should().Be(created.SaleNumber);
        using var read = await client.GetAsync($"/api/sales/{created.Id}");
        (await read.ReadSaleAsync()).SaleNumber.Should().Be(created.SaleNumber);
    }
}
