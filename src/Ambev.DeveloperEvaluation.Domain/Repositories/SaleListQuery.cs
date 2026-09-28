namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Which sales <see cref="ISaleRepository.ListAsync"/> reads: those matching <see cref="Filter"/>, one page, in order.
/// </summary>
/// <param name="Page">The page number, from 1.</param>
/// <param name="Size">The number of sales per page, from 1.</param>
/// <param name="Sorts">The order, applied field by field. The id always breaks the remaining ties.</param>
public sealed record SaleListQuery(int Page, int Size, IReadOnlyList<SaleSort> Sorts)
{
    /// <summary>
    /// Gets which sales count and can appear on the page. <see cref="SaleListFilter.None"/>, the default, keeps them all.
    /// </summary>
    public SaleListFilter Filter { get; init; } = SaleListFilter.None;
}
