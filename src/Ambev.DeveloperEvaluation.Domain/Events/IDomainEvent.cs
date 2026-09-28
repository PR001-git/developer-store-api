using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Something that happened to an aggregate. The aggregate records it; the application publishes it
/// through MediatR only after the change is saved (spec decision D8).
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Gets when the event happened, in UTC: the moment the aggregate recorded it.
    /// </summary>
    DateTime OccurredAt { get; }
}
