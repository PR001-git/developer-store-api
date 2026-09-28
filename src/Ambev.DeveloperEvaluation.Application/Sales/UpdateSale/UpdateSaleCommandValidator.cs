using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Validates <see cref="UpdateSaleCommand"/> before its handler runs. It has the create rules without the sale number,
/// plus the sale id. The line rules come from <see cref="SaleValidationRules"/>, shared with create. A failure throws
/// <see cref="ValidationException"/>, which the API returns as 400 <c>ValidationError</c>.
/// </summary>
public sealed class UpdateSaleCommandValidator : AbstractValidator<UpdateSaleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleCommandValidator"/> class with the update rules.
    /// </summary>
    public UpdateSaleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.SaleDate).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.CustomerName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);
        RuleFor(command => command.BranchId).NotEmpty();
        RuleFor(command => command.BranchName).NotEmpty().MaximumTrimmedLength(ExternalIdentity.NameMaxLength);
        RuleFor(command => command.Items).ValidSaleItems();
    }
}
