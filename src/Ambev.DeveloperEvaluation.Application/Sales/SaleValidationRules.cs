using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Validation rules that the Sales commands share, so create and update check the same things the same way.
/// </summary>
public static class SaleValidationRules
{
    /// <summary>
    /// Checks the length of a text after trimming, as the domain stores it. A <c>null</c> passes: pair it with <c>NotEmpty</c>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="ruleBuilder">The rule for the text property.</param>
    /// <param name="maximumLength">The largest length allowed after trimming.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptions<T, string> MaximumTrimmedLength<T>(this IRuleBuilder<T, string> ruleBuilder, int maximumLength) =>
        ruleBuilder
            .Must(value => value is null || value.Trim().Length <= maximumLength)
            .WithMessage($"'{{PropertyName}}' must have at most {maximumLength} characters after trimming.");

    /// <summary>
    /// Checks the lines of a sale: at least one (rule R5), one per product (rule R4), and each line present and valid.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="ruleBuilder">The rule for the lines property.</param>
    /// <returns>The rule builder, for chaining.</returns>
    public static IRuleBuilderOptions<T, IEnumerable<SaleItemInput>> ValidSaleItems<T>(
        this IRuleBuilder<T, IReadOnlyList<SaleItemInput>> ruleBuilder) =>
        ruleBuilder
            .NotEmpty().WithMessage(Sale.NoItemsMessage)
            .Must(items => items is null || HasOneLinePerProduct(items)).WithMessage(Sale.RepeatedProductMessage)
            .ForEach(item => item.NotNull().SetValidator(new SaleItemInputValidator()));

    /// <summary>
    /// Tells whether no product appears in two lines. Missing lines are skipped here; the per-line rule reports them.
    /// </summary>
    private static bool HasOneLinePerProduct(IReadOnlyList<SaleItemInput> items)
    {
        var productIds = items.Where(item => item is not null).Select(item => item.ProductId).ToList();
        return productIds.Distinct().Count() == productIds.Count;
    }
}
