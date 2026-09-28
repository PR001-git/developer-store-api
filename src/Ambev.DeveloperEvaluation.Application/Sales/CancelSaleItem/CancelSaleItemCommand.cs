using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

/// <summary>
/// Cancels one line of a sale. Cancelling the last active line also cancels the sale.
/// </summary>
/// <param name="SaleId">The id of the sale.</param>
/// <param name="ItemId">The id of the line.</param>
public sealed record CancelSaleItemCommand(Guid SaleId, Guid ItemId) : IRequest<SaleResult>;
