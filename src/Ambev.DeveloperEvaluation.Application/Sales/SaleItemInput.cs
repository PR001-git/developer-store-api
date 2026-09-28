using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// One line of a sale as a command sends it. There are no discount fields: the domain computes them (rule R2).
/// </summary>
public sealed class SaleItemInput
{
    /// <summary>
    /// Gets or sets the id of the product.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product's name, copied into the sale.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of identical items, from 1 to 20.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price of one item: above 0, with at most 2 decimal places.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Converts the line into the domain's input, which the create and update handlers pass to <c>Sale</c>.
    /// </summary>
    /// <returns>The line's input data.</returns>
    public SaleItemData ToItemData() => new(new ExternalIdentity(ProductId, ProductName), Quantity, UnitPrice);
}
