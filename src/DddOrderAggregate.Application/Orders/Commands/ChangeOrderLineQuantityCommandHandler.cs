using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Orders.Common;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Commands;

public sealed record ChangeOrderLineQuantityCommand(
    Guid OrderId,
    Guid LineId,
    int Quantity,
    int? ExpectedVersion = null);

/// <summary>
/// Orchestrates updating the quantity of an existing line item.
/// </summary>
public sealed class ChangeOrderLineQuantityCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ChangeOrderLineQuantityCommandHandler(
        IOrderRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrderResponse>> HandleAsync(ChangeOrderLineQuantityCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var quantityResult = Quantity.Create(command.Quantity);
        if (quantityResult.IsFailure)
        {
            return quantityResult.Error;
        }

        var orderId = OrderId.From(command.OrderId);
        var lineId = OrderLineId.From(command.LineId);

        return await OrderExecutionHelper.ExecuteMutationAsync(
            _repository,
            _unitOfWork,
            orderId,
            command.ExpectedVersion,
            order => order.ChangeLineQuantity(lineId, quantityResult.Value, _timeProvider.GetUtcNow()),
            cancellationToken).ConfigureAwait(false);
    }
}
