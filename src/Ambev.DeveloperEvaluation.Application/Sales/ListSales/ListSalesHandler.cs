using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Handles <see cref="ListSalesQuery"/>: applies the defaults, parses the order and reads the page.
/// </summary>
public sealed class ListSalesHandler : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Reads the requested page of sales.
    /// </summary>
    /// <param name="query">The validated query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page of sales, with the page, size and total count.</returns>
    public async Task<ListSalesResult> Handle(ListSalesQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? ListSalesQuery.DefaultPage;
        var size = query.Size ?? ListSalesQuery.DefaultSize;

        var sales = await _saleRepository.ListAsync(
            new SaleListQuery(page, size, SaleOrderParser.Parse(query.Order)), cancellationToken);

        return new ListSalesResult(_mapper.Map<List<SaleResult>>(sales.Sales), page, size, sales.TotalCount);
    }
}
