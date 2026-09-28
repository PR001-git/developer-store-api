namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Which sales <see cref="ISaleRepository.ListAsync"/> keeps (spec §7.3). A <c>null</c> criterion doesn't filter,
/// and a sale must match every criterion that is set.
/// </summary>
public sealed record SaleListFilter
{
    /// <summary>
    /// Gets a filter that keeps every sale.
    /// </summary>
    public static SaleListFilter None { get; } = new();

    /// <summary>
    /// Gets the sale-number text, matched case-insensitively: <c>value*</c> starts with, <c>*value</c> ends with,
    /// <c>*value*</c> contains, no <c>*</c> equals. Every other character, an inner <c>*</c> included, matches itself.
    /// </summary>
    public string? SaleNumber { get; init; }

    /// <summary>
    /// Gets the customer-name text, matched like <see cref="SaleNumber"/>.
    /// </summary>
    public string? CustomerName { get; init; }

    /// <summary>
    /// Gets the branch-name text, matched like <see cref="SaleNumber"/>.
    /// </summary>
    public string? BranchName { get; init; }

    /// <summary>
    /// Gets the customer id, matched exactly.
    /// </summary>
    public Guid? CustomerId { get; init; }

    /// <summary>
    /// Gets the branch id, matched exactly.
    /// </summary>
    public Guid? BranchId { get; init; }

    /// <summary>
    /// Gets whether the sales are cancelled.
    /// </summary>
    public bool? IsCancelled { get; init; }

    /// <summary>
    /// Gets the earliest sale date, inclusive, in UTC.
    /// </summary>
    public DateTime? MinSaleDate { get; init; }

    /// <summary>
    /// Gets the latest sale date, inclusive, in UTC.
    /// </summary>
    public DateTime? MaxSaleDate { get; init; }

    /// <summary>
    /// Gets the lowest sale total, inclusive.
    /// </summary>
    public decimal? MinTotalAmount { get; init; }

    /// <summary>
    /// Gets the highest sale total, inclusive.
    /// </summary>
    public decimal? MaxTotalAmount { get; init; }
}
