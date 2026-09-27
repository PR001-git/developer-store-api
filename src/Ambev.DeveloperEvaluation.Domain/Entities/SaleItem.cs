using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// One line of a <see cref="Sale"/>: a product, how many identical items were sold, their price,
/// and the discount and total that the domain computed. It changes only through its sale.
/// </summary>
public sealed class SaleItem : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaleItem"/> class for EF Core, which sets the properties itself.
    /// </summary>
    private SaleItem()
    {
    }

    /// <summary>
    /// Initializes a new line with a new id, and computes its discount (rule R1) and amounts (rule R3).
    /// </summary>
    /// <param name="product">The product sold.</param>
    /// <param name="quantity">The quantity of identical items, from 1 to 20.</param>
    /// <param name="unitPrice">The price of one item.</param>
    /// <exception cref="DomainException">
    /// Thrown when the quantity is outside 1 to 20, or the unit price isn't above 0 with at most 2 decimal places.
    /// </exception>
    internal SaleItem(ExternalIdentity product, int quantity, decimal unitPrice)
    {
        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero");

        if (decimal.Round(unitPrice, 2) != unitPrice)
            throw new DomainException("Unit price must have at most 2 decimal places");

        Id = Guid.NewGuid();
        Product = product;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercentage = DiscountPolicy.GetDiscountPercentage(quantity);

        var grossAmount = quantity * unitPrice;
        DiscountAmount = Math.Round(grossAmount * DiscountPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        TotalAmount = grossAmount - DiscountAmount;
    }

    /// <summary>
    /// Gets the product sold.
    /// </summary>
    public ExternalIdentity Product { get; private set; } = null!;

    /// <summary>
    /// Gets the quantity of identical items, from 1 to 20.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Gets the price of one item.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Gets the discount percentage for the quantity: 0, 10 or 20.
    /// </summary>
    public decimal DiscountPercentage { get; private set; }

    /// <summary>
    /// Gets the discount: quantity × unit price × percentage ÷ 100, rounded to 2 decimal places away from zero.
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// Gets the line total: quantity × unit price, minus the discount.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the line was cancelled. A cancelled line doesn't count in the sale total.
    /// </summary>
    public bool IsCancelled { get; private set; }
}
