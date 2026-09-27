namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// A sale in a response body (spec §7.2), with flat fields. It has no <c>isDeleted</c> or <c>deletedAt</c>:
/// deleted sales are never returned.
/// </summary>
public sealed class SaleResponse
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
    public IReadOnlyList<SaleItemResponse> Items { get; set; } = [];
}
