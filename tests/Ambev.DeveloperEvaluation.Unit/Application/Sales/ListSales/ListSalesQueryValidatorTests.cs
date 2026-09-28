using System.Globalization;
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

    /// <summary>
    /// Tests sale-date ranges that pass: a min inside a whole-day max, equal bounds, and a single bound.
    /// </summary>
    [Theory(DisplayName = "Given a sale-date range whose min is not above its max When validating Then there are no failures")]
    [InlineData("2026-01-31T10:00:00", "2026-01-31")]
    [InlineData("2026-01-31", "2026-01-31")]
    [InlineData("2026-01-31T10:00:00Z", "2026-01-31T10:00:00Z")]
    [InlineData("2026-02-01", null)]
    [InlineData(null, "2026-01-31")]
    public void Given_SaleDateRangeInOrder_When_Validating_Then_NoFailures(string? min, string? max)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { MinSaleDate = QueryDate(min), MaxSaleDate = QueryDate(max) });

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that a min sale date above the max fails, comparing UTC bounds: the whole-day rule and offsets count.
    /// </summary>
    [Theory(DisplayName = "Given a min sale date above the max When validating Then _minSaleDate fails")]
    [InlineData("2026-02-01", "2026-01-31")]
    [InlineData("2026-01-31T10:00:01Z", "2026-01-31T10:00:00Z")]
    [InlineData("2026-01-31T10:00:00-03:00", "2026-01-31T12:00:00Z")]
    public void Given_MinSaleDateAboveMax_When_Validating_Then_MinSaleDateFails(string min, string max)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { MinSaleDate = QueryDate(min), MaxSaleDate = QueryDate(max) });

        // Then
        result.ShouldHaveValidationErrorFor("_minSaleDate")
            .WithErrorMessage("'_minSaleDate' must not be above '_maxSaleDate'.")
            .Only();
    }

    /// <summary>
    /// Tests total-amount ranges that pass: equal bounds, an ordered range, and a single bound.
    /// </summary>
    [Theory(DisplayName = "Given a total-amount range whose min is not above its max When validating Then there are no failures")]
    [InlineData("10", "10")]
    [InlineData("10", "20.50")]
    [InlineData(null, "5")]
    [InlineData("5", null)]
    public void Given_TotalAmountRangeInOrder_When_Validating_Then_NoFailures(string? min, string? max)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { MinTotalAmount = Amount(min), MaxTotalAmount = Amount(max) });

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that a min total amount above the max fails.
    /// </summary>
    [Theory(DisplayName = "Given a min total amount above the max When validating Then _minTotalAmount fails")]
    [InlineData("20.01", "20")]
    [InlineData("0", "-1")]
    public void Given_MinTotalAmountAboveMax_When_Validating_Then_MinTotalAmountFails(string min, string max)
    {
        // When
        var result = _validator.TestValidate(new ListSalesQuery { MinTotalAmount = Amount(min), MaxTotalAmount = Amount(max) });

        // Then
        result.ShouldHaveValidationErrorFor("_minTotalAmount")
            .WithErrorMessage("'_minTotalAmount' must not be above '_maxTotalAmount'.")
            .Only();
    }

    // ASP.NET Core's DateTimeModelBinder parses a query value this way: no offset gives Unspecified, an offset or Z gives UTC.
    private static DateTime? QueryDate(string? value) =>
        value is null
            ? null
            : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces);

    private static decimal? Amount(string? value) =>
        value is null ? null : decimal.Parse(value, CultureInfo.InvariantCulture);
}
