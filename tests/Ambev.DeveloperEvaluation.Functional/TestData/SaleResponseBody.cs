namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// A sale in the <c>data</c> of a Sales API success response (spec §7.2), read back for assertions.
/// </summary>
/// <param name="Id">The id of the sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="SaleDate">The date and time of the sale, in UTC.</param>
/// <param name="CustomerId">The id of the customer.</param>
/// <param name="CustomerName">The customer's name.</param>
/// <param name="BranchId">The id of the branch.</param>
/// <param name="BranchName">The branch's name.</param>
/// <param name="TotalAmount">The sale total.</param>
/// <param name="IsCancelled">Whether the sale was cancelled.</param>
/// <param name="CreatedAt">When the sale was created, in UTC.</param>
/// <param name="UpdatedAt">When the sale was last changed, in UTC, if ever.</param>
/// <param name="Items">The lines of the sale.</param>
public sealed record SaleResponseBody(
    Guid Id,
    string SaleNumber,
    DateTime SaleDate,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    decimal TotalAmount,
    bool IsCancelled,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<SaleItemResponseBody> Items);

/// <summary>
/// One line of a <see cref="SaleResponseBody"/>, with the discount and totals the domain computed.
/// </summary>
/// <param name="Id">The id of the line.</param>
/// <param name="ProductId">The id of the product.</param>
/// <param name="ProductName">The product's name.</param>
/// <param name="Quantity">The quantity of identical items.</param>
/// <param name="UnitPrice">The price of one item.</param>
/// <param name="DiscountPercentage">The discount percentage.</param>
/// <param name="DiscountAmount">The discount amount.</param>
/// <param name="TotalAmount">The line total, after the discount.</param>
/// <param name="IsCancelled">Whether the line was cancelled.</param>
public sealed record SaleItemResponseBody(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TotalAmount,
    bool IsCancelled);
