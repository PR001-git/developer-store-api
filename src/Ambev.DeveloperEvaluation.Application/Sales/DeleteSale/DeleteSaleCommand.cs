using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

/// <summary>
/// Soft-deletes a sale. It returns no data; <see cref="Unit"/> keeps it an <see cref="IRequest{TResponse}"/>,
/// so the MediatR <c>ValidationBehavior</c> runs <see cref="DeleteSaleCommandValidator"/> first.
/// </summary>
/// <param name="Id">The id of the sale.</param>
public sealed record DeleteSaleCommand(Guid Id) : IRequest<Unit>;
