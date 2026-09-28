using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;

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
    /// <exception cref="DomainException">
    /// Thrown when another sale already has the sale number, as when two requests pass <see cref="ExistsBySaleNumberAsync"/> together.
    /// </exception>
    Task CreateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a sale with its items. The sale is tracked, so a handler can change it and save it.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sale, or <c>null</c> if there is none with that id.</returns>
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the changes made to a sale loaded with <see cref="GetByIdAsync"/>, its items included.
    /// </summary>
    /// <param name="sale">The loaded, changed sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the changes are saved.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the sale wasn't loaded with <see cref="GetByIdAsync"/>.</exception>
    Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one page of sales with their items, untracked. The sales follow <see cref="SaleListQuery.Sorts"/>,
    /// then their id, so sales with equal keys always come in one order and pages never overlap.
    /// </summary>
    /// <param name="query">The page, its size and the order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, and the number of sales in all.</returns>
    Task<SalePage> ListAsync(SaleListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells whether a sale already has this sale number. The comparison is exact: case counts.
    /// </summary>
    /// <param name="saleNumber">The sale number, already trimmed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a sale has the number.</returns>
    Task<bool> ExistsBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default);
}
