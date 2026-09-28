using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.DeleteSale;

/// <summary>
/// Contains unit tests for <see cref="DeleteSaleCommandValidator"/>.
/// </summary>
public sealed class DeleteSaleCommandValidatorTests
{
    private readonly DeleteSaleCommandValidator _validator = new();

    /// <summary>
    /// Tests that an id is required.
    /// </summary>
    [Fact(DisplayName = "Given an empty id When validating Then Id fails as empty")]
    public void Given_EmptyId_When_Validating_Then_IdFails()
    {
        // When
        var result = _validator.TestValidate(new DeleteSaleCommand(Guid.Empty));

        // Then
        result.ShouldHaveValidationErrorFor(command => command.Id).WithErrorMessage("'Id' must not be empty.").Only();
    }

    /// <summary>
    /// Tests that any other id passes.
    /// </summary>
    [Fact(DisplayName = "Given an id When validating Then there are no failures")]
    public void Given_Id_When_Validating_Then_NoFailures()
    {
        // When
        var result = _validator.TestValidate(new DeleteSaleCommand(Guid.NewGuid()));

        // Then
        result.ShouldNotHaveAnyValidationErrors();
    }
}
