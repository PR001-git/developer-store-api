namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// The input for one line of a sale. The sale computes the discount and the totals from it (rule R2).
/// </summary>
/// <param name="Product">The product sold.</param>
/// <param name="Quantity">The quantity of identical items, from 1 to 20.</param>
/// <param name="UnitPrice">The price of one item: above 0, with at most 2 decimal places.</param>
public sealed record SaleItemData(ExternalIdentity Product, int Quantity, decimal UnitPrice);
