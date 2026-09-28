namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

/// <summary>
/// The body of <c>POST /api/sales</c> (spec §7.2). The Application validator checks it, not this contract (spec decision D10).
/// </summary>
public sealed class CreateSaleRequest
{
    /// <summary>
    /// Gets or sets the sale number, or <c>null</c> for the server to generate one (rule R12). A sent number, trimmed,
    /// must have 1 to 50 characters. There is no initializer, so an omitted number arrives as <c>null</c>, not as <c>""</c>.
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
    /// Gets or sets the customer's name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the branch.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the branch's name.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lines of the sale: at least one, and one per product.
    /// </summary>
    public List<SaleItemRequest> Items { get; set; } = [];
}
