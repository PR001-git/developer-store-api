using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

/// <summary>
/// Handles <see cref="GetSaleQuery"/>.
/// </summary>
public sealed class GetSaleHandler : IRequestHandler<GetSaleQuery, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSaleHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public GetSaleHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Loads the sale with its items.
    /// </summary>
    /// <param name="query">The validated query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sale.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when there is no sale with the id.</exception>
    public async Task<SaleResult> Handle(GetSaleQuery query, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"The sale with ID {query.Id} does not exist");

        return _mapper.Map<SaleResult>(sale);
    }
}
