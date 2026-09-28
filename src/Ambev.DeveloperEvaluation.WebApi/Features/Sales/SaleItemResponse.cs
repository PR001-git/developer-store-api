namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// A line of a <see cref="SaleResponse"/>, with the discount and totals the domain computed.
/// </summary>
public sealed class SaleItemResponse
{
    /// <summary>
    /// Gets or sets the id of the line.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the product.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product's name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of identical items.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price of one item.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the discount percentage: 0, 10 or 20.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the line total, after the discount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the line was cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
