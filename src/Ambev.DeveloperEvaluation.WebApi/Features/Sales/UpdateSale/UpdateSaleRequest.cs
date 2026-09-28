namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

/// <summary>
/// The body of <c>PUT /api/sales/{id}</c> (spec §7.2): the create body without <c>saleNumber</c>. A
/// <c>saleNumber</c> sent anyway is ignored, like any unknown property, so the number never changes. The Application
/// validator checks the body, not this contract (spec decision D10).
/// </summary>
public sealed class UpdateSaleRequest
{
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
    /// Gets or sets the lines the sale must have: at least one, and one per product. An active line whose product
    /// isn't here is cancelled.
    /// </summary>
    public List<SaleItemRequest> Items { get; set; } = [];
}
