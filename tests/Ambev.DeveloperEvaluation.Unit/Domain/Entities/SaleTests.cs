using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

/// <summary>
/// Contains unit tests for the <see cref="Sale"/> aggregate.
/// </summary>
public sealed class SaleTests
{
    /// <summary>
    /// One line per row: quantity, unit price, then the expected discount percentage, discount amount and line total.
    /// </summary>
    public static TheoryData<int, decimal, decimal, decimal, decimal> LinesAtEachTier => new()
    {
        { 3, 10.00m, 0m, 0.00m, 30.00m },
        { 4, 4.50m, 10m, 1.80m, 16.20m },
        { 5, 4.45m, 10m, 2.23m, 20.02m },
        { 10, 4.50m, 20m, 9.00m, 36.00m },
        { 20, 0.33m, 20m, 1.32m, 5.28m }
    };

    /// <summary>
    /// Tests rules R1 and R3 on one line. 5 × 4.45 = 22.25, whose 10% is 2.225: rounding away from zero gives 2.23,
    /// where the default banker's rounding would give 2.22.
    /// </summary>
    [Theory(DisplayName = "Given a line at a discount tier When creating a sale Then the line gets the tier's discount, rounded away from zero, and its total")]
    [MemberData(nameof(LinesAtEachTier))]
    public void Given_LineAtDiscountTier_When_CreatingSale_Then_LineGetsDiscountAndTotal(
        int quantity, decimal unitPrice, decimal discountPercentage, decimal discountAmount, decimal totalAmount)
    {
        // Given
        var item = SaleTestData.GenerateItem(quantity, unitPrice);

        // When
        var sale = SaleTestData.CreateSale(item);

        // Then
        sale.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            item.Product,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountPercentage = discountPercentage,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            IsCancelled = false
        });
    }

    /// <summary>
    /// Tests that the sale total is the sum of its line totals (rule R3).
    /// </summary>
    [Fact(DisplayName = "Given two lines When creating a sale Then the sale total is the sum of the line totals")]
    public void Given_TwoLines_When_CreatingSale_Then_TotalIsSumOfLineTotals()
    {
        // Given
        var discounted = SaleTestData.GenerateItem(4, 4.50m);
        var fullPrice = SaleTestData.GenerateItem(3, 10.00m);

        // When
        var sale = SaleTestData.CreateSale(discounted, fullPrice);

        // Then (16.20 + 30.00)
        sale.TotalAmount.Should().Be(46.20m);
    }

    /// <summary>
    /// Tests that the domain creates the sale id and the item ids.
    /// </summary>
    [Fact(DisplayName = "Given two lines When creating a sale Then the sale and each item get their own new id")]
    public void Given_TwoLines_When_CreatingSale_Then_SaleAndItemsGetNewIds()
    {
        // When
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(), SaleTestData.GenerateItem());

        // Then
        var ids = sale.Items.Select(item => item.Id).Append(sale.Id).ToList();
        ids.Should().NotContain(Guid.Empty).And.OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Tests that the lines can't be changed from outside the aggregate, even through a cast.
    /// </summary>
    [Fact(DisplayName = "Given a sale When clearing its items from outside Then it throws NotSupportedException and the items stay")]
    public void Given_Sale_When_ClearingItemsFromOutside_Then_ThrowsAndItemsStay()
    {
        // Given
        var sale = SaleTestData.CreateSale();

        // When
        var act = () => ((ICollection<SaleItem>)sale.Items).Clear();

        // Then
        act.Should().Throw<NotSupportedException>();
        sale.Items.Should().ContainSingle();
    }

    /// <summary>
    /// Tests that a new sale is open: not cancelled, not deleted and never updated, with its creation time in UTC.
    /// </summary>
    [Fact(DisplayName = "Given valid input When creating a sale Then it is open, not deleted, never updated, and created now in UTC")]
    public void Given_ValidInput_When_CreatingSale_Then_IsOpenAndCreatedNowInUtc()
    {
        // Given
        var before = DateTime.UtcNow;

        // When
        var sale = SaleTestData.CreateSale();

        // Then
        var after = DateTime.UtcNow;
        sale.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        sale.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        sale.Should().BeEquivalentTo(new { IsCancelled = false, IsDeleted = false, DeletedAt = (DateTime?)null, UpdatedAt = (DateTime?)null });
    }

    /// <summary>
    /// Tests rule R13: a sale date in UTC, or without a kind (no offset in the JSON), is stored as that UTC time.
    /// </summary>
    /// <param name="kind">The kind of the sale date that is sent.</param>
    [Theory(DisplayName = "Given a sale date in UTC or without a kind When creating a sale Then it is stored as that time in UTC")]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Given_SaleDateInUtcOrWithoutKind_When_CreatingSale_Then_StoredAsThatTimeInUtc(DateTimeKind kind)
    {
        // Given
        var saleDate = new DateTime(2026, 9, 24, 14, 30, 0, kind);

        // When
        var sale = Sale.Create(SaleTestData.GenerateSaleNumber(), saleDate, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        sale.SaleDate.Should().Be(new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc));
        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests rule R13: a local sale date (JSON with an offset arrives as local time) is converted to UTC.
    /// </summary>
    [Fact(DisplayName = "Given a local sale date When creating a sale Then it is converted to UTC")]
    public void Given_LocalSaleDate_When_CreatingSale_Then_ConvertedToUtc()
    {
        // Given
        var saleDate = new DateTime(2026, 9, 24, 11, 30, 0, DateTimeKind.Local);

        // When
        var sale = Sale.Create(SaleTestData.GenerateSaleNumber(), saleDate, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        sale.SaleDate.Should().Be(new DateTimeOffset(saleDate).UtcDateTime);
        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
    }
}
