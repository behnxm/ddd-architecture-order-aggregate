using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Orders.Queries;

/// <summary>
/// Handles querying paginated order summaries via the high-performance read repository.
/// </summary>
public sealed class ListOrdersQueryHandler
{
    private readonly IOrderReadRepository _readRepository;

    public ListOrdersQueryHandler(IOrderReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedList<OrderSummaryResponse>>> HandleAsync(
        Guid? customerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return Result.Failure<PagedList<OrderSummaryResponse>>(
                Error.Validation("Pagination.InvalidPage", "Page index must be greater than or equal to 1."));
        }

        if (pageSize is < 1 or > 100)
        {
            return Result.Failure<PagedList<OrderSummaryResponse>>(
                Error.Validation("Pagination.InvalidPageSize", "Page size must be between 1 and 100."));
        }

        var results = await _readRepository.ListOrdersAsync(
            customerId,
            status,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);

        return results;
    }
}
