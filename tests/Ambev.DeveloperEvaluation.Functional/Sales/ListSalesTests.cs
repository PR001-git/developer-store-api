using System.Net;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for listing sales with <c>GET /api/sales</c>: paging and ordering. Each test starts
/// from empty tables, logs in and creates three sales with one line of quantity 1 (no discount), in this order:
/// <c>middle</c> (2026-01-02, 20.00), <c>newest</c> (2026-01-03, 10.00, then cancelled), <c>oldest</c> (2026-01-01, 30.00).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ListSalesTests : IAsyncLifetime
{
    private readonly ApiFixture _api;
    private readonly HttpClient _client;
    private SaleResponseBody _middle = null!;
    private SaleResponseBody _newest = null!;
    private SaleResponseBody _oldest = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public ListSalesTests(ApiFixture api)
    {
        _api = api;
        _client = api.CreateClient();
    }

    /// <summary>
    /// Empties the tables, then logs in and creates the three sales, so the totals are exact.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _api.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await _client.LogInAsNewUserAsync();

        _middle = await CreateSaleAsync(day: 2, total: 20.00m);
        var newest = await CreateSaleAsync(day: 3, total: 10.00m);
        _newest = await _client.CancelSaleAsync(newest.Id);
        _oldest = await CreateSaleAsync(day: 1, total: 30.00m);
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests the defaults: page 1 of 10 ordered by <c>saleDate desc</c>, the paged envelope built once, and each sale with its items.
    /// </summary>
    [Fact(DisplayName = "Given three sales When listing without parameters Then returns 200 with page 1, newest first, each sale with its items")]
    public async Task Given_ThreeSales_When_ListingWithoutParameters_Then_ReturnsNewestFirstWithItems()
    {
        // When
        using var response = await _client.GetAsync("/api/sales");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await SaleJson.ShouldBeSalePageEnvelopeAsync(response, "Sales retrieved successfully");
        var page = await ReadPageAsync(response);
        new { page.CurrentPage, page.TotalPages, page.TotalItems }.Should().Be(new { CurrentPage = 1, TotalPages = 1, TotalItems = 3 });
        page.Data.Select(sale => sale.Id).Should().Equal(_newest.Id, _middle.Id, _oldest.Id);
        page.Data.Should().BeEquivalentTo(new[] { _newest, _middle, _oldest }, options => options
            .ComparingRecordsByMembers()
            .Excluding(sale => sale.CreatedAt)
            .Excluding(sale => sale.UpdatedAt));
    }

    /// <summary>
    /// Tests a quoted, multi-field order in mixed case over HTTP.
    /// </summary>
    [Fact(DisplayName = "Given open and cancelled sales When ordering by isCancelled asc then totalAmount DESC Then open sales come first, highest total first")]
    public async Task Given_OpenAndCancelledSales_When_OrderingByTwoFields_Then_OpenSalesFirstHighestTotalFirst()
    {
        // When
        using var response = await _client.GetAsync(
            $"/api/sales?_order={Uri.EscapeDataString("\"isCancelled asc, totalAmount DESC\"")}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPageAsync(response);
        page.Data.Select(sale => sale.Id).Should().Equal(_oldest.Id, _middle.Id, _newest.Id);
    }

    /// <summary>
    /// Tests that an order by a field that can't be sorted is a 400 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given an order by a field that can't be sorted When listing Then returns 400 ValidationError for _order")]
    public async Task Given_UnsortableField_When_Listing_Then_Returns400ValidationError()
    {
        // When
        using var response = await _client.GetAsync("/api/sales?_order=price%20desc");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"_order: 'price' is not a sortable field. Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled."}""");
    }

    /// <summary>
    /// Tests that a page size above 100 is a 400 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given _size=101 When listing Then returns 400 ValidationError for _size")]
    public async Task Given_SizeAbove100_When_Listing_Then_Returns400ValidationError()
    {
        // When
        using var response = await _client.GetAsync("/api/sales?_size=101");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"_size: '_size' must be between 1 and 100."}""");
    }

    /// <summary>
    /// Tests the paging metadata of a last, partial page.
    /// </summary>
    [Fact(DisplayName = "Given three sales When listing page 2 of size 2 Then returns the oldest sale with currentPage 2, totalPages 2 and totalItems 3")]
    public async Task Given_ThreeSales_When_ListingSecondPageOfTwo_Then_ReturnsOldestWithPagingMetadata()
    {
        // When
        using var response = await _client.GetAsync("/api/sales?_page=2&_size=2");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPageAsync(response);
        page.Data.Select(sale => sale.Id).Should().Equal(_oldest.Id);
        new { page.CurrentPage, page.TotalPages, page.TotalItems }.Should().Be(new { CurrentPage = 2, TotalPages = 2, TotalItems = 3 });
    }

    /// <summary>
    /// Tests that a page past the end is empty and still reports the totals.
    /// </summary>
    [Fact(DisplayName = "Given three sales When listing a page past the end Then returns 200 with no sales and the same totals")]
    public async Task Given_ThreeSales_When_ListingPagePastTheEnd_Then_ReturnsNoSalesWithTotals()
    {
        // When
        using var response = await _client.GetAsync("/api/sales?_page=3&_size=2");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPageAsync(response);
        page.Data.Should().BeEmpty();
        new { page.CurrentPage, page.TotalPages, page.TotalItems }.Should().Be(new { CurrentPage = 3, TotalPages = 2, TotalItems = 3 });
    }

    private static async Task<SalePageResponseBody> ReadPageAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<SalePageResponseBody>())!;

    private Task<SaleResponseBody> CreateSaleAsync(int day, decimal total) =>
        _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid(SaleRequestBodyTestData.GenerateItem(1, total)) with
        {
            SaleDate = new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc)
        });
}
