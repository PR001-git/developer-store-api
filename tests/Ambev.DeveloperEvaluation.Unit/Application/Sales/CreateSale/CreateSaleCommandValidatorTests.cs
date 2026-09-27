using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CreateSale;

/// <summary>
/// Contains unit tests for <see cref="CreateSaleCommandValidator"/>, one per rule. Each invalid case checks that
/// its failure is the only one, so the random valid data can't hide another problem.
/// </summary>
public sealed class CreateSaleCommandValidatorTests
{
    private readonly CreateSaleCommandValidator _validator = new();

    /// <summary>
    /// Unit prices that are zero or negative.
    /// </summary>
    public static TheoryData<decimal> NonPositiveUnitPrices => new() { 0m, -4.50m };

    /// <summary>
    /// Tests that a command with valid data has no failures.
    /// </summary>
    [Fact(DisplayName = "Given a valid command When validating Then there are no failures")]
    public void Given_ValidCommand_When_Validating_Then_NoFailures()
    {
        // When
        var result = _validator.TestValidate(CreateSaleCommandTestData.GenerateValidCommand());

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that the sale number is required. For now the client always sends it; ticket 06 makes it optional.
    /// </summary>
    /// <param name="saleNumber">The sale number to try.</param>
    [Theory(DisplayName = "Given a missing or blank sale number When validating Then SaleNumber fails as empty")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_MissingOrBlankSaleNumber_When_Validating_Then_SaleNumberFails(string? saleNumber)
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = saleNumber!;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.SaleNumber).WithErrorMessage("'Sale Number' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the sale number has at most 50 characters after trimming.
    /// </summary>
    [Fact(DisplayName = "Given a sale number of 51 characters When validating Then SaleNumber fails as too long")]
    public void Given_SaleNumberOf51Characters_When_Validating_Then_SaleNumberFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = new string('S', 51);

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.SaleNumber)
            .WithErrorMessage("'Sale Number' must have at most 50 characters after trimming.").Only();
    }

