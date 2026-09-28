using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
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

    /// <summary>
    /// Tests that creating a sale records exactly one <see cref="SaleCreatedEvent"/> with the §5.4 payload,
    /// stamped with the creation time.
    /// </summary>
    [Fact(DisplayName = "Given valid input When creating a sale Then it records one SaleCreatedEvent with the sale's data")]
    public void Given_ValidInput_When_CreatingSale_Then_RecordsSaleCreatedEvent()
    {
        // When
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));

        // Then
        sale.DomainEvents.Should().ContainSingle().Which.Should().Be(new SaleCreatedEvent(
            SaleId: sale.Id,
            SaleNumber: sale.SaleNumber,
            CustomerId: sale.Customer.Id,
            BranchId: sale.Branch.Id,
            TotalAmount: 46.20m,
            ItemCount: 2,
            OccurredAt: sale.CreatedAt));
    }

    /// <summary>
    /// Tests that the recorded events can be cleared once they have been published.
    /// </summary>
    [Fact(DisplayName = "Given a new sale with a recorded event When clearing its domain events Then none remain")]
    public void Given_NewSaleWithRecordedEvent_When_ClearingDomainEvents_Then_NoneRemain()
    {
        // Given
        var sale = SaleTestData.CreateSale();

        // When
        sale.ClearDomainEvents();

        // Then
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that cancelling marks the sale cancelled and sets <see cref="Sale.UpdatedAt"/> to now, in UTC (rule R13).
    /// </summary>
    [Fact(DisplayName = "Given an open sale When cancelling it Then it is cancelled and updated now in UTC")]
    public void Given_OpenSale_When_Cancelling_Then_IsCancelledAndUpdatedNowInUtc()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        var before = DateTime.UtcNow;

        // When
        sale.Cancel();

        // Then
        var after = DateTime.UtcNow;
        sale.IsCancelled.Should().BeTrue();
        sale.UpdatedAt.Should().NotBeNull().And.BeOnOrAfter(before).And.BeOnOrBefore(after);
        sale.UpdatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests rule R8: a cancelled sale keeps its lines and total as the historical record.
    /// </summary>
    [Fact(DisplayName = "Given a sale with discounted lines When cancelling it Then its items and total stay unchanged")]
    public void Given_SaleWithLines_When_Cancelling_Then_ItemsAndTotalUnchanged()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var itemsBefore = sale.Items
            .Select(item => new
            {
                item.Id, item.Product, item.Quantity, item.UnitPrice,
                item.DiscountPercentage, item.DiscountAmount, item.TotalAmount, item.IsCancelled
            })
            .ToList();

        // When
        sale.Cancel();

        // Then (16.20 + 30.00)
        sale.Items.Should().BeEquivalentTo(itemsBefore, options => options.WithStrictOrdering());
        sale.TotalAmount.Should().Be(46.20m);
    }

    /// <summary>
    /// Tests that cancelling records exactly one <see cref="SaleCancelledEvent"/> with the §5.4 payload,
    /// stamped with <see cref="Sale.UpdatedAt"/>.
    /// </summary>
    [Fact(DisplayName = "Given a loaded open sale When cancelling it Then it records one SaleCancelledEvent with the sale's data")]
    public void Given_LoadedOpenSale_When_Cancelling_Then_RecordsSaleCancelledEvent()
    {
        // Given (a loaded sale has no recorded events)
        var sale = SaleTestData.CreateSale();
        sale.ClearDomainEvents();

        // When
        sale.Cancel();

        // Then
        sale.DomainEvents.Should().ContainSingle().Which.Should().Be(
            new SaleCancelledEvent(SaleId: sale.Id, SaleNumber: sale.SaleNumber, OccurredAt: sale.UpdatedAt!.Value));
    }

    /// <summary>
    /// Tests rule R7: a cancelled sale is read-only, so a second cancel is rejected and changes nothing.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When cancelling it again Then it throws DomainException and changes nothing")]
    public void Given_CancelledSale_When_CancellingAgain_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        sale.Cancel();
        sale.ClearDomainEvents();
        var updatedAt = sale.UpdatedAt;

        // When
        var act = () => sale.Cancel();

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        sale.UpdatedAt.Should().Be(updatedAt);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R9: a cancelled line drops out of the total and keeps its amounts as history, and the sale stays open
    /// while another line is active.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active lines When cancelling one Then that line is cancelled, the total drops to the other line and the sale stays open")]
    public void Given_SaleWithTwoActiveLines_When_CancellingOne_Then_TotalDropsAndSaleStaysOpen()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var cancelled = sale.Items.First();
        var active = sale.Items.Last();

        // When
        sale.CancelItem(cancelled.Id);

        // Then (the cancelled line keeps its 16.20; 30.00 is left)
        cancelled.IsCancelled.Should().BeTrue();
        cancelled.TotalAmount.Should().Be(16.20m);
        active.IsCancelled.Should().BeFalse();
        sale.TotalAmount.Should().Be(30.00m);
        sale.IsCancelled.Should().BeFalse();
    }

    /// <summary>
    /// Tests rule R13: cancelling an item sets <see cref="Sale.UpdatedAt"/> to now, in UTC, so the <c>Sales</c> row
    /// is written and its concurrency token covers item-only changes.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When cancelling an item Then UpdatedAt is set to now in UTC")]
    public void Given_OpenSale_When_CancellingItem_Then_UpdatedNowInUtc()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(), SaleTestData.GenerateItem());
        var before = DateTime.UtcNow;

        // When
        sale.CancelItem(sale.Items.First().Id);

        // Then
        var after = DateTime.UtcNow;
        sale.UpdatedAt.Should().NotBeNull().And.BeOnOrAfter(before).And.BeOnOrBefore(after);
        sale.UpdatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that cancelling a line records exactly one <see cref="ItemCancelledEvent"/> with the §5.4 payload,
    /// stamped with <see cref="Sale.UpdatedAt"/>.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale with two active lines When cancelling one Then it records one ItemCancelledEvent with the sale's and the line's data")]
    public void Given_LoadedSaleWithTwoActiveLines_When_CancellingOne_Then_RecordsItemCancelledEvent()
    {
        // Given (a loaded sale has no recorded events)
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(), SaleTestData.GenerateItem());
        sale.ClearDomainEvents();
        var item = sale.Items.First();

        // When
        sale.CancelItem(item.Id);

        // Then
        sale.DomainEvents.Should().ContainSingle().Which.Should().Be(new ItemCancelledEvent(
            SaleId: sale.Id,
            SaleNumber: sale.SaleNumber,
            ItemId: item.Id,
            ProductId: item.Product.Id,
            OccurredAt: sale.UpdatedAt!.Value));
    }

    /// <summary>
    /// Tests rule R6 and decision D5: cancelling the last active line cancels the sale too, the total becomes 0,
    /// and <see cref="ItemCancelledEvent"/> is recorded before <see cref="SaleCancelledEvent"/>, both with one timestamp.
    /// </summary>
    [Fact(DisplayName = "Given a sale with one active line left When cancelling it Then the sale is cancelled with total 0, and ItemCancelledEvent then SaleCancelledEvent are recorded")]
    public void Given_SaleWithOneActiveLineLeft_When_CancellingIt_Then_SaleCancelledWithZeroTotal()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        sale.CancelItem(sale.Items.First().Id);
        sale.ClearDomainEvents();
        var last = sale.Items.Last();

        // When
        sale.CancelItem(last.Id);

        // Then
        sale.IsCancelled.Should().BeTrue();
        sale.TotalAmount.Should().Be(0m);
        sale.Items.Should().OnlyContain(item => item.IsCancelled);
        sale.DomainEvents.Should().Equal(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, last.Id, last.Product.Id, sale.UpdatedAt!.Value),
            new SaleCancelledEvent(sale.Id, sale.SaleNumber, sale.UpdatedAt!.Value));
    }

    /// <summary>
    /// Tests rule R7: a cancelled sale is read-only, so cancelling one of its lines is rejected and changes nothing.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When cancelling one of its lines Then it throws DomainException and changes nothing")]
    public void Given_CancelledSale_When_CancellingLine_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.Cancel();
        sale.ClearDomainEvents();
        var updatedAt = sale.UpdatedAt;
        var item = sale.Items.Single();

        // When
        var act = () => sale.CancelItem(item.Id);

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        item.IsCancelled.Should().BeFalse();
        sale.TotalAmount.Should().Be(16.20m);
        sale.UpdatedAt.Should().Be(updatedAt);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R9: a line that is already cancelled can't be cancelled again, and nothing changes.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled line When cancelling it again Then it throws DomainException and changes nothing")]
    public void Given_CancelledLine_When_CancellingAgain_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var item = sale.Items.First();
        sale.CancelItem(item.Id);
        sale.ClearDomainEvents();
        var updatedAt = sale.UpdatedAt;

        // When
        var act = () => sale.CancelItem(item.Id);

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Item {item.Id} of sale {sale.SaleNumber} is already cancelled");
        sale.TotalAmount.Should().Be(30.00m);
        sale.IsCancelled.Should().BeFalse();
        sale.UpdatedAt.Should().Be(updatedAt);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests the domain's defence in depth: an id that isn't one of the sale's lines is rejected, and nothing changes.
    /// </summary>
    [Fact(DisplayName = "Given an item id that isn't in the sale When cancelling it Then it throws DomainException and changes nothing")]
    public void Given_ItemIdNotInSale_When_CancellingIt_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.ClearDomainEvents();
        var itemId = Guid.NewGuid();

        // When
        var act = () => sale.CancelItem(itemId);

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} has no item with ID {itemId}");
        sale.Items.Should().OnlyContain(item => !item.IsCancelled);
        sale.UpdatedAt.Should().BeNull();
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rules R10 and R13: an update replaces the customer, the branch and the sale date, which is read as UTC
    /// when it has no kind. The sale number stays the same (rule R12).
    /// </summary>
    [Fact(DisplayName = "Given an open sale When updating it with a date without a kind, a new customer and a new branch Then the header is replaced, the date is in UTC and the number is kept")]
    public void Given_OpenSale_When_UpdatingHeader_Then_HeaderReplacedInUtcAndNumberKept()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        var saleNumber = sale.SaleNumber;
        var customer = SaleTestData.GenerateCustomer();
        var branch = SaleTestData.GenerateBranch();

        // When
        sale.Update(new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Unspecified), customer, branch,
            [SaleTestData.GenerateItem(sale.Items.Single().Product.Id, 2, 8.00m)]);

        // Then
        sale.SaleDate.Should().Be(new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc));
        sale.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
        sale.Customer.Should().Be(customer);
        sale.Branch.Should().Be(branch);
        sale.SaleNumber.Should().Be(saleNumber);
    }

    /// <summary>
    /// Tests rule R10: an active line whose product is sent keeps its id, and gets the new quantity, price and name
    /// and the discount of the new quantity.
    /// </summary>
    [Fact(DisplayName = "Given an active line of 4 items at 10% When updating its product to 10 items with a new price and name Then the line keeps its id and gets 20% and the new amounts")]
    public void Given_ActiveLineAt10Percent_When_UpdatingItsProductTo10Items_Then_LineKeepsIdAndGets20Percent()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        var line = sale.Items.Single();
        var lineId = line.Id;
        var sent = SaleTestData.GenerateItem(line.Product.Id, 10, 5.00m);

        // When
        UpdateLines(sale, sent);

        // Then (10 × 5.00 = 50.00; 20% of it is 10.00)
        sale.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = lineId,
            sent.Product,
            Quantity = 10,
            UnitPrice = 5.00m,
            DiscountPercentage = 20m,
            DiscountAmount = 10.00m,
            TotalAmount = 40.00m,
            IsCancelled = false
        });
    }

    /// <summary>
    /// Tests rule R10: a sent product with no line gets a new active line, with its own new id and its discount.
    /// </summary>
    [Fact(DisplayName = "Given a sale with one line When updating it with that product and a new one Then the new product gets a new active line with its discount")]
    public void Given_SaleWithOneLine_When_UpdatingWithNewProduct_Then_NewProductGetsNewLine()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        var existing = sale.Items.Single();
        var added = SaleTestData.GenerateItem(5, 4.45m);

        // When
        UpdateLines(sale, SaleTestData.GenerateItem(existing.Product.Id, 4, 4.50m), added);

        // Then (5 × 4.45 = 22.25; its 10% is 2.225, which rounds away from zero to 2.23)
        sale.Items.Should().HaveCount(2);
        var newLine = sale.Items.Should().ContainSingle(item => item.Product.Id == added.Product.Id).Subject;
        newLine.Id.Should().NotBe(Guid.Empty).And.NotBe(existing.Id);
        newLine.Should().BeEquivalentTo(new
        {
            added.Product,
            Quantity = 5,
            UnitPrice = 4.45m,
            DiscountPercentage = 10m,
            DiscountAmount = 2.23m,
            TotalAmount = 20.02m,
            IsCancelled = false
        });
    }

    /// <summary>
    /// Tests rule R10 and spec decision D6: an active line whose product isn't sent is cancelled, not removed. It keeps
    /// its amounts as history and no longer counts in the total.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active lines When updating it without the first product Then that line is cancelled with its amounts kept and leaves the total")]
    public void Given_SaleWithTwoActiveLines_When_UpdatingWithoutFirstProduct_Then_ThatLineIsCancelledAndKeepsAmounts()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var dropped = sale.Items.First();
        var kept = sale.Items.Last();

        // When
        UpdateLines(sale, SaleTestData.GenerateItem(kept.Product.Id, 3, 10.00m));

        // Then (the dropped line keeps its 16.20; 30.00 is left)
        sale.Items.Should().HaveCount(2).And.Contain(dropped);
        dropped.Should().BeEquivalentTo(new { Quantity = 4, UnitPrice = 4.50m, DiscountAmount = 1.80m, TotalAmount = 16.20m, IsCancelled = true });
        kept.IsCancelled.Should().BeFalse();
        sale.TotalAmount.Should().Be(30.00m);
    }

    /// <summary>
    /// Tests rule R10: a product whose only line was cancelled gets a new active line. The cancelled line stays as it
    /// was, because cancelled lines never change.
    /// </summary>
    [Fact(DisplayName = "Given a product whose only line was cancelled When updating the sale with it again Then it gets a new active line and the cancelled line stays unchanged")]
    public void Given_ProductWhoseLineWasCancelled_When_UpdatingWithItAgain_Then_GetsNewLineAndCancelledLineUnchanged()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var cancelled = sale.Items.First();
        var cancelledProduct = cancelled.Product;
        var active = sale.Items.Last();
        sale.CancelItem(cancelled.Id);

        // When
        UpdateLines(sale,
            SaleTestData.GenerateItem(cancelledProduct.Id, 10, 4.50m),
            SaleTestData.GenerateItem(active.Product.Id, 3, 10.00m));

        // Then (the new line is 10 × 4.50 at 20% = 36.00; with the other 30.00 the total is 66.00)
        sale.Items.Should().HaveCount(3);
        cancelled.Should().BeEquivalentTo(new { Product = cancelledProduct, Quantity = 4, TotalAmount = 16.20m, IsCancelled = true });
        sale.Items.Should().ContainSingle(item => item.Product.Id == cancelledProduct.Id && !item.IsCancelled)
            .Which.Should().BeEquivalentTo(new { Quantity = 10, DiscountPercentage = 20m, TotalAmount = 36.00m });
        sale.TotalAmount.Should().Be(66.00m);
    }

    /// <summary>
    /// Tests rules R3 and R13 after an update: the total is the sum of the active lines, and
    /// <see cref="Sale.UpdatedAt"/> is now, in UTC.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When updating its lines Then the total is the sum of the active lines and UpdatedAt is now in UTC")]
    public void Given_OpenSale_When_UpdatingLines_Then_TotalOfActiveLinesAndUpdatedNowInUtc()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        var before = DateTime.UtcNow;

        // When (the first line goes to 10 × 4.50 at 20%, the second is dropped, and 2 × 8.00 is added)
        UpdateLines(sale,
            SaleTestData.GenerateItem(sale.Items.First().Product.Id, 10, 4.50m),
            SaleTestData.GenerateItem(2, 8.00m));

        // Then (36.00 + 16.00; the dropped 30.00 doesn't count)
        var after = DateTime.UtcNow;
        sale.TotalAmount.Should().Be(52.00m);
        sale.UpdatedAt.Should().NotBeNull().And.BeOnOrAfter(before).And.BeOnOrBefore(after);
        sale.UpdatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests the event sequence of §5.3: one <see cref="ItemCancelledEvent"/> per cancelled line, in line order, then
    /// <see cref="SaleModifiedEvent"/> with the new total, all stamped with <see cref="Sale.UpdatedAt"/>.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale with three lines When updating it with only the third product Then it records ItemCancelledEvent for the first two lines, then SaleModifiedEvent")]
    public void Given_LoadedSaleWithThreeLines_When_UpdatingWithOnlyThirdProduct_Then_RecordsItemCancelledTwiceThenSaleModified()
    {
        // Given (a loaded sale has no recorded events)
        var sale = SaleTestData.CreateSale(
            SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m), SaleTestData.GenerateItem(2, 8.00m));
        sale.ClearDomainEvents();
        var first = sale.Items.ElementAt(0);
        var second = sale.Items.ElementAt(1);
        var third = sale.Items.ElementAt(2);

        // When
        UpdateLines(sale, SaleTestData.GenerateItem(third.Product.Id, 2, 8.00m));

        // Then
        var updatedAt = sale.UpdatedAt!.Value;
        sale.DomainEvents.Should().Equal(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, first.Id, first.Product.Id, updatedAt),
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, second.Id, second.Product.Id, updatedAt),
            new SaleModifiedEvent(sale.Id, sale.SaleNumber, 16.00m, updatedAt));
    }

    /// <summary>
    /// Tests §5.3: an update that changes nothing still records <see cref="SaleModifiedEvent"/>, and only that. The
    /// lines keep their ids and amounts, and none is cancelled.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale When updating it with the same header and lines Then only SaleModifiedEvent is recorded and the lines are unchanged")]
    public void Given_LoadedSale_When_UpdatingWithSameHeaderAndLines_Then_OnlySaleModifiedIsRecorded()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        sale.ClearDomainEvents();
        var itemsBefore = sale.Items
            .Select(item => new
            {
                item.Id, item.Product, item.Quantity, item.UnitPrice,
                item.DiscountPercentage, item.DiscountAmount, item.TotalAmount, item.IsCancelled
            })
            .ToList();
        var sameLines = sale.Items.Select(item => new SaleItemData(item.Product, item.Quantity, item.UnitPrice)).ToArray();

        // When
        UpdateLines(sale, sameLines);

        // Then (16.20 + 30.00)
        sale.DomainEvents.Should().ContainSingle().Which.Should().Be(
            new SaleModifiedEvent(sale.Id, sale.SaleNumber, 46.20m, sale.UpdatedAt!.Value));
        sale.Items.Should().BeEquivalentTo(itemsBefore, options => options.WithStrictOrdering());
    }

    /// <summary>
    /// Tests rule R7: a cancelled sale is read-only, so an update is rejected and changes nothing.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When updating it Then it throws DomainException and changes nothing")]
    public void Given_CancelledSale_When_Updating_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.Cancel();
        sale.ClearDomainEvents();
        var before = StateOf(sale);

        // When
        var act = () => sale.Update(DateTime.UtcNow, SaleTestData.GenerateCustomer(), SaleTestData.GenerateBranch(),
            [SaleTestData.GenerateItem(sale.Items.Single().Product.Id, 10, 4.50m)]);

        // Then
        act.Should().Throw<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        StateOf(sale).Should().BeEquivalentTo(before);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R5 in an update: the lines can't be emptied, and a rejected update changes nothing.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When updating it with no items Then it throws DomainException and changes nothing")]
    public void Given_OpenSale_When_UpdatingWithNoItems_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.ClearDomainEvents();
        var before = StateOf(sale);

        // When
        var act = () => sale.Update(DateTime.UtcNow, SaleTestData.GenerateCustomer(), SaleTestData.GenerateBranch(), []);

        // Then
        act.Should().Throw<DomainException>().WithMessage("A sale must have at least one item");
        StateOf(sale).Should().BeEquivalentTo(before);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R4 in an update: a product sent twice is rejected, and nothing changes.
    /// </summary>
    [Fact(DisplayName = "Given an open sale When updating it with the same product twice Then it throws DomainException and changes nothing")]
    public void Given_OpenSale_When_UpdatingWithSameProductTwice_Then_ThrowsAndChangesNothing()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.ClearDomainEvents();
        var productId = sale.Items.Single().Product.Id;
        var before = StateOf(sale);

        // When
        var act = () => sale.Update(DateTime.UtcNow, SaleTestData.GenerateCustomer(), SaleTestData.GenerateBranch(),
            [SaleTestData.GenerateItem(productId, 10, 4.50m), SaleTestData.GenerateItem(productId, 2, 4.50m)]);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Each product can appear only once in a sale");
        StateOf(sale).Should().BeEquivalentTo(before);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R1 in an update: a quantity outside 1 to 20 is rejected. The valid line sent before it changes
    /// nothing either, because every line is checked before anything changes.
    /// </summary>
    /// <param name="quantity">A quantity outside 1 to 20.</param>
    /// <param name="message">The expected message, the same as create's.</param>
    [Theory(DisplayName = "Given an open sale When updating it with a valid line and one with a quantity outside 1 to 20 Then it throws DomainException and changes nothing")]
    [InlineData(0, "Quantity must be at least 1")]
    [InlineData(21, "It's not possible to sell above 20 identical items")]
    public void Given_OpenSale_When_UpdatingWithQuantityOutside1To20_Then_ThrowsAndChangesNothing(int quantity, string message)
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        sale.ClearDomainEvents();
        var before = StateOf(sale);

        // When
        var act = () => sale.Update(DateTime.UtcNow, SaleTestData.GenerateCustomer(), SaleTestData.GenerateBranch(),
            [SaleTestData.GenerateItem(sale.Items.Single().Product.Id, 10, 4.50m), SaleTestData.GenerateItem(quantity, 4.50m)]);

        // Then
        act.Should().Throw<DomainException>().WithMessage(message);
        StateOf(sale).Should().BeEquivalentTo(before);
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Updates the sale's lines and keeps its header, for the tests that are about the lines.
    /// </summary>
    private static void UpdateLines(Sale sale, params SaleItemData[] items) =>
        sale.Update(sale.SaleDate, sale.Customer, sale.Branch, items);

    /// <summary>
    /// Captures everything an update can change, so a test can check that a rejected update changed nothing.
    /// </summary>
    private static object StateOf(Sale sale) => new
    {
        sale.SaleDate,
        sale.Customer,
        sale.Branch,
        sale.TotalAmount,
        sale.IsCancelled,
        sale.UpdatedAt,
        Items = sale.Items
            .Select(item => new
            {
                item.Id, item.Product, item.Quantity, item.UnitPrice,
                item.DiscountPercentage, item.DiscountAmount, item.TotalAmount, item.IsCancelled
            })
            .ToList()
    };
}
