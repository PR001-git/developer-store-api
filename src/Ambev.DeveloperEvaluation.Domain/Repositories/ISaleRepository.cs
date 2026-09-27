using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Stores and loads <see cref="Sale"/> aggregates, always together with their items.
/// </summary>
public interface ISaleRepository
{
    /// <summary>
    /// Saves a new sale and its items.
    /// </summary>
    /// <param name="sale">The sale to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the sale is saved.</returns>
    Task CreateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a sale with its items. The sale is tracked, so a handler can change it and save it.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sale, or <c>null</c> if there is none with that id.</returns>
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells whether a sale already has this sale number. The comparison is exact: case counts.
    /// </summary>
    /// <param name="saleNumber">The sale number, already trimmed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a sale has the number.</returns>
    Task<bool> ExistsBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default);
}
