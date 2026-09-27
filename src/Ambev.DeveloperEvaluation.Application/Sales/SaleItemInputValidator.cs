using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Validates one line of a sale command: a product, a quantity from 1 to 20 (rule R1) and a unit price
/// above 0 with at most 2 decimal places.
/// </summary>
public sealed class SaleItemInputValidator : AbstractValidator<SaleItemInput>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaleItemInputValidator"/> class with the line rules.
    /// </summary>
    public SaleItemInputValidator()
    {
        RuleFor(item => item.ProductId).NotEmpty();
        RuleFor(item => item.ProductName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);

        RuleFor(item => item.Quantity)
            .GreaterThanOrEqualTo(DiscountPolicy.MinQuantity)
            .LessThanOrEqualTo(DiscountPolicy.MaxQuantity).WithMessage(DiscountPolicy.MaxQuantityExceededMessage);

        RuleFor(item => item.UnitPrice)
            .GreaterThan(0)
            .Must(price => decimal.Round(price, 2) == price).WithMessage("'{PropertyName}' must have at most 2 decimal places.");
    }
}
