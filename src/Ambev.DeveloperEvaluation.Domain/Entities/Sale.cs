using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
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

    /// <summary>
    /// Builds the message for a sale number that another sale already has (rule R12).
    /// </summary>
    /// <param name="saleNumber">The sale number that is taken.</param>
    /// <returns>The message, such as "Sale number S-000123 already exists".</returns>
    public static string DuplicateSaleNumberMessage(string saleNumber) => $"Sale number {saleNumber} already exists";

    /// <summary>
    /// The message for a sale without lines (rule R5).
    /// </summary>
    public const string NoItemsMessage = "A sale must have at least one item";

    /// <summary>
    /// The message for a product that appears in more than one line (rule R4).
    /// </summary>
    public const string RepeatedProductMessage = "Each product can appear only once in a sale";

    private readonly List<SaleItem> _items = [];
    private readonly List<IDomainEvent> _domainEvents = [];

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
    /// Gets the events recorded since the sale was created or loaded, in order. They aren't stored:
    /// the application publishes them after saving the sale, then clears them.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Creates a sale with a new id. Each line gets its discount (rule R1) and amounts (rule R3),
    /// the sale total is the sum of the line totals, and a <see cref="SaleCreatedEvent"/> is recorded.
    /// </summary>
    /// <param name="saleNumber">The sale number. Trimmed, it must have 1 to 50 characters.</param>
    /// <param name="saleDate">The date and time of the sale. A value without a kind is read as UTC; a local one is converted (rule R13).</param>
    /// <param name="customer">The customer who bought.</param>
    /// <param name="branch">The branch where the sale was made.</param>
    /// <param name="items">The lines of the sale: at least one, and one per product.</param>
    /// <returns>The new sale.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the sale number is missing or too long, there are no lines, a product repeats,
    /// or a line has a quantity outside 1 to 20 or an invalid unit price.
    /// </exception>
    public static Sale Create(
        string saleNumber,
        DateTime saleDate,
        ExternalIdentity customer,
        ExternalIdentity branch,
        IReadOnlyCollection<SaleItemData> items)
    {
        var trimmedSaleNumber = saleNumber?.Trim() ?? string.Empty;
        if (trimmedSaleNumber.Length is 0 or > SaleNumberMaxLength)
            throw new DomainException($"Sale number must have 1 to {SaleNumberMaxLength} characters");

        EnsureValidLines(items);

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = trimmedSaleNumber,
            SaleDate = ToUtc(saleDate),
            Customer = customer,
            Branch = branch,
            CreatedAt = DateTime.UtcNow
        };

        sale._items.AddRange(items.Select(item => new SaleItem(item.Product, item.Quantity, item.UnitPrice)));
        sale.RecalculateTotal();

        sale._domainEvents.Add(new SaleCreatedEvent(
            sale.Id, sale.SaleNumber, customer.Id, branch.Id, sale.TotalAmount, sale._items.Count, sale.CreatedAt));

        return sale;
    }

    /// <summary>
    /// Forgets the recorded events, once they have been published.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    private static void EnsureValidLines(IReadOnlyCollection<SaleItemData> items)
    {
        if (items.Count == 0)
            throw new DomainException(NoItemsMessage);

        if (items.DistinctBy(item => item.Product.Id).Count() != items.Count)
            throw new DomainException(RepeatedProductMessage);
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
