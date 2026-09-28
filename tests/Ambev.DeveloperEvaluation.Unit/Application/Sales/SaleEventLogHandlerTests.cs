using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Domain.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for <see cref="SaleEventLogHandler"/>, which writes each published sale event to the log
/// in place of a message broker.
/// </summary>
public sealed class SaleEventLogHandlerTests
{
    private readonly ILogger<SaleEventLogHandler> _logger = Substitute.For<ILogger<SaleEventLogHandler>>();

    /// <summary>
    /// Tests that the event becomes one structured Information entry: the template, the event name and the
    /// event itself, which Serilog destructures because of the <c>@</c>.
    /// </summary>
    [Fact(DisplayName = "Given a SaleCreatedEvent When the log handler handles it Then it writes one structured Information entry with the event")]
    public async Task Given_SaleCreatedEvent_When_Handled_Then_WritesStructuredInformationEntry()
    {
        // Given
        var handler = new SaleEventLogHandler(_logger);
        var saleCreated = new SaleCreatedEvent(Guid.NewGuid(), "S-000123", Guid.NewGuid(), Guid.NewGuid(), 20.25m, 1, DateTime.UtcNow);

        // When
        await handler.Handle(saleCreated, CancellationToken.None);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Information);
        logCall.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>().Which.Should().Equal(
            new KeyValuePair<string, object?>("EventName", "SaleCreatedEvent"),
            new KeyValuePair<string, object?>("@Event", saleCreated),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }

    /// <summary>
    /// Tests the wiring the handlers rely on: an event published through <see cref="IPublisher"/> as
    /// <see cref="IDomainEvent"/>, as <see cref="SaleEventPublisher"/> does, reaches the handler of its concrete type.
    /// </summary>
    [Fact(DisplayName = "Given MediatR with the Application handlers When a SaleCreatedEvent is published as IDomainEvent Then the log handler writes it")]
    public async Task Given_MediatRWithApplicationHandlers_When_EventPublishedAsIDomainEvent_Then_LogHandlerWritesIt()
    {
        // Given
        var services = new ServiceCollection();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        services.AddSingleton(_logger);
        await using var provider = services.BuildServiceProvider();
        IDomainEvent saleCreated = new SaleCreatedEvent(Guid.NewGuid(), "S-000123", Guid.NewGuid(), Guid.NewGuid(), 20.25m, 1, DateTime.UtcNow);

        // When
        await provider.GetRequiredService<IPublisher>().Publish(saleCreated);

        // Then
        _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Which.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>()
            .Which.Should().Contain(new KeyValuePair<string, object?>("@Event", saleCreated));
    }

    /// <summary>
    /// Tests that a <see cref="SaleCancelledEvent"/> published as <see cref="IDomainEvent"/> reaches the log handler
    /// and becomes one structured Information entry.
    /// </summary>
    [Fact(DisplayName = "Given MediatR with the Application handlers When a SaleCancelledEvent is published as IDomainEvent Then the log handler writes one structured Information entry with it")]
    public async Task Given_MediatRWithApplicationHandlers_When_SaleCancelledEventPublished_Then_LogHandlerWritesIt()
    {
        // Given
        var services = new ServiceCollection();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        services.AddSingleton(_logger);
        await using var provider = services.BuildServiceProvider();
        IDomainEvent saleCancelled = new SaleCancelledEvent(Guid.NewGuid(), "S-000123", DateTime.UtcNow);

        // When
        await provider.GetRequiredService<IPublisher>().Publish(saleCancelled);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Information);
        logCall.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>().Which.Should().Equal(
            new KeyValuePair<string, object?>("EventName", "SaleCancelledEvent"),
            new KeyValuePair<string, object?>("@Event", saleCancelled),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }

    /// <summary>
    /// Tests that an <see cref="ItemCancelledEvent"/> published as <see cref="IDomainEvent"/> reaches the log handler
    /// and becomes one structured Information entry.
    /// </summary>
    [Fact(DisplayName = "Given MediatR with the Application handlers When an ItemCancelledEvent is published as IDomainEvent Then the log handler writes one structured Information entry with it")]
    public async Task Given_MediatRWithApplicationHandlers_When_ItemCancelledEventPublished_Then_LogHandlerWritesIt()
    {
        // Given
        var services = new ServiceCollection();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        services.AddSingleton(_logger);
        await using var provider = services.BuildServiceProvider();
        IDomainEvent itemCancelled = new ItemCancelledEvent(Guid.NewGuid(), "S-000123", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        // When
        await provider.GetRequiredService<IPublisher>().Publish(itemCancelled);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Information);
        logCall.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>().Which.Should().Equal(
            new KeyValuePair<string, object?>("EventName", "ItemCancelledEvent"),
            new KeyValuePair<string, object?>("@Event", itemCancelled),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }

    /// <summary>
    /// Tests that a <see cref="SaleModifiedEvent"/> published as <see cref="IDomainEvent"/> reaches the log handler
    /// and becomes one structured Information entry.
    /// </summary>
    [Fact(DisplayName = "Given MediatR with the Application handlers When a SaleModifiedEvent is published as IDomainEvent Then the log handler writes one structured Information entry with it")]
    public async Task Given_MediatRWithApplicationHandlers_When_SaleModifiedEventPublished_Then_LogHandlerWritesIt()
    {
        // Given
        var services = new ServiceCollection();
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        services.AddSingleton(_logger);
        await using var provider = services.BuildServiceProvider();
        IDomainEvent saleModified = new SaleModifiedEvent(Guid.NewGuid(), "S-000123", 52.00m, DateTime.UtcNow);

        // When
        await provider.GetRequiredService<IPublisher>().Publish(saleModified);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Information);
        logCall.GetArguments()[2].Should().BeAssignableTo<IReadOnlyList<KeyValuePair<string, object?>>>().Which.Should().Equal(
            new KeyValuePair<string, object?>("EventName", "SaleModifiedEvent"),
            new KeyValuePair<string, object?>("@Event", saleModified),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Sale event {EventName} published {@Event}"));
    }
}
