using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.UpdateSale;

/// <summary>
/// Contains unit tests for <see cref="UpdateSaleCommandValidator"/>: the id, each header rule, and the shared line
/// rules, which <c>CreateSaleCommandValidatorTests</c> covers one by one. Each invalid case checks that its failure
/// is the only one.
/// </summary>
public sealed class UpdateSaleCommandValidatorTests
{
    private readonly UpdateSaleCommandValidator _validator = new();

    /// <summary>
    /// Tests that a command with valid data has no failures.
    /// </summary>
    [Fact(DisplayName = "Given a valid command When validating Then there are no failures")]
    public void Given_ValidCommand_When_Validating_Then_NoFailures()
    {
        // When
        var result = _validator.TestValidate(UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid()));

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that the sale id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty id When validating Then Id fails as empty")]
    public void Given_EmptyId_When_Validating_Then_IdFails()
    {
        // When
        var result = _validator.TestValidate(UpdateSaleCommandTestData.GenerateValidCommand(Guid.Empty));

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Id).WithErrorMessage("'Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the sale date is required. A missing <c>saleDate</c> in the JSON arrives as <see cref="DateTime.MinValue"/>.
    /// </summary>
    [Fact(DisplayName = "Given no sale date When validating Then SaleDate fails as empty")]
    public void Given_NoSaleDate_When_Validating_Then_SaleDateFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
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
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.CustomerId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.CustomerId).WithErrorMessage("'Customer Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the customer name has at most 100 characters after trimming.
    /// </summary>
    [Fact(DisplayName = "Given a customer name of 101 characters When validating Then CustomerName fails as too long")]
    public void Given_CustomerNameOf101Characters_When_Validating_Then_CustomerNameFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.CustomerName = new string('c', 101);

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.CustomerName)
            .WithErrorMessage("'Customer Name' must have at most 100 characters after trimming.").Only();
    }

    /// <summary>
    /// Tests that the branch id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty branch id When validating Then BranchId fails as empty")]
    public void Given_EmptyBranchId_When_Validating_Then_BranchIdFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.BranchId = Guid.Empty;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.BranchId).WithErrorMessage("'Branch Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the branch name is required.
    /// </summary>
    [Fact(DisplayName = "Given a blank branch name When validating Then BranchName fails as empty")]
    public void Given_BlankBranchName_When_Validating_Then_BranchNameFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.BranchName = "   ";

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.BranchName).WithErrorMessage("'Branch Name' must not be empty.").Only();
    }

    /// <summary>
    /// Tests rule R5 through the shared line rules: an update can't empty the sale.
    /// </summary>
    [Fact(DisplayName = "Given no items When validating Then Items fails with the no-items message")]
    public void Given_NoItems_When_Validating_Then_ItemsFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.Items = [];

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Items).WithErrorMessage("A sale must have at least one item").Only();
    }

    /// <summary>
    /// Tests rule R4 through the shared line rules: a product can be sent only once.
    /// </summary>
    [Fact(DisplayName = "Given two items for the same product When validating Then Items fails with the repeated-product message")]
    public void Given_TwoItemsForSameProduct_When_Validating_Then_ItemsFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.Items[1].ProductId = command.Items[0].ProductId;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor(c => c.Items).WithErrorMessage("Each product can appear only once in a sale").Only();
    }

    /// <summary>
    /// Tests rule R1's limit through the shared per-line rules, with R1's own message.
    /// </summary>
    [Fact(DisplayName = "Given an item with 21 identical items When validating Then Items[0].Quantity fails with R1's message")]
    public void Given_ItemWith21Items_When_Validating_Then_QuantityFails()
    {
        // Given
        var command = UpdateSaleCommandTestData.GenerateValidCommand(Guid.NewGuid());
        command.Items[0].Quantity = 21;

        // When
        var result = _validator.TestValidate(command);

        // Then
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("It's not possible to sell above 20 identical items").Only();
    }
}
