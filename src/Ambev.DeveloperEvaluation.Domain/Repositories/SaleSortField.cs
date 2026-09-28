namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// A field a sales list can be ordered by (spec §7.3). The id isn't one: it is always the last tie-breaker.
/// </summary>
public enum SaleSortField
{
    /// <summary>The sale number.</summary>
    SaleNumber,

    /// <summary>The date of the sale.</summary>
    SaleDate,

    /// <summary>The customer's name.</summary>
    CustomerName,

    /// <summary>The branch's name.</summary>
    BranchName,

    /// <summary>The sale total.</summary>
    TotalAmount,

    /// <summary>Whether the sale is cancelled. Ascending puts open sales first.</summary>
    IsCancelled
}
