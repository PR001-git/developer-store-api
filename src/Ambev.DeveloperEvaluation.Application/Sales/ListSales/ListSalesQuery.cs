using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Lists sales a page at a time, in the requested order (spec §7.3). An omitted parameter takes its default.
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
}
