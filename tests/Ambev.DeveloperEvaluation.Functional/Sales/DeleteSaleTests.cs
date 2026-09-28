using System.Net;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for soft-deleting a sale with <c>DELETE /api/sales/{id}</c>. Each test starts from
/// empty tables and logs in, so the list totals are exact.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DeleteSaleTests : IAsyncLifetime
{
    private const string DeletedBody = """{"success":true,"message":"Sale deleted successfully"}""";

    private readonly ApiFixture _api;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSaleTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public DeleteSaleTests(ApiFixture api)
    {
        _api = api;
        _client = api.CreateClient();
    }

    /// <summary>
    /// Empties the tables, then logs in.
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
    /// Tests the delete response: 200 with exactly <c>{success, message}</c>, built once.
    /// </summary>
    [Fact(DisplayName = "Given a sale When deleting it Then returns 200 with exactly success and message")]
    public async Task Given_Sale_When_Deleting_Then_Returns200WithSuccessAndMessageOnly()
    {
        // Given
        var sale = await _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());

        // When
        using var response = await _client.DeleteAsync($"/api/sales/{sale.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be(DeletedBody);
    }

    /// <summary>
    /// Tests rule R11: a deleted sale is not found.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When getting it by id Then returns 404 ResourceNotFound")]
    public async Task Given_DeletedSale_When_GettingIt_Then_Returns404ResourceNotFound()
    {
        // Given
        var sale = await CreateDeletedSaleAsync();

        // When
        using var response = await _client.GetAsync($"/api/sales/{sale.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{sale.Id}} does not exist"}""");
    }

    /// <summary>
    /// Tests rule R11: the list omits a deleted sale, and <c>totalItems</c> doesn't count it.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale and a kept one When listing sales Then only the kept sale is listed and counted")]
    public async Task Given_DeletedAndKeptSales_When_Listing_Then_OnlyKeptSaleIsListedAndCounted()
    {
        // Given
        var kept = await _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await CreateDeletedSaleAsync();

        // When
        using var response = await _client.GetAsync("/api/sales");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<SalePageResponseBody>())!;
        page.Data.Select(sale => sale.Id).Should().Equal(kept.Id);
        page.TotalItems.Should().Be(1);
    }

    /// <summary>
    /// Tests rule R7: a cancelled sale is read-only, but it can still be deleted.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When deleting it Then returns 200")]
    public async Task Given_CancelledSale_When_Deleting_Then_Returns200()
    {
        // Given
        var sale = await _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        await _client.CancelSaleAsync(sale.Id);

        // When
        using var response = await _client.DeleteAsync($"/api/sales/{sale.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be(DeletedBody);
    }

    /// <summary>
    /// Tests that a second delete finds nothing to delete.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When deleting it again Then returns 404 ResourceNotFound")]
    public async Task Given_DeletedSale_When_DeletingAgain_Then_Returns404ResourceNotFound()
    {
        // Given
        var sale = await CreateDeletedSaleAsync();

        // When
        using var response = await _client.DeleteAsync($"/api/sales/{sale.Id}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID {{sale.Id}} does not exist"}""");
    }

    /// <summary>
    /// Tests rule R12: a deleted sale's number stays taken.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When creating a sale with its number Then returns 409 BusinessRuleViolation")]
    public async Task Given_DeletedSale_When_CreatingSaleWithItsNumber_Then_Returns409BusinessRuleViolation()
    {
        // Given
        var deleted = await CreateDeletedSaleAsync();
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = deleted.SaleNumber };

        // When
        using var response = await _client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale number {{deleted.SaleNumber}} already exists"}""");
    }

    /// <summary>
    /// Tests that the validator runs through the MediatR pipeline (Decision 5): the empty id is a 400, not a 404.
    /// </summary>
    [Fact(DisplayName = "Given the empty id When deleting the sale Then returns 400 ValidationError")]
    public async Task Given_EmptyId_When_DeletingSale_Then_Returns400ValidationError()
    {
        // When
        using var response = await _client.DeleteAsync($"/api/sales/{Guid.Empty}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"Id: 'Id' must not be empty."}""");
    }

    /// <summary>
    /// Tests that the endpoint requires a JWT and answers without one with the documented 401 body.
    /// </summary>
    [Fact(DisplayName = "Given no token When deleting a sale Then returns 401 AuthenticationError with the invalid-token body")]
    public async Task Given_NoToken_When_DeletingSale_Then_Returns401WithInvalidTokenBody()
    {
        // Given
        using var client = _api.CreateClient();

        // When
        using var response = await client.DeleteAsync($"/api/sales/{Guid.NewGuid()}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Be("Bearer");
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}""");
    }

    /// <summary>
    /// Creates a sale and deletes it, for tests whose subject is a later step, and fails the test if the delete
    /// doesn't return 200.
    /// </summary>
    /// <returns>The sale as the create response returned it.</returns>
    private async Task<SaleResponseBody> CreateDeletedSaleAsync()
    {
        var sale = await _client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        using var response = await _client.DeleteAsync($"/api/sales/{sale.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return sale;
    }
}
