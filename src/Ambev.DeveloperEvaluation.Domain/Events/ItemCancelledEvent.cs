namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// A line of a sale was cancelled. It no longer counts in the sale total (rule R9).
/// </summary>
/// <param name="SaleId">The id of the sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="ItemId">The id of the cancelled line.</param>
/// <param name="ProductId">The id of the line's product.</param>
/// <param name="OccurredAt">When the line was cancelled, in UTC.</param>
public sealed record ItemCancelledEvent(Guid SaleId, string SaleNumber, Guid ItemId, Guid ProductId, DateTime OccurredAt) : IDomainEvent;
