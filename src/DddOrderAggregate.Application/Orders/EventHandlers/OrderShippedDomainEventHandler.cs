using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders.Events;
using Microsoft.Extensions.Logging;

namespace DddOrderAggregate.Application.Orders.EventHandlers;

/// <summary>
/// Domain event handler for OrderShipped. Demonstrates local handling without outbox publication.
/// </summary>
public sealed partial class OrderShippedDomainEventHandler : IDomainEventHandler<OrderShippedDomainEvent>
{
    private readonly ILogger<OrderShippedDomainEventHandler> _logger;

    public OrderShippedDomainEventHandler(ILogger<OrderShippedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderShippedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogOrderShipped(_logger, domainEvent.OrderId.Value, domainEvent.OccurredOn);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order '{OrderId}' successfully marked as shipped on {OccurredOn}.")]
    private static partial void LogOrderShipped(ILogger logger, Guid orderId, DateTimeOffset occurredOn);
}
