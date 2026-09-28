using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Validates <see cref="ListSalesQuery"/> before its handler runs. Failures name the query parameters the client
/// sent (<c>_page</c>, <c>_size</c>, <c>_order</c>, <c>_minSaleDate</c>, <c>_minTotalAmount</c>), as a model-binding
/// failure on them does. Omitted values pass.
/// </summary>
public sealed class ListSalesQueryValidator : AbstractValidator<ListSalesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesQueryValidator"/> class with the paging, ordering and range rules.
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

        // Compared as the bounds the handler uses, so a midnight _maxSaleDate covers its whole day.
        RuleFor(query => query.MinSaleDate)
            .Must((query, min) => min is null || query.MaxSaleDate is null
                || SaleDateBounds.LowerBound(min.Value) <= SaleDateBounds.UpperBound(query.MaxSaleDate.Value))
            .OverridePropertyName("_minSaleDate")
            .WithMessage("'_minSaleDate' must not be above '_maxSaleDate'.");

        RuleFor(query => query.MinTotalAmount)
            .Must((query, min) => min is null || query.MaxTotalAmount is null || min <= query.MaxTotalAmount)
            .OverridePropertyName("_minTotalAmount")
            .WithMessage("'_minTotalAmount' must not be above '_maxTotalAmount'.");
    }
}
