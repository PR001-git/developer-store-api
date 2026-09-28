using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.ListSales;

/// <summary>
/// Contains unit tests for <see cref="ListSalesQueryValidator"/>. Failures name the query parameters the client sent.
/// </summary>
public sealed class ListSalesQueryValidatorTests
{
    private readonly ListSalesQueryValidator _validator = new();

    /// <summary>
    /// Tests that omitted parameters and values inside the rules pass.
    /// </summary>
    [Theory(DisplayName = "Given paging and ordering within the rules When validating Then there are no failures")]
    [InlineData(null, null, null)]
    [InlineData(1, 1, "saleNumber")]
    [InlineData(1000, 100, "\"saleDate desc, totalAmount\"")]
    public void Given_ValidParameters_When_Validating_Then_NoFailures(int? page, int? size, string? order)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { Page = page, Size = size, Order = order });

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that the page must be at least 1.
    /// </summary>
    [Theory(DisplayName = "Given a page below 1 When validating Then _page fails")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Given_PageBelowOne_When_Validating_Then_PageFails(int page)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { Page = page });

        // Then
        result.ShouldHaveValidationErrorFor("_page").WithErrorMessage("'_page' must be at least 1.").Only();
    }

    /// <summary>
    /// Tests that the size must be from 1 to 100.
    /// </summary>
    [Theory(DisplayName = "Given a size outside 1 to 100 When validating Then _size fails")]
    [InlineData(0)]
    [InlineData(101)]
    public void Given_SizeOutOfRange_When_Validating_Then_SizeFails(int size)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { Size = size });

        // Then
        result.ShouldHaveValidationErrorFor("_size").WithErrorMessage("'_size' must be between 1 and 100.").Only();
    }

    /// <summary>
    /// Tests that an order the parser rejects fails with the parser's reason.
    /// </summary>
    [Fact(DisplayName = "Given an order the parser rejects When validating Then _order fails with the parser's reason")]
    public void Given_InvalidOrder_When_Validating_Then_OrderFailsWithParserReason()
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { Order = "price desc" });

        // Then
        result.ShouldHaveValidationErrorFor("_order")
            .WithErrorMessage("'price' is not a sortable field. Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled.")
            .Only();
    }
}
