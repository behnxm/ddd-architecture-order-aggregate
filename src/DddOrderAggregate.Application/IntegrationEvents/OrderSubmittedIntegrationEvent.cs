namespace DddOrderAggregate.Application.IntegrationEvents;

/// <summary>
/// Integration event published when an order is submitted for fulfillment.
/// Contract: ordering.order-submitted.v1
/// </summary>
public sealed record OrderSubmittedIntegrationEvent(
    Guid MessageId,
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset OccurredOn) : IIntegrationEvent
{
    public const string StableContractName = "ordering.order-submitted.v1";

    public string ContractName => StableContractName;

    public static OrderSubmittedIntegrationEvent Create(
        Guid orderId,
        Guid customerId,
        decimal totalAmount,
        string currency,
        DateTimeOffset occurredOn) =>
        new(Guid.CreateVersion7(), orderId, customerId, totalAmount, currency, occurredOn);
}
