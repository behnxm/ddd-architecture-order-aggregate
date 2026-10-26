using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders.Events;
using Microsoft.Extensions.Logging;

namespace DddOrderAggregate.Application.Orders.EventHandlers;

/// <summary>
/// Domain event handler for OrderPaid. Demonstrates that not all domain events translate to integration events.
/// This side effect remains local to the service boundary (structured audit log).
/// </summary>
public sealed partial class OrderPaidDomainEventHandler : IDomainEventHandler<OrderPaidDomainEvent>
{
    private readonly ILogger<OrderPaidDomainEventHandler> _logger;

    public OrderPaidDomainEventHandler(ILogger<OrderPaidDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderPaidDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        LogOrderPaid(_logger, domainEvent.OrderId.Value, domainEvent.OccurredOn);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order '{OrderId}' successfully marked as paid on {OccurredOn}.")]
    private static partial void LogOrderPaid(ILogger logger, Guid orderId, DateTimeOffset occurredOn);
}
