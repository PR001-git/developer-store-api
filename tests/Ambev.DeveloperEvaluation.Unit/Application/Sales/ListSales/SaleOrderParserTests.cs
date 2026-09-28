using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.ListSales;

/// <summary>
/// Contains unit tests for <see cref="SaleOrderParser"/>, the <c>_order</c> grammar of spec §7.3.
/// </summary>
public sealed class SaleOrderParserTests
{
    private const string UseSortableFields =
        "Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled.";

    /// <summary>
    /// Tests that an omitted, blank or empty quoted order is the default, <c>saleDate desc</c>.
    /// </summary>
    [Theory(DisplayName = "Given no order When parsing Then the order is saleDate desc")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("\"\"")]
    public void Given_NoOrder_When_Parsing_Then_OrderIsSaleDateDescending(string? order)
    {
        // When
        var parsed = SaleOrderParser.TryParse(order, out var sorts, out var error);

        // Then
        parsed.Should().BeTrue();
        error.Should().BeNull();
        sorts.Should().Equal(new SaleSort(SaleSortField.SaleDate, Descending: true));
    }

    /// <summary>
    /// Tests that each JSON field name maps to its field, and that the direction defaults to ascending.
    /// </summary>
    [Theory(DisplayName = "Given a field name alone When parsing Then it maps to its field, ascending")]
    [InlineData("saleNumber", SaleSortField.SaleNumber)]
    [InlineData("saleDate", SaleSortField.SaleDate)]
    [InlineData("customerName", SaleSortField.CustomerName)]
    [InlineData("branchName", SaleSortField.BranchName)]
    [InlineData("totalAmount", SaleSortField.TotalAmount)]
    [InlineData("isCancelled", SaleSortField.IsCancelled)]
    public void Given_FieldNameAlone_When_Parsing_Then_MapsToFieldAscending(string order, SaleSortField field)
    {
        // When
        var parsed = SaleOrderParser.TryParse(order, out var sorts, out _);

        // Then
        parsed.Should().BeTrue();
        sorts.Should().Equal(new SaleSort(field, Descending: false));
    }

    /// <summary>
    /// Tests the <c>general-api.md</c> form, with and without the surrounding quotes.
    /// </summary>
    [Theory(DisplayName = "Given two clauses with or without quotes When parsing Then they keep their order and directions")]
    [InlineData("\"saleDate desc, totalAmount\"")]
    [InlineData("saleDate desc, totalAmount")]
    public void Given_TwoClausesWithOrWithoutQuotes_When_Parsing_Then_KeepOrderAndDirections(string order)
    {
        // When
        var parsed = SaleOrderParser.TryParse(order, out var sorts, out _);

        // Then
        parsed.Should().BeTrue();
        sorts.Should().Equal(
            new SaleSort(SaleSortField.SaleDate, Descending: true),
            new SaleSort(SaleSortField.TotalAmount, Descending: false));
    }

    /// <summary>
    /// Tests that field names and directions ignore case.
    /// </summary>
    [Fact(DisplayName = "Given names and directions in mixed case When parsing Then case is ignored")]
    public void Given_MixedCase_When_Parsing_Then_CaseIsIgnored()
    {
        // When
        var parsed = SaleOrderParser.TryParse("SALEDATE Desc, customername ASC", out var sorts, out _);

        // Then
        parsed.Should().BeTrue();
        sorts.Should().Equal(
            new SaleSort(SaleSortField.SaleDate, Descending: true),
            new SaleSort(SaleSortField.CustomerName, Descending: false));
    }

    /// <summary>
    /// Tests that extra spaces and tabs around the quotes, clauses and tokens are ignored.
    /// </summary>
    [Fact(DisplayName = "Given extra spaces and tabs When parsing Then they are ignored")]
    public void Given_ExtraWhitespace_When_Parsing_Then_ItIsIgnored()
    {
        // When
        var parsed = SaleOrderParser.TryParse("  \" branchName \t desc ,  isCancelled \"  ", out var sorts, out _);

        // Then
        parsed.Should().BeTrue();
        sorts.Should().Equal(
            new SaleSort(SaleSortField.BranchName, Descending: true),
            new SaleSort(SaleSortField.IsCancelled, Descending: false));
    }

    /// <summary>
    /// Tests every rejection: empty clause, unknown field (including <c>id</c> and an unbalanced quote),
    /// unknown direction, too many tokens, repeated field.
    /// </summary>
    [Theory(DisplayName = "Given an invalid order When parsing Then it fails with the reason")]
    [InlineData("saleDate,,totalAmount", "Each comma-separated sort clause needs a field.")]
    [InlineData("saleDate,", "Each comma-separated sort clause needs a field.")]
    [InlineData("price desc", "'price' is not a sortable field. " + UseSortableFields)]
    [InlineData("id", "'id' is not a sortable field. " + UseSortableFields)]
    [InlineData("\"saleDate", "'\"saleDate' is not a sortable field. " + UseSortableFields)]
    [InlineData("saleDate down", "'down' is not a sort direction. Use asc or desc.")]
    [InlineData("saleDate desc asc", "'saleDate desc asc' must be a field, optionally followed by asc or desc.")]
    [InlineData("saleDate, SALEDATE desc", "'SALEDATE' appears more than once.")]
    public void Given_InvalidOrder_When_Parsing_Then_FailsWithReason(string order, string reason)
    {
        // When
        var parsed = SaleOrderParser.TryParse(order, out var sorts, out var error);

        // Then
        parsed.Should().BeFalse();
        error.Should().Be(reason);
        sorts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="SaleOrderParser.Parse"/>, which runs after validation, refuses an invalid order loudly.
    /// </summary>
    [Fact(DisplayName = "Given an invalid order When calling Parse Then it throws FormatException with the reason")]
    public void Given_InvalidOrder_When_CallingParse_Then_ThrowsFormatException()
    {
        // When
        var act = () => SaleOrderParser.Parse("price");

        // Then
        act.Should().Throw<FormatException>().WithMessage("'price' is not a sortable field. " + UseSortableFields);
    }
}
