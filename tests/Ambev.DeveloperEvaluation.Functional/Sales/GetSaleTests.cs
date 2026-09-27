using System.Net;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for reading a sale with <c>GET /api/sales/{id}</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class GetSaleTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public GetSaleTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that a created sale reads back from the database as the create response showed it.
    /// The create response comes from memory, so its <c>createdAt</c> has 100 ns ticks where PostgreSQL keeps microseconds.
    /// </summary>
    [Fact(DisplayName = "Given a created sale When getting it by id Then returns 200 with the same sale as the create response")]
    public async Task Given_CreatedSale_When_GettingById_Then_Returns200WithSameSale()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var created = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(
            SaleRequestBodyTestData.GenerateItem(4, 4.50m),
            SaleRequestBodyTestData.GenerateItem(12, 3.10m)));

        // When
        using var response = await client.GetAsync($"/api/sales/{created.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSaleEnvelopeAsync(response, "Sale retrieved successfully");
        (await response.ReadSaleAsync()).Should().BeEquivalentTo(created, options => options
            .ComparingRecordsByMembers()
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMicroseconds(1)))
            .WhenTypeIs<DateTime>());
    }

    /// <summary>
    /// Tests that an unknown id is a 404 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an unknown id When getting the sale Then returns 404 ResourceNotFound")]
    public async Task Given_UnknownId_When_GettingSale_Then_Returns404ResourceNotFound()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var id = Guid.NewGuid();

        // When
        using var response = await client.GetAsync($"/api/sales/{id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{id}} does not exist"}""");
    }

    /// <summary>
    /// Tests that the query validator runs through the MediatR pipeline: the empty id is a validation error.
    /// </summary>
    [Fact(DisplayName = "Given the empty id When getting the sale Then returns 400 ValidationError")]
    public async Task Given_EmptyId_When_GettingSale_Then_Returns400ValidationError()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();

        // When
        using var response = await client.GetAsync($"/api/sales/{Guid.Empty}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Id: 'Id' must not be empty."}""");
    }
}
