namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// Issues the sale number of a sale created without one (rule R12).
/// </summary>
public interface ISaleNumberGenerator
{
    /// <summary>
    /// Returns the next free sale number: <c>S-</c> and the next value of the sale number sequence, zero-padded to
    /// 6 digits and longer when needed. Numbers that a sale already has are skipped (spec §8.3).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A sale number that no sale had when it was checked.</returns>
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
