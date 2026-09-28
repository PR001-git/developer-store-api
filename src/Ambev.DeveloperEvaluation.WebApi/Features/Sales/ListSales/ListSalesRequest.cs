using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

/// <summary>
/// The query string of <c>GET /api/sales</c> (<c>.doc/general-api.md</c>). Every parameter is optional: an omitted one
/// is <c>null</c> and takes its default in the Application layer.
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
}
