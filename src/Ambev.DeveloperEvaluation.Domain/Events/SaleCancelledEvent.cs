namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// A sale was cancelled. Its items and total stay as they were (rule R8).
/// </summary>
/// <param name="SaleId">The id of the cancelled sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="OccurredAt">When the sale was cancelled, in UTC.</param>
public sealed record SaleCancelledEvent(Guid SaleId, string SaleNumber, DateTime OccurredAt) : IDomainEvent;
