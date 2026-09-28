namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// The paged body of <c>GET /api/sales</c> (spec §7.3), read back for assertions. <c>SaleJson</c> checks
/// <c>success</c>, <c>message</c> and the field names.
/// </summary>
/// <param name="Data">The sales on the page.</param>
/// <param name="CurrentPage">The page number.</param>
/// <param name="TotalPages">The number of pages.</param>
/// <param name="TotalItems">The number of sales across every page.</param>
public sealed record SalePageResponseBody(
    IReadOnlyList<SaleResponseBody> Data,
    int CurrentPage,
    int TotalPages,
    int TotalItems);
