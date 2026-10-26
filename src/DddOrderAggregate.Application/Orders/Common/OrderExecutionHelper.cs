using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Common;

/// <summary>
/// Internal orchestration helper factoring the repetitive load/version-check/mutate/save/map pipeline.
/// </summary>
internal static class OrderExecutionHelper
{
    public static async Task<Result<OrderResponse>> ExecuteMutationAsync(
        IOrderRepository repository,
        IUnitOfWork unitOfWork,
        OrderId orderId,
        int? expectedVersion,
        Func<Order, Result> mutate,
        CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return OrderErrors.NotFound(orderId);
        }

        if (expectedVersion.HasValue && expectedVersion.Value != order.Version)
        {
            return OrderErrors.VersionMismatch(expectedVersion.Value, order.Version);
        }

        var result = mutate(order);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return order.ToResponse();
    }
}
