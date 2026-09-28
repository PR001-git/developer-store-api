using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CancelSaleItem;

/// <summary>
/// Contains unit tests for <see cref="CancelSaleItemCommandValidator"/>.
/// </summary>
public sealed class CancelSaleItemCommandValidatorTests
{
    private readonly CancelSaleItemCommandValidator _validator = new();

    /// <summary>
    /// Tests that the sale id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty sale id When validating Then SaleId fails as empty")]
    public void Given_EmptySaleId_When_Validating_Then_SaleIdFails()
    {
        // When
        var result = _validator.TestValidate(new CancelSaleItemCommand(Guid.Empty, Guid.NewGuid()));

        // Then
        result.ShouldHaveValidationErrorFor(command => command.SaleId).WithErrorMessage("'Sale Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that the item id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty item id When validating Then ItemId fails as empty")]
    public void Given_EmptyItemId_When_Validating_Then_ItemIdFails()
    {
        // When
        var result = _validator.TestValidate(new CancelSaleItemCommand(Guid.NewGuid(), Guid.Empty));

        // Then
        result.ShouldHaveValidationErrorFor(command => command.ItemId).WithErrorMessage("'Item Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that any other pair of ids passes.
    /// </summary>
    [Fact(DisplayName = "Given a sale id and an item id When validating Then there are no failures")]
    public void Given_SaleIdAndItemId_When_Validating_Then_NoFailures()
    {
        // When
        var result = _validator.TestValidate(new CancelSaleItemCommand(Guid.NewGuid(), Guid.NewGuid()));

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }
}
