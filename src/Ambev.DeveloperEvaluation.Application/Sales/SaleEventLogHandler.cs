using Ambev.DeveloperEvaluation.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Writes each published sale event to the application log as a structured entry. It stands in for a message
/// broker (spec decision D8), and it is the one handler for every sale event: later events add an interface here.
/// </summary>
public sealed class SaleEventLogHandler :
    INotificationHandler<SaleCreatedEvent>,
    INotificationHandler<SaleCancelledEvent>,
    INotificationHandler<ItemCancelledEvent>
{
    private readonly ILogger<SaleEventLogHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleEventLogHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger; Serilog writes its entries.</param>
    public SaleEventLogHandler(ILogger<SaleEventLogHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs a <see cref="SaleCreatedEvent"/>.
    /// </summary>
    /// <param name="notification">The event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(SaleCreatedEvent notification, CancellationToken cancellationToken) => Log(notification);

    /// <summary>
    /// Logs a <see cref="SaleCancelledEvent"/>.
    /// </summary>
    /// <param name="notification">The event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(SaleCancelledEvent notification, CancellationToken cancellationToken) => Log(notification);

    /// <summary>
    /// Logs an <see cref="ItemCancelledEvent"/>.
    /// </summary>
    /// <param name="notification">The event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(ItemCancelledEvent notification, CancellationToken cancellationToken) => Log(notification);

    private Task Log(IDomainEvent domainEvent)
    {
        _logger.LogInformation("Sale event {EventName} published {@Event}", domainEvent.GetType().Name, domainEvent);
        return Task.CompletedTask;
    }
}
