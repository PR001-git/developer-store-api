using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Creates a sale. The discounts and totals aren't sent: the domain computes them (rule R2).
/// </summary>
/// <remarks>
/// <see cref="CreateSaleCommandValidator"/> checks it before the handler runs, through the MediatR <c>ValidationBehavior</c>.
/// </remarks>
public sealed class CreateSaleCommand : IRequest<SaleResult>
{
    /// <summary>
    /// Gets or sets the sale number, or <c>null</c> for the handler to generate one (rule R12).
    /// A sent number, trimmed, must have 1 to 50 characters and belong to no other sale.
    /// </summary>
    public string? SaleNumber { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the sale. A value without an offset is read as UTC.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets or sets the id of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer's name, copied into the sale.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the branch.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the branch's name, copied into the sale.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lines of the sale: at least one, and one per product.
    /// </summary>
    public IReadOnlyList<SaleItemInput> Items { get; set; } = [];
}
