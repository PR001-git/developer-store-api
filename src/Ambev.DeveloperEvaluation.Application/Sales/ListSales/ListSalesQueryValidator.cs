using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Validates <see cref="ListSalesQuery"/> before its handler runs. Failures name the query parameters the client
/// sent (<c>_page</c>, <c>_size</c>, <c>_order</c>), as a model-binding failure on them does. Omitted values pass.
/// </summary>
public sealed class ListSalesQueryValidator : AbstractValidator<ListSalesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesQueryValidator"/> class with the paging and ordering rules.
    /// </summary>
    public ListSalesQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .OverridePropertyName("_page")
            .WithMessage("'_page' must be at least 1.");

        RuleFor(query => query.Size)
            .InclusiveBetween(1, ListSalesQuery.MaxSize)
            .OverridePropertyName("_size")
            .WithMessage($"'_size' must be between 1 and {ListSalesQuery.MaxSize}.");

        RuleFor(query => query.Order).Custom((order, context) =>
        {
            if (!SaleOrderParser.TryParse(order, out _, out var error))
                context.AddFailure("_order", error);
        });
    }
}
