using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Validates <see cref="CreateSaleCommand"/> before its handler runs. A failure throws <see cref="ValidationException"/>,
/// which the API returns as 400 <c>ValidationError</c>.
/// </summary>
public sealed class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleCommandValidator"/> class with the create rules.
    /// </summary>
    public CreateSaleCommandValidator()
    {
        RuleFor(command => command.SaleNumber).NotEmpty().MaximumTrimmedLength(Sale.SaleNumberMaxLength);
        RuleFor(command => command.SaleDate).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.CustomerName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);
        RuleFor(command => command.BranchId).NotEmpty();
        RuleFor(command => command.BranchName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);
        RuleFor(command => command.Items).ValidSaleItems();
    }
}
