using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

/// <summary>
/// Validates <see cref="CancelSaleItemCommand"/> before its handler runs.
/// </summary>
public sealed class CancelSaleItemCommandValidator : AbstractValidator<CancelSaleItemCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleItemCommandValidator"/> class: both ids are required.
    /// </summary>
    public CancelSaleItemCommandValidator()
    {
        RuleFor(command => command.SaleId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
