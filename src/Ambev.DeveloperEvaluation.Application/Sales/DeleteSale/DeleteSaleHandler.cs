using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

/// <summary>
/// Handles <see cref="DeleteSaleCommand"/>: the sale marks itself deleted and the repository saves it.
/// Nothing is published (rule R11).
/// </summary>
public sealed class DeleteSaleHandler : IRequestHandler<DeleteSaleCommand, Unit>
{
    private readonly ISaleRepository _saleRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSaleHandler"/> class.
    /// </summary>
    /// <param name="saleRepository">The sale repository.</param>
    public DeleteSaleHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    /// <summary>
    /// Loads the sale, deletes it and saves it.
    /// </summary>
    /// <param name="command">The validated command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Unit.Value"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when there is no sale with the id, or it was already deleted.</exception>
    public async Task<Unit> Handle(DeleteSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"The sale with ID {command.Id} does not exist");

        sale.Delete();

        await _saleRepository.UpdateAsync(sale, cancellationToken);

        return Unit.Value;
    }
}
