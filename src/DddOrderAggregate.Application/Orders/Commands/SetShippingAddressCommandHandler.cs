using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Orders.Common;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Commands;

public sealed record SetShippingAddressCommand(
    Guid OrderId,
    string Street,
    string City,
    string PostalCode,
    string CountryCode,
    int? ExpectedVersion = null);

/// <summary>
/// Orchestrates setting the validated delivery destination address.
/// </summary>
public sealed class SetShippingAddressCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SetShippingAddressCommandHandler(
        IOrderRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrderResponse>> HandleAsync(SetShippingAddressCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var addressResult = Address.Create(command.Street, command.City, command.PostalCode, command.CountryCode);
        if (addressResult.IsFailure)
        {
            return addressResult.Error;
        }

        var orderId = OrderId.From(command.OrderId);

        return await OrderExecutionHelper.ExecuteMutationAsync(
            _repository,
            _unitOfWork,
            orderId,
            command.ExpectedVersion,
            order => order.SetShippingAddress(addressResult.Value, _timeProvider.GetUtcNow()),
            cancellationToken).ConfigureAwait(false);
    }
}
