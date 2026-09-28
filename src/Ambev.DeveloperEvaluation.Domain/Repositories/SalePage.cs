using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// One page of sales, and how many sales there are across every page.
/// </summary>
/// <param name="Sales">The sales on the page, each with its items. Empty past the last page.</param>
/// <param name="TotalCount">How many sales there are in all.</param>
public sealed record SalePage(IReadOnlyList<Sale> Sales, int TotalCount);
