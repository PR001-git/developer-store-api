using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

/// <summary>
/// Gets one sale, with its items.
/// </summary>
/// <param name="Id">The id of the sale.</param>
public sealed record GetSaleQuery(Guid Id) : IRequest<SaleResult>;
