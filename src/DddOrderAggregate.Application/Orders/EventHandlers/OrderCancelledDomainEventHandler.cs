using DddOrderAggregate.Application.IntegrationEvents;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders.Events;
using Microsoft.Extensions.Logging;

namespace DddOrderAggregate.Application.Orders.EventHandlers;

/// <summary>
/// Translates internal OrderCancelledDomainEvent into an asynchronous OrderCancelledIntegrationEvent and writes it to the Outbox.
/// </summary>
public sealed partial class OrderCancelledDomainEventHandler : IDomainEventHandler<OrderCancelledDomainEvent>
{
    private readonly IOutbox _outbox;
    private readonly ILogger<OrderCancelledDomainEventHandler> _logger;

    public OrderCancelledDomainEventHandler(IOutbox outbox, ILogger<OrderCancelledDomainEventHandler> logger)
    {
        _outbox = outbox;
        _logger = logger;
    }

    public Task HandleAsync(OrderCancelledDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogOrderCancelled(_logger, domainEvent.OrderId.Value, domainEvent.Reason);

        var integrationEvent = OrderCancelledIntegrationEvent.Create(
            domainEvent.OrderId.Value,
            domainEvent.Reason,
            domainEvent.OccurredOn);

        _outbox.Enqueue(integrationEvent);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Domain event OrderCancelled received for Order '{OrderId}' with reason '{Reason}'. Enqueuing integration event.")]
    private static partial void LogOrderCancelled(ILogger logger, Guid orderId, string reason);
}
