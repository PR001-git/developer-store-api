namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Which page of sales <see cref="ISaleRepository.ListAsync"/> reads, and in what order.
/// </summary>
/// <param name="Page">The page number, from 1.</param>
/// <param name="Size">The number of sales per page, from 1.</param>
/// <param name="Sorts">The order, applied field by field. The id always breaks the remaining ties.</param>
public sealed record SaleListQuery(int Page, int Size, IReadOnlyList<SaleSort> Sorts);
