using System.Text.Json;
using FluentAssertions;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

/// <summary>
/// The JSON shape of a sale in the Sales API's success responses (spec §7.2).
/// </summary>
internal static class SaleJson
{
    private static readonly string[] SaleFields =
    [
        "id", "saleNumber", "saleDate", "customerId", "customerName", "branchId", "branchName",
        "totalAmount", "isCancelled", "createdAt", "updatedAt", "items"
    ];

    private static readonly string[] ItemFields =
    [
        "id", "productId", "productName", "quantity", "unitPrice", "discountPercentage", "discountAmount",
        "totalAmount", "isCancelled"
    ];

    /// <summary>
    /// Checks that the body is exactly <c>{success, message, data}</c>, built once, and that the sale in <c>data</c>
    /// and each of its lines have exactly the documented fields, in order: no <c>isDeleted</c>, no <c>deletedAt</c>.
    /// </summary>
    /// <param name="response">A success response of the Sales API.</param>
    /// <param name="message">The expected success message.</param>
    /// <returns>A task that completes when the body has been checked.</returns>
    public static async Task ShouldBeSaleEnvelopeAsync(HttpResponseMessage response, string message)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.EnumerateObject().Select(property => property.Name).Should().Equal("success", "message", "data");
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be(message);

        ShouldBeSale(root.GetProperty("data"));
    }

    /// <summary>
    /// Checks that the body is exactly <c>{success, message, data, currentPage, totalPages, totalItems}</c>, built once,
    /// and that every sale in <c>data</c> (at least one) and each of its lines have exactly the documented fields.
    /// </summary>
    /// <param name="response">A paged success response of the Sales API.</param>
    /// <param name="message">The expected success message.</param>
    /// <returns>A task that completes when the body has been checked.</returns>
    public static async Task ShouldBeSalePageEnvelopeAsync(HttpResponseMessage response, string message)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.EnumerateObject().Select(property => property.Name).Should()
            .Equal("success", "message", "data", "currentPage", "totalPages", "totalItems");
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be(message);
        root.GetProperty("data").EnumerateArray().Should().NotBeEmpty().And.AllSatisfy(ShouldBeSale);
    }

    private static void ShouldBeSale(JsonElement sale)
    {
        sale.EnumerateObject().Select(property => property.Name).Should().Equal(SaleFields);
        sale.GetProperty("items").EnumerateArray().Should().NotBeEmpty()
            .And.AllSatisfy(item => item.EnumerateObject().Select(property => property.Name).Should().Equal(ItemFields));
    }
}
