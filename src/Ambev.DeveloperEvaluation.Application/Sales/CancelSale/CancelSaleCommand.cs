using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

/// <summary>
/// Cancels a sale. Its items and total stay as they were.
/// </summary>
/// <param name="Id">The id of the sale.</param>
public sealed record CancelSaleCommand(Guid Id) : IRequest<SaleResult>;
