namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// One step of a sales list's order: a field and a direction.
/// </summary>
/// <param name="Field">The field to order by.</param>
/// <param name="Descending"><c>true</c> for descending, <c>false</c> for ascending.</param>
public readonly record struct SaleSort(SaleSortField Field, bool Descending);
