using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for creating a sale with <c>POST /api/sales</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CreateSaleTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public CreateSaleTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// One line at 4.50 per row: quantity, then the expected discount percentage, discount amount and line total.
    /// </summary>
    public static TheoryData<int, decimal, decimal, decimal> DiscountTiers => new()
    {
        { 3, 0m, 0.00m, 13.50m },
        { 4, 10m, 1.80m, 16.20m },
        { 10, 20m, 9.00m, 36.00m }
    };

    /// <summary>
    /// Tests the whole create response: 201, the <c>Location</c> of the new sale, the envelope built once with the
    /// documented fields, and the sale as sent plus what the domain computed (5 × 4.50 gets 10%).
    /// </summary>
    [Fact(DisplayName = "Given a logged-in client When posting a valid sale Then returns 201 at /api/sales/{id} with the sale and its discount")]
    public async Task Given_LoggedInClient_When_PostingValidSale_Then_Returns201WithSaleAndDiscount()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(5, 4.50m));

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale created successfully");
        var created = await response.ReadSaleAsync();
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/sales/{created.Id}");
        created.Should().BeEquivalentTo(new
        {
            sale.SaleNumber,
            sale.SaleDate,
            sale.CustomerId,
            sale.CustomerName,
            sale.BranchId,
            sale.BranchName,
            TotalAmount = 20.25m,
            IsCancelled = false,
            UpdatedAt = (DateTime?)null,
            Items = new[]
            {
                new
                {
                    sale.Items[0].ProductId,
                    sale.Items[0].ProductName,
                    Quantity = 5,
                    UnitPrice = 4.50m,
                    DiscountPercentage = 10m,
                    DiscountAmount = 2.25m,
                    TotalAmount = 20.25m,
                    IsCancelled = false
                }
            }
        });
    }

    /// <summary>
    /// Tests rule R1 over HTTP at the three tiers: 3 gets nothing, 4 gets 10% (spec decision D3), 10 gets 20%.
    /// </summary>
    [Theory(DisplayName = "Given a line at a discount tier When posting the sale Then the response has the tier's discount and totals")]
    [MemberData(nameof(DiscountTiers))]
    public async Task Given_LineAtDiscountTier_When_PostingSale_Then_ResponseHasTierDiscount(
        int quantity, decimal discountPercentage, decimal discountAmount, decimal totalAmount)
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(quantity, 4.50m));

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.ReadSaleAsync();
        created.TotalAmount.Should().Be(totalAmount);
        created.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            DiscountPercentage = discountPercentage,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount
        });
    }

    /// <summary>
    /// Tests rule R2: discount fields that a client sends are ignored, and the domain computes its own.
    /// </summary>
    [Fact(DisplayName = "Given a body with discount fields When posting the sale Then they are ignored and the domain's discount applies")]
    public async Task Given_BodyWithDiscountFields_When_PostingSale_Then_DomainDiscountApplies()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var body = JsonSerializer.SerializeToNode(
            SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(3, 10.00m)),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        body["totalAmount"] = 15.00m;
        var line = body["items"]![0]!.AsObject();
        line["discountPercentage"] = 50m;
        line["discountAmount"] = 15.00m;
        line["totalAmount"] = 15.00m;

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", body);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.ReadSaleAsync();
        created.TotalAmount.Should().Be(30.00m);
        created.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            DiscountPercentage = 0m,
            DiscountAmount = 0.00m,
            TotalAmount = 30.00m
        });
    }

    /// <summary>
    /// Tests rule R1's limit: 21 identical items are a validation error with R1's message.
    /// </summary>
    [Fact(DisplayName = "Given a line with 21 identical items When posting the sale Then returns 400 ValidationError with R1's message")]
    public async Task Given_LineWith21Items_When_PostingSale_Then_Returns400WithR1Message()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(21, 4.50m));

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Items[0].Quantity: It's not possible to sell above 20 identical items"}""");
    }

    /// <summary>
    /// Tests rule R4: a product that appears in two lines is a validation error.
    /// </summary>
    [Fact(DisplayName = "Given two lines for the same product When posting the sale Then returns 400 ValidationError")]
    public async Task Given_TwoLinesForSameProduct_When_PostingSale_Then_Returns400()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var line = SaleRequestBodyTestData.GenerateItem(2, 4.50m);
        var sale = SaleRequestBodyTestData.GenerateValid(line, line with { Quantity = 3 });

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Items: Each product can appear only once in a sale"}""");
    }

    /// <summary>
    /// Tests rule R5: a sale without lines is a validation error.
    /// </summary>
    [Fact(DisplayName = "Given an empty item list When posting the sale Then returns 400 ValidationError")]
    public async Task Given_EmptyItemList_When_PostingSale_Then_Returns400()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid() with { Items = [] };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Items: A sale must have at least one item"}""");
    }
}
