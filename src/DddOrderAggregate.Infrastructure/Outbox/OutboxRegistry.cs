using System.Collections.Frozen;
using System.Text.Json;
using DddOrderAggregate.Application.IntegrationEvents;

namespace DddOrderAggregate.Infrastructure.Outbox;

/// <summary>
/// Static registry mapping stable contract names to known integration event types.
/// Explicitly forbids dynamic Type.GetType to eliminate arbitrary type loading and security vulnerabilities.
/// </summary>
public static class OutboxRegistry
{
    private static readonly FrozenDictionary<string, Type> NameToTypeMap =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            [OrderSubmittedIntegrationEvent.StableContractName] = typeof(OrderSubmittedIntegrationEvent),
            [OrderCancelledIntegrationEvent.StableContractName] = typeof(OrderCancelledIntegrationEvent)
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize<TEvent>(TEvent integrationEvent) where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions);
    }

    public static IIntegrationEvent? Deserialize(string contractName, string payload)
    {
        if (!NameToTypeMap.TryGetValue(contractName, out var eventType))
        {
            throw new InvalidOperationException($"Unrecognized outbox integration event contract: '{contractName}'.");
        }

        return JsonSerializer.Deserialize(payload, eventType, JsonOptions) as IIntegrationEvent;
    }
}
