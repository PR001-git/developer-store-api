namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// A sale as the Sales use cases return it: flat fields (<c>CustomerName</c>, not <c>Customer.Name</c>),
/// so the API can filter and order by the JSON field names (spec decision D12).
/// </summary>
/// <remarks>
/// It has no soft-delete fields: deleted sales are never returned.
/// </remarks>
public sealed class SaleResult
{
    /// <summary>
    /// Gets or sets the id of the sale.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the sale number.
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time of the sale, in UTC.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets or sets the id of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer's name, as copied when the sale was written.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the branch.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the branch's name, as copied when the sale was written.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sale total: the sum of the totals of the lines that aren't cancelled.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sale was cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets when the sale was created, in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the sale was last changed, in UTC, or <c>null</c> if it never was.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the lines of the sale, cancelled ones included.
    /// </summary>
    public IReadOnlyList<SaleItemResult> Items { get; set; } = [];
}
