using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Replaces a sale's date, customer and branch, and reconciles its lines by product (rule R10). It has no sale
/// number, because the number never changes.
/// </summary>
/// <remarks>
/// <see cref="UpdateSaleCommandValidator"/> checks it before the handler runs, through the MediatR <c>ValidationBehavior</c>.
/// </remarks>
public sealed class UpdateSaleCommand : IRequest<SaleResult>
{
    /// <summary>
    /// Gets or sets the id of the sale. The API takes it from the route.
    /// </summary>
    public Guid Id { get; set; }

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
    /// Gets or sets the lines the sale must have: at least one, and one per product. An active line whose product
    /// isn't here is cancelled.
    /// </summary>
    public IReadOnlyList<SaleItemInput> Items { get; set; } = [];
}
