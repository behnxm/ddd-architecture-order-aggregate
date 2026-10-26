using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Queries;

/// <summary>
/// Handles retrieving a complete order response via the high-performance read repository.
/// </summary>
public sealed class GetOrderQueryHandler
{
    private readonly IOrderReadRepository _readRepository;

    public GetOrderQueryHandler(IOrderReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<OrderResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _readRepository.GetOrderAsync(id, cancellationToken).ConfigureAwait(false);
        if (response is null)
        {
            return OrderErrors.NotFound(OrderId.From(id));
        }

        return response;
    }
}
