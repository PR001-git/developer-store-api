namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// A sale was updated: its header was replaced and its lines were reconciled (rule R10). It is recorded on every
/// update, after that update's <see cref="ItemCancelledEvent"/>s, even when nothing changed.
/// </summary>
/// <param name="SaleId">The id of the sale.</param>
/// <param name="SaleNumber">The sale number.</param>
/// <param name="TotalAmount">The sale total after the update.</param>
/// <param name="OccurredAt">When the sale was updated, in UTC.</param>
public sealed record SaleModifiedEvent(Guid SaleId, string SaleNumber, decimal TotalAmount, DateTime OccurredAt) : IDomainEvent;
