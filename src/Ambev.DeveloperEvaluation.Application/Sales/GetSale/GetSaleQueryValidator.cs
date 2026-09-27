using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

/// <summary>
/// Validates <see cref="GetSaleQuery"/> before its handler runs.
/// </summary>
public sealed class GetSaleQueryValidator : AbstractValidator<GetSaleQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSaleQueryValidator"/> class: the id is required.
    /// </summary>
    public GetSaleQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
