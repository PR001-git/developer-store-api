using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
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
    /// Unit prices that are zero or negative.
    /// </summary>
    public static TheoryData<decimal> NonPositiveUnitPrices => new() { 0m, -4.50m };

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

    /// <summary>
    /// Tests rule R5: a sale needs at least one line.
    /// </summary>
    [Fact(DisplayName = "Given no items When creating a sale Then it throws DomainException")]
    public void Given_NoItems_When_CreatingSale_Then_ThrowsDomainException()
    {
        // When
        var act = () => Sale.Create(SaleTestData.GenerateSaleNumber(), DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), []);

        // Then
        act.Should().Throw<DomainException>().WithMessage("A sale must have at least one item");
    }

    /// <summary>
    /// Tests rule R4: identical items share a product, so a product can have only one line.
    /// </summary>
    [Fact(DisplayName = "Given two lines for the same product When creating a sale Then it throws DomainException")]
    public void Given_TwoLinesForSameProduct_When_CreatingSale_Then_ThrowsDomainException()
    {
        // Given
        var first = SaleTestData.GenerateItem(2, 4.50m);
        var second = first with { Product = new ExternalIdentity(first.Product.Id, "Another name"), Quantity = 3 };

        // When
        var act = () => SaleTestData.CreateSale(first, second);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Each product can appear only once in a sale");
    }

    /// <summary>
    /// Tests that the sale number is stored without surrounding spaces.
    /// </summary>
    [Fact(DisplayName = "Given a sale number with surrounding spaces When creating a sale Then the number is trimmed")]
    public void Given_SaleNumberWithSpaces_When_CreatingSale_Then_NumberIsTrimmed()
    {
        // When
        var sale = Sale.Create("  S-000123  ", DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        sale.SaleNumber.Should().Be("S-000123");
    }

    /// <summary>
    /// Tests that a sale number is required.
    /// </summary>
    /// <param name="saleNumber">The sale number to try.</param>
    [Theory(DisplayName = "Given a missing or blank sale number When creating a sale Then it throws DomainException")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_MissingOrBlankSaleNumber_When_CreatingSale_Then_ThrowsDomainException(string? saleNumber)
    {
        // When
        var act = () => Sale.Create(saleNumber!, DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Sale number must have 1 to 50 characters");
    }

    /// <summary>
    /// Tests that a sale number longer than 50 characters is rejected.
    /// </summary>
    [Fact(DisplayName = "Given a sale number of 51 characters When creating a sale Then it throws DomainException")]
    public void Given_SaleNumberOf51Characters_When_CreatingSale_Then_ThrowsDomainException()
    {
        // When
        var act = () => Sale.Create(new string('S', 51), DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Sale number must have 1 to 50 characters");
    }

    /// <summary>
    /// Tests that the length limit applies after trimming, so 50 characters plus spaces are accepted.
    /// </summary>
    [Fact(DisplayName = "Given a sale number of 50 characters with surrounding spaces When creating a sale Then it is accepted")]
    public void Given_SaleNumberOf50CharactersWithSpaces_When_CreatingSale_Then_IsAccepted()
    {
        // Given
        var saleNumber = new string('S', 50);

        // When
        var sale = Sale.Create($" {saleNumber} ", DateTime.UtcNow, SaleTestData.GenerateCustomer(),
            SaleTestData.GenerateBranch(), [SaleTestData.GenerateItem()]);

        // Then
        sale.SaleNumber.Should().Be(saleNumber);
    }

    /// <summary>
    /// Tests rule R1 at the aggregate: a line needs 1 to 20 identical items. <see cref="SaleItem"/> gets the
    /// percentage from the discount policy, so these pass as soon as lines are built; they pin that the sale enforces it.
    /// </summary>
    /// <param name="quantity">A quantity outside 1 to 20.</param>
    /// <param name="message">The expected message.</param>
    [Theory(DisplayName = "Given a quantity outside 1 to 20 When creating a sale Then it throws DomainException")]
    [InlineData(0, "Quantity must be at least 1")]
    [InlineData(21, "It's not possible to sell above 20 identical items")]
    public void Given_QuantityOutside1To20_When_CreatingSale_Then_ThrowsDomainException(int quantity, string message)
    {
        // When
        var act = () => SaleTestData.CreateSale(SaleTestData.GenerateItem(quantity, 4.50m));

        // Then
        act.Should().Throw<DomainException>().WithMessage(message);
    }

    /// <summary>
    /// Tests that a unit price must be above zero.
    /// </summary>
    /// <param name="unitPrice">A unit price that is zero or negative.</param>
    [Theory(DisplayName = "Given a unit price of zero or less When creating a sale Then it throws DomainException")]
    [MemberData(nameof(NonPositiveUnitPrices))]
    public void Given_UnitPriceOfZeroOrLess_When_CreatingSale_Then_ThrowsDomainException(decimal unitPrice)
    {
        // When
        var act = () => SaleTestData.CreateSale(SaleTestData.GenerateItem(1, unitPrice));

        // Then
        act.Should().Throw<DomainException>().WithMessage("Unit price must be greater than zero");
    }

    /// <summary>
    /// Tests that a unit price can't have fractions of a cent.
    /// </summary>
    [Fact(DisplayName = "Given a unit price with 3 decimal places When creating a sale Then it throws DomainException")]
    public void Given_UnitPriceWith3DecimalPlaces_When_CreatingSale_Then_ThrowsDomainException()
    {
        // When
        var act = () => SaleTestData.CreateSale(SaleTestData.GenerateItem(1, 4.455m));

        // Then
        act.Should().Throw<DomainException>().WithMessage("Unit price must have at most 2 decimal places");
    }

    /// <summary>
    /// Tests that the decimal-places rule looks at the value, so trailing zeros (as JSON can send them) are fine.
    /// </summary>
    [Fact(DisplayName = "Given a unit price of 4.500 When creating a sale Then it is accepted")]
    public void Given_UnitPriceWithTrailingZero_When_CreatingSale_Then_IsAccepted()
    {
        // When
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(1, 4.500m));

        // Then
        sale.Items.Should().ContainSingle().Which.UnitPrice.Should().Be(4.50m);
    }
}
