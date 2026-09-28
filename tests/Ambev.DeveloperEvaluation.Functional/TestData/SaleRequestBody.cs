namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// The JSON body of <c>POST /api/sales</c> (spec §7.2), as a client writes it.
/// </summary>
/// <param name="SaleNumber">The sale number, or <c>null</c> for the server to generate one.</param>
/// <param name="SaleDate">The date and time of the sale.</param>
/// <param name="CustomerId">The id of the customer.</param>
/// <param name="CustomerName">The customer's name.</param>
/// <param name="BranchId">The id of the branch.</param>
/// <param name="BranchName">The branch's name.</param>
/// <param name="Items">The lines of the sale.</param>
public sealed record SaleRequestBody(
    string? SaleNumber,
    DateTime SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    IReadOnlyList<SaleItemRequestBody> Items);

/// <summary>
/// One line of a <see cref="SaleRequestBody"/>. Like the API contract, it has no discount fields.
/// </summary>
/// <param name="ProductId">The id of the product.</param>
/// <param name="ProductName">The product's name.</param>
/// <param name="Quantity">The quantity of identical items.</param>
/// <param name="UnitPrice">The price of one item.</param>
public sealed record SaleItemRequestBody(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
