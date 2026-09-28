using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

/// <summary>
/// The query string of <c>GET /api/sales</c> (<c>.doc/general-api.md</c>, spec §7.3). Every parameter is optional:
/// an omitted one is <c>null</c>, so paging and ordering take their defaults in the Application layer and a filter
/// doesn't filter. A malformed value fails model binding (400).
/// </summary>
public sealed class ListSalesRequest
{
    /// <summary>
    /// Gets or sets the page number, <c>_page</c>: at least 1, default 1.
    /// </summary>
    [FromQuery(Name = "_page")]
    public int? Page { get; set; }

    /// <summary>
    /// Gets or sets the page size, <c>_size</c>: 1 to 100, default 10.
    /// </summary>
    [FromQuery(Name = "_size")]
    public int? Size { get; set; }

    /// <summary>
    /// Gets or sets the order, <c>_order</c>, such as <c>"saleDate desc, totalAmount"</c>: default <c>saleDate desc</c>.
    /// </summary>
    [FromQuery(Name = "_order")]
    public string? Order { get; set; }

    /// <summary>
    /// Gets or sets the sale-number filter, <c>saleNumber</c>: case-insensitive; <c>value*</c> starts with,
    /// <c>*value</c> ends with, <c>*value*</c> contains, no <c>*</c> equals.
    /// </summary>
    [FromQuery(Name = "saleNumber")]
    public string? SaleNumber { get; set; }

    /// <summary>
    /// Gets or sets the customer-name filter, <c>customerName</c>, matched like <see cref="SaleNumber"/>.
    /// </summary>
    [FromQuery(Name = "customerName")]
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the branch-name filter, <c>branchName</c>, matched like <see cref="SaleNumber"/>.
    /// </summary>
    [FromQuery(Name = "branchName")]
    public string? BranchName { get; set; }

    /// <summary>
    /// Gets or sets the customer id, <c>customerId</c>, matched exactly.
    /// </summary>
    [FromQuery(Name = "customerId")]
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the branch id, <c>branchId</c>, matched exactly.
    /// </summary>
    [FromQuery(Name = "branchId")]
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Gets or sets whether the sales are cancelled, <c>isCancelled</c>.
    /// </summary>
    [FromQuery(Name = "isCancelled")]
    public bool? IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets the earliest sale date, <c>_minSaleDate</c>, inclusive. A value without an offset is UTC.
    /// </summary>
    [FromQuery(Name = "_minSaleDate")]
    public DateTime? MinSaleDate { get; set; }

    /// <summary>
    /// Gets or sets the latest sale date, <c>_maxSaleDate</c>, inclusive. A value without an offset is UTC, and exactly
    /// midnight (such as <c>2026-01-31</c>) covers that whole day.
    /// </summary>
    [FromQuery(Name = "_maxSaleDate")]
    public DateTime? MaxSaleDate { get; set; }

    /// <summary>
    /// Gets or sets the lowest sale total, <c>_minTotalAmount</c>, inclusive.
    /// </summary>
    [FromQuery(Name = "_minTotalAmount")]
    public decimal? MinTotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the highest sale total, <c>_maxTotalAmount</c>, inclusive.
    /// </summary>
    [FromQuery(Name = "_maxTotalAmount")]
    public decimal? MaxTotalAmount { get; set; }
}
