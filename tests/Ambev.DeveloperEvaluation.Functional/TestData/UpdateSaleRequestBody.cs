namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// The JSON body of <c>PUT /api/sales/{id}</c> (spec §7.2), as a client writes it: the create body without
/// <c>saleNumber</c>.
/// </summary>
/// <param name="SaleDate">The date and time of the sale.</param>
/// <param name="CustomerId">The id of the customer.</param>
/// <param name="CustomerName">The customer's name.</param>
/// <param name="BranchId">The id of the branch.</param>
/// <param name="BranchName">The branch's name.</param>
/// <param name="Items">The lines the sale must have.</param>
public sealed record UpdateSaleRequestBody(
    DateTime SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    IReadOnlyList<SaleItemRequestBody> Items);
