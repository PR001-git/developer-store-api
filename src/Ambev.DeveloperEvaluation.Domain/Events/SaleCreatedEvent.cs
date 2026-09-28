namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// A sale was created.
/// </summary>
/// <param name="SaleId">The id of the new sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="CustomerId">The id of the customer who bought.</param>
/// <param name="BranchId">The id of the branch where the sale was made.</param>
/// <param name="TotalAmount">The sale total.</param>
/// <param name="ItemCount">The number of lines.</param>
/// <param name="OccurredAt">When the sale was created, in UTC.</param>
public sealed record SaleCreatedEvent(
    Guid SaleId,
    string SaleNumber,
    Guid CustomerId,
    Guid BranchId,
    decimal TotalAmount,
    int ItemCount,
    DateTime OccurredAt) : IDomainEvent;
