using Ambev.DeveloperEvaluation.Domain.Entities;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Publishes the events a sale recorded, for every Sales handler that changes a sale.
/// </summary>
public static class SaleEventPublisher
{
    /// <summary>
    /// Publishes the sale's recorded events through MediatR, in the order they were recorded, then clears them.
    /// Call it only after the save has returned, so a failed save publishes nothing (rule R14).
    /// </summary>
    /// <param name="publisher">The MediatR publisher.</param>
    /// <param name="sale">The saved sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when every event has been handled.</returns>
    public static async Task PublishDomainEventsAsync(this IPublisher publisher, Sale sale, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in sale.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);

        sale.ClearDomainEvents();
    }
}
