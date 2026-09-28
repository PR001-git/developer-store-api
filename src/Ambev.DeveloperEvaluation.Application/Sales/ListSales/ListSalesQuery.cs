using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Lists the sales that match the filters, a page at a time, in the requested order (spec §7.3). An omitted paging
/// or ordering parameter takes its default; an omitted filter doesn't filter.
/// </summary>
public sealed class ListSalesQuery : IRequest<ListSalesResult>
{
    /// <summary>
    /// The page read when <see cref="Page"/> is omitted.
    /// </summary>
    public const int DefaultPage = 1;

    /// <summary>
    /// The page size used when <see cref="Size"/> is omitted.
    /// </summary>
    public const int DefaultSize = 10;

    /// <summary>
    /// The largest page size a client may ask for.
    /// </summary>
    public const int MaxSize = 100;

    /// <summary>
    /// Gets or sets the page number (<c>_page</c>), at least 1, or <c>null</c> for <see cref="DefaultPage"/>.
    /// </summary>
    public int? Page { get; set; }

    /// <summary>
    /// Gets or sets the page size (<c>_size</c>), from 1 to <see cref="MaxSize"/>, or <c>null</c> for <see cref="DefaultSize"/>.
    /// </summary>
    public int? Size { get; set; }

    /// <summary>
    /// Gets or sets the order (<c>_order</c>), such as <c>"saleDate desc, totalAmount"</c>, or <c>null</c> for <c>saleDate desc</c>.
    /// </summary>
    public string? Order { get; set; }

    /// <summary>
    /// Gets or sets the sale-number text (<c>saleNumber</c>), matched case-insensitively: <c>value*</c> starts with,
    /// <c>*value</c> ends with, <c>*value*</c> contains, no <c>*</c> equals.
    /// </summary>
    public string? SaleNumber { get; set; }

    /// <summary>
    /// Gets or sets the customer-name text (<c>customerName</c>), matched like <see cref="SaleNumber"/>.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the branch-name text (<c>branchName</c>), matched like <see cref="SaleNumber"/>.
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Gets or sets the customer id (<c>customerId</c>), matched exactly.
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the branch id (<c>branchId</c>), matched exactly.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Gets or sets whether the sales are cancelled (<c>isCancelled</c>).
    /// </summary>
    public bool? IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets the earliest sale date (<c>_minSaleDate</c>), inclusive. A value without an offset is UTC.
    /// </summary>
    public DateTime? MinSaleDate { get; set; }

    /// <summary>
    /// Gets or sets the latest sale date (<c>_maxSaleDate</c>), inclusive. A value without an offset is UTC, and
    /// exactly midnight covers that whole day.
    /// </summary>
    public DateTime? MaxSaleDate { get; set; }

    /// <summary>
    /// Gets or sets the lowest sale total (<c>_minTotalAmount</c>), inclusive.
    /// </summary>
    public decimal? MinTotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the highest sale total (<c>_maxTotalAmount</c>), inclusive.
    /// </summary>
    public decimal? MaxTotalAmount { get; set; }
}
