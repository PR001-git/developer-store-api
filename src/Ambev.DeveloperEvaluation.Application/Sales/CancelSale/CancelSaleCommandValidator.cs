using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

/// <summary>
/// Validates <see cref="CancelSaleCommand"/> before its handler runs.
/// </summary>
public sealed class CancelSaleCommandValidator : AbstractValidator<CancelSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleCommandValidator"/> class: the id is required.
    /// </summary>
    public CancelSaleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
