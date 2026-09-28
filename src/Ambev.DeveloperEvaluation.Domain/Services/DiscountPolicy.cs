using Ambev.DeveloperEvaluation.Domain.Exceptions;

namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// The quantity-based discount tiers for a sale line (rule R1).
/// </summary>
/// <remarks>
/// Identical items are the items of one product. 1 to 3 get no discount, 4 to 9 get 10%, 10 to 20 get 20%,
/// and more than 20 can't be sold. Quantity 4 gets 10% (spec decision D3).
/// </remarks>
public static class DiscountPolicy
{
    /// <summary>
    /// The smallest quantity a line can have.
    /// </summary>
    public const int MinQuantity = 1;

    /// <summary>
    /// The largest quantity a line can have.
    /// </summary>
    public const int MaxQuantity = 20;

    /// <summary>
    /// The message for a quantity above <see cref="MaxQuantity"/>, as rule R1 words it.
    /// </summary>
    public const string MaxQuantityExceededMessage = "It's not possible to sell above 20 identical items";

    /// <summary>
    /// Returns the discount percentage for a line with the given quantity of identical items.
    /// </summary>
    /// <param name="quantity">The quantity of identical items, from 1 to 20.</param>
    /// <returns>0, 10 or 20.</returns>
    /// <exception cref="DomainException">Thrown when the quantity is below 1 or above 20.</exception>
    public static decimal GetDiscountPercentage(int quantity) => quantity switch
    {
        < MinQuantity => throw new DomainException($"Quantity must be at least {MinQuantity}"),
        > MaxQuantity => throw new DomainException(MaxQuantityExceededMessage),
        >= 10 => 20m,
        >= 4 => 10m,
        _ => 0m
    };
}
