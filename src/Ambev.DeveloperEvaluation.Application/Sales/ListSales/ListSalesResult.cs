namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// One page of sales, with the figures the paged response needs.
/// </summary>
/// <param name="Sales">The sales on the page, each with its items. Empty past the last page.</param>
/// <param name="Page">The page number that was read.</param>
/// <param name="Size">The page size that was used.</param>
/// <param name="TotalCount">How many sales match the filters, across every page.</param>
public sealed record ListSalesResult(IReadOnlyList<SaleResult> Sales, int Page, int Size, int TotalCount);
