using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// A sale: the aggregate root that owns its lines and enforces the sale rules.
/// </summary>
/// <remarks>
/// Customer, branch and products belong to other domains, so the sale refers to them as
/// <see cref="ExternalIdentity"/> values (spec decision D2). Discounts and totals are always computed here (rule R2).
/// </remarks>
public sealed class Sale : BaseEntity
{
    /// <summary>
    /// The maximum length of <see cref="SaleNumber"/>, after trimming.
    /// </summary>
    public const int SaleNumberMaxLength = 50;

    private readonly List<SaleItem> _items = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Sale"/> class for EF Core, which sets the properties itself.
    /// </summary>
    private Sale()
    {
    }

    /// <summary>
    /// Gets the sale number, unique across all sales. It never changes.
    /// </summary>
    public string SaleNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the date and time of the sale, in UTC.
    /// </summary>
    public DateTime SaleDate { get; private set; }

    /// <summary>
    /// Gets the customer who bought.
    /// </summary>
    public ExternalIdentity Customer { get; private set; } = null!;

    /// <summary>
    /// Gets the branch where the sale was made.
    /// </summary>
    public ExternalIdentity Branch { get; private set; } = null!;

    /// <summary>
    /// Gets the lines of the sale, cancelled ones included. Read-only: lines change only through the sale.
    /// </summary>
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Gets the sale total: the sum of the totals of the lines that aren't cancelled.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the sale was cancelled.
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the sale was soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets when the sale was soft-deleted, in UTC, or <c>null</c> if it wasn't.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Gets when the sale was created, in UTC.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets when the sale was last changed, in UTC, or <c>null</c> if it never was.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a sale with a new id. Each line gets its discount (rule R1) and amounts (rule R3),
    /// and the sale total is the sum of the line totals.
    /// </summary>
    /// <param name="saleNumber">The sale number.</param>
    /// <param name="saleDate">The date and time of the sale. A value without a kind is read as UTC; a local one is converted (rule R13).</param>
    /// <param name="customer">The customer who bought.</param>
    /// <param name="branch">The branch where the sale was made.</param>
    /// <param name="items">The lines of the sale.</param>
    /// <returns>The new sale.</returns>
    public static Sale Create(
        string saleNumber,
        DateTime saleDate,
        ExternalIdentity customer,
        ExternalIdentity branch,
        IReadOnlyCollection<SaleItemData> items)
    {
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = saleNumber,
            SaleDate = ToUtc(saleDate),
            Customer = customer,
            Branch = branch,
            CreatedAt = DateTime.UtcNow
        };

        sale._items.AddRange(items.Select(item => new SaleItem(item.Product, item.Quantity, item.UnitPrice)));
        sale.RecalculateTotal();

        return sale;
    }

    private void RecalculateTotal() =>
        TotalAmount = _items.Where(item => !item.IsCancelled).Sum(item => item.TotalAmount);

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
