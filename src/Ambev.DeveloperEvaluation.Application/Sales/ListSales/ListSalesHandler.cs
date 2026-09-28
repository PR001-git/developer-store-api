using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Handles <see cref="ListSalesQuery"/>: applies the defaults, parses the order, builds the filter and reads the page.
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

        var listQuery = new SaleListQuery(page, size, SaleOrderParser.Parse(query.Order)) { Filter = ToFilter(query) };
        var sales = await _saleRepository.ListAsync(listQuery, cancellationToken);

        return new ListSalesResult(_mapper.Map<List<SaleResult>>(sales.Sales), page, size, sales.TotalCount);
    }

    /// <summary>
    /// Copies the filters, turning the sale-date limits into inclusive UTC bounds.
    /// </summary>
    private static SaleListFilter ToFilter(ListSalesQuery query) => new()
    {
        SaleNumber = query.SaleNumber,
        CustomerName = query.CustomerName,
        BranchName = query.BranchName,
        CustomerId = query.CustomerId,
        BranchId = query.BranchId,
        IsCancelled = query.IsCancelled,
        MinSaleDate = query.MinSaleDate is { } min ? SaleDateBounds.LowerBound(min) : null,
        MaxSaleDate = query.MaxSaleDate is { } max ? SaleDateBounds.UpperBound(max) : null,
        MinTotalAmount = query.MinTotalAmount,
        MaxTotalAmount = query.MaxTotalAmount
    };
}
