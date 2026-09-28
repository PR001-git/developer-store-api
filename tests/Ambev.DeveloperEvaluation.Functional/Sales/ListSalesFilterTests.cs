using System.Net;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for the filters of <c>GET /api/sales</c>. Each test starts from empty tables with a
/// logged-in client.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ListSalesFilterTests : IAsyncLifetime
{
    private readonly ApiFixture _api;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesFilterTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public ListSalesFilterTests(ApiFixture api)
    {
        _api = api;
        _client = api.CreateClient();
    }

    /// <summary>
    /// Empties the tables, then logs in, so the counts are exact.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _api.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await _client.LogInAsNewUserAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests a representative combination. The request keeps customer names ending in "maria", open sales, sale dates
    /// from 2026-01-10T12:00Z (sent as 09:00 at -03:00) through the whole of 2026-01-31, and totals of 15.00 or more:
    /// <code>
    /// customerName  saleDate (UTC)       total  cancelled  kept
    /// Ana Maria     2026-01-10 11:59:59  30.00  no         no: before the min date
    /// Joana Maria   2026-01-10 12:00:00  20.00  no         yes: at the min date
    /// Bia Maria     2026-01-31 23:00:00  40.00  no         yes: the max date covers its whole day
    /// Duda Maria    2026-01-20 12:00:00  10.00  no         no: below the min total
    /// Maria Silva   2026-01-20 12:00:00  60.00  no         no: the name doesn't end in maria
    /// Clara Maria   2026-01-20 12:00:00  50.00  yes        no: cancelled
    /// </code>
    /// By highest total, one per page: page 2 holds Joana Maria, and the totals count the two matches.
    /// </summary>
    [Fact(DisplayName = "Given six sales When combining text, status, date and amount filters with paging and ordering Then page 2 holds the second match and totalItems is 2")]
    public async Task Given_SixSales_When_CombiningFiltersWithPagingAndOrdering_Then_ReturnsSecondMatchWithFilteredTotals()
    {
        // Given
        await CreateSaleAsync("Ana Maria", new DateTime(2026, 1, 10, 11, 59, 59, DateTimeKind.Utc), 30.00m);
        var atMinDate = await CreateSaleAsync("Joana Maria", new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 20.00m);
        await CreateSaleAsync("Bia Maria", new DateTime(2026, 1, 31, 23, 0, 0, DateTimeKind.Utc), 40.00m);
        await CreateSaleAsync("Duda Maria", new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 10.00m);
        await CreateSaleAsync("Maria Silva", new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 60.00m);
        var toCancel = await CreateSaleAsync("Clara Maria", new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 50.00m);
        await _client.CancelSaleAsync(toCancel.Id);

        // When
        using var response = await _client.GetAsync(
            "/api/sales?customerName=*MARIA&isCancelled=false&_minSaleDate=2026-01-10T09:00:00-03:00&_maxSaleDate=2026-01-31"
            + "&_minTotalAmount=15&_order=totalAmount%20desc&_page=2&_size=1");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<SalePageResponseBody>())!;
        page.Data.Select(sale => sale.Id).Should().Equal(atMinDate.Id);
        new { page.CurrentPage, page.TotalPages, page.TotalItems }.Should().Be(new { CurrentPage = 2, TotalPages = 2, TotalItems = 2 });
    }

    /// <summary>
    /// Tests that a min above its max is a 400 with the documented body.
    /// </summary>
    [Theory(DisplayName = "Given a min above its max When listing Then returns 400 ValidationError for the min")]
    [InlineData("_minTotalAmount=50&_maxTotalAmount=10", "_minTotalAmount: '_minTotalAmount' must not be above '_maxTotalAmount'.")]
    [InlineData("_minSaleDate=2026-02-01&_maxSaleDate=2026-01-31", "_minSaleDate: '_minSaleDate' must not be above '_maxSaleDate'.")]
    public async Task Given_MinAboveMax_When_Listing_Then_Returns400ValidationError(string query, string detail)
    {
        // When
        using var response = await _client.GetAsync($"/api/sales?{query}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ValidationError","error":"Invalid input data","detail":"{{detail}}"}""");
    }

    /// <summary>
    /// Tests that a malformed Guid, date, number or boolean fails model binding with the documented 400 body.
    /// </summary>
    [Theory(DisplayName = "Given a malformed filter value When listing Then returns 400 ValidationError naming the parameter")]
    [InlineData("customerId=not-a-guid", "customerId: The value 'not-a-guid' is not valid for CustomerId.")]
    [InlineData("_minSaleDate=yesterday", "_minSaleDate: The value 'yesterday' is not valid for MinSaleDate.")]
    [InlineData("_maxTotalAmount=ten", "_maxTotalAmount: The value 'ten' is not valid for MaxTotalAmount.")]
    [InlineData("isCancelled=maybe", "isCancelled: The value 'maybe' is not valid for IsCancelled.")]
    public async Task Given_MalformedFilterValue_When_Listing_Then_Returns400ValidationError(string query, string detail)
    {
        // When
        using var response = await _client.GetAsync($"/api/sales?{query}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ValidationError","error":"Invalid input data","detail":"{{detail}}"}""");
    }

    private Task<SaleResponseBody> CreateSaleAsync(string customerName, DateTime saleDate, decimal total) =>
        _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(1, total)) with
        {
            CustomerName = customerName,
            SaleDate = saleDate
        });
}
