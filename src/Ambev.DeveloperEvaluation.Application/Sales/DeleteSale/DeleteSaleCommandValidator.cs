using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

/// <summary>
/// Validates <see cref="DeleteSaleCommand"/> before its handler runs.
/// </summary>
public sealed class DeleteSaleCommandValidator : AbstractValidator<DeleteSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSaleCommandValidator"/> class: the id is required.
    /// </summary>
    public DeleteSaleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