    /// <summary>
    /// Tests that surrounding spaces don't count towards the sale number's length, as the domain trims them.
    /// </summary>
    [Fact(DisplayName = "Given a sale number of 50 characters with surrounding spaces When validating Then there are no failures")]
    public void Given_SaleNumberOf50CharactersWithSpaces_When_Validating_Then_NoFailures()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleNumber = $"  {new string('S', 50)}  ";

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that the sale date is required. A missing <c>saleDate</c> in the JSON arrives as <see cref="DateTime.MinValue"/>.
    /// </summary>
    [Fact(DisplayName = "Given no sale date When validating Then SaleDate fails as empty")]
    public void Given_NoSaleDate_When_Validating_Then_SaleDateFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.SaleDate = default;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.SaleDate).WithErrorMessage("'Sale Date' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the customer id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty customer id When validating Then CustomerId fails as empty")]
    public void Given_EmptyCustomerId_When_Validating_Then_CustomerIdFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.CustomerId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.CustomerId).WithErrorMessage("'Customer Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the customer name is required.
    /// </summary>
    [Fact(DisplayName = "Given a blank customer name When validating Then CustomerName fails as empty")]
    public void Given_BlankCustomerName_When_Validating_Then_CustomerNameFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.CustomerName = "   ";

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.CustomerName).WithErrorMessage("'Customer Name' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the branch id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty branch id When validating Then BranchId fails as empty")]
    public void Given_EmptyBranchId_When_Validating_Then_BranchIdFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.BranchId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.BranchId).WithErrorMessage("'Branch Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the branch name has at most 100 characters after trimming.
    /// </summary>
    [Fact(DisplayName = "Given a branch name of 101 characters When validating Then BranchName fails as too long")]
    public void Given_BranchNameOf101Characters_When_Validating_Then_BranchNameFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.BranchName = new string('b', 101);

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.BranchName)
            .WithErrorMessage("'Branch Name' must have at most 100 characters after trimming.").Only();
    }

    /// <summary>
    /// Tests rule R5: a sale needs at least one line.
    /// </summary>
    [Fact(DisplayName = "Given no items When validating Then Items fails with the no-items message")]
    public void Given_NoItems_When_Validating_Then_ItemsFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        command.Items = [];

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Items).WithErrorMessage("A sale must have at least one item").Only();
    }

    /// <summary>
    /// Tests rule R4: a product can have only one line.
    /// </summary>
    [Fact(DisplayName = "Given two items for the same product When validating Then Items fails with the repeated-product message")]
    public void Given_TwoItemsForSameProduct_When_Validating_Then_ItemsFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 2);
        command.Items[1].ProductId = command.Items[0].ProductId;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Items).WithErrorMessage("Each product can appear only once in a sale").Only();
    }

    /// <summary>
    /// Tests that a missing line (<c>"items": [null]</c> in the JSON) is a validation failure, not a crash.
    /// </summary>
    [Fact(DisplayName = "Given a null item When validating Then Items[0] fails as empty")]
    public void Given_NullItem_When_Validating_Then_ItemFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items = [null!];

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0]").WithErrorMessage("'Items' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that each line needs a product id.
    /// </summary>
    [Fact(DisplayName = "Given an item with an empty product id When validating Then Items[0].ProductId fails as empty")]
    public void Given_ItemWithEmptyProductId_When_Validating_Then_ProductIdFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].ProductId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].ProductId").WithErrorMessage("'Product Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that each line needs a product name.
    /// </summary>
    [Fact(DisplayName = "Given an item with a blank product name When validating Then Items[0].ProductName fails as empty")]
    public void Given_ItemWithBlankProductName_When_Validating_Then_ProductNameFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].ProductName = string.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].ProductName").WithErrorMessage("'Product Name' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that surrounding spaces don't count towards a name's length, as the domain trims them.
    /// </summary>
    [Fact(DisplayName = "Given a product name of 100 characters with surrounding spaces When validating Then there are no failures")]
    public void Given_ProductNameOf100CharactersWithSpaces_When_Validating_Then_NoFailures()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].ProductName = $" {new string('p', 100)} ";

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests rule R1's range: a quantity below 1, or above 20 with R1's own message.
    /// </summary>
    /// <param name="quantity">A quantity outside 1 to 20.</param>
    /// <param name="message">The expected message.</param>
    [Theory(DisplayName = "Given an item with a quantity outside 1 to 20 When validating Then Items[0].Quantity fails")]
    [InlineData(0, "'Quantity' must be greater than or equal to '1'.")]
    [InlineData(21, "It's not possible to sell above 20 identical items")]
    public void Given_ItemWithQuantityOutside1To20_When_Validating_Then_QuantityFails(int quantity, string message)
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].Quantity = quantity;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].Quantity").WithErrorMessage(message).Only();
    }

    /// <summary>
    /// Tests that the quantity limits themselves are accepted.
    /// </summary>
    /// <param name="quantity">1 or 20.</param>
    [Theory(DisplayName = "Given an item with a quantity of 1 or 20 When validating Then there are no failures")]
    [InlineData(1)]
    [InlineData(20)]
    public void Given_ItemWithQuantityAtLimit_When_Validating_Then_NoFailures(int quantity)
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].Quantity = quantity;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that a unit price must be above zero.
    /// </summary>
    /// <param name="unitPrice">A unit price that is zero or negative.</param>
    [Theory(DisplayName = "Given an item with a unit price of zero or less When validating Then Items[0].UnitPrice fails")]
    [MemberData(nameof(NonPositiveUnitPrices))]
    public void Given_ItemWithUnitPriceOfZeroOrLess_When_Validating_Then_UnitPriceFails(decimal unitPrice)
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].UnitPrice = unitPrice;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice").WithErrorMessage("'Unit Price' must be greater than '0'.").Only();
    }

    /// <summary>
    /// Tests that a unit price can't have fractions of a cent.
    /// </summary>
    [Fact(DisplayName = "Given an item with a unit price with 3 decimal places When validating Then Items[0].UnitPrice fails")]
    public void Given_ItemWithUnitPriceWith3DecimalPlaces_When_Validating_Then_UnitPriceFails()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].UnitPrice = 4.455m;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice")
            .WithErrorMessage("'Unit Price' must have at most 2 decimal places.").Only();
    }
}
