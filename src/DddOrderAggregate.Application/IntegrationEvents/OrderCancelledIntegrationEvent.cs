namespace DddOrderAggregate.Application.IntegrationEvents;

/// <summary>
/// Integration event published when an order is cancelled.
/// Contract: ordering.order-cancelled.v1
/// </summary>
public sealed record OrderCancelledIntegrationEvent(
    Guid MessageId,
    Guid OrderId,
    string Reason,
    DateTimeOffset OccurredOn) : IIntegrationEvent
{
    public const string StableContractName = "ordering.order-cancelled.v1";

    public string ContractName => StableContractName;

    public static OrderCancelledIntegrationEvent Create(
        Guid orderId,
        string reason,
        DateTimeOffset occurredOn) =>
        new(Guid.CreateVersion7(), orderId, reason, occurredOn);
}
