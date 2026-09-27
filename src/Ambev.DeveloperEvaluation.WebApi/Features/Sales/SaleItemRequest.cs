namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// One line of a sale in a request body. There are no discount fields: the domain computes them (rule R2).
/// </summary>
public sealed class SaleItemRequest
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
}
