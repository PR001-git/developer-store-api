using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.Functional.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// Contains functional tests for the sale number of <c>POST /api/sales</c> (rule R12): generated when omitted,
/// kept trimmed when sent, and unique.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SaleNumberTests
{
    /// <summary>
    /// A generated number: <c>S-</c> and at least 6 digits (spec §8.3). Tests match the shape, not the value,
    /// because the order of tests in a class isn't fixed.
    /// </summary>
    private const string GeneratedNumberPattern = @"^S-\d{6,}$";

    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleNumberTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public SaleNumberTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that a body without <c>saleNumber</c> gets a generated number.
    /// </summary>
    [Fact(DisplayName = "Given a body without saleNumber When posting the sale Then returns 201 with a generated S- number")]
    public async Task Given_BodyWithoutSaleNumber_When_PostingSale_Then_Returns201WithGeneratedNumber()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var body = JsonSerializer.SerializeToNode(
            SaleRequestBodyTestData.GenerateValid(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        body.Remove("saleNumber");

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", body);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadSaleAsync()).SaleNumber.Should().MatchRegex(GeneratedNumberPattern);
    }

    /// <summary>
    /// Tests that <c>"saleNumber": null</c> counts as omitted.
    /// </summary>
    [Fact(DisplayName = "Given a body with a null saleNumber When posting the sale Then returns 201 with a generated S- number")]
    public async Task Given_BodyWithNullSaleNumber_When_PostingSale_Then_Returns201WithGeneratedNumber()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = null };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadSaleAsync()).SaleNumber.Should().MatchRegex(GeneratedNumberPattern);
    }

    /// <summary>
    /// Tests that a new number is kept, without its surrounding spaces.
    /// </summary>
    [Fact(DisplayName = "Given a new sale number with surrounding spaces When posting the sale Then returns 201 with the number trimmed")]
    public async Task Given_NewSaleNumberWithSpaces_When_PostingSale_Then_Returns201WithTrimmedNumber()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var number = $"S-{Guid.NewGuid():N}";
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = $"  {number}  " };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadSaleAsync()).SaleNumber.Should().Be(number);
    }

    /// <summary>
    /// Tests that a number another sale already has is a 409 with the documented body.
    /// </summary>
    [Fact(DisplayName = "Given a sale number another sale already has When posting the sale Then returns 409 BusinessRuleViolation")]
    public async Task Given_TakenSaleNumber_When_PostingSale_Then_Returns409BusinessRuleViolation()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var existing = await client.CreateSaleAsync(SaleRequestBodyTestData.GenerateValid());
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = existing.SaleNumber };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            $$"""{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"Sale number {{existing.SaleNumber}} already exists"}""");
    }

    /// <summary>
    /// Tests that an empty number is invalid input, not a request to generate one.
    /// </summary>
    [Fact(DisplayName = "Given an empty saleNumber When posting the sale Then returns 400 ValidationError")]
    public async Task Given_EmptySaleNumber_When_PostingSale_Then_Returns400ValidationError()
    {
        // Given
        using var client = _api.CreateClient();
        await client.LogInAsNewUserAsync();
        var sale = SaleRequestBodyTestData.GenerateValid() with { SaleNumber = string.Empty };

        // When
        using var response = await client.PostAsJsonAsync("/api/sales", sale);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"SaleNumber: 'Sale Number' must not be empty."}""");
    }
}
