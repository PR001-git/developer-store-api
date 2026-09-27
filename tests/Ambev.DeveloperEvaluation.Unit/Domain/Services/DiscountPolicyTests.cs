using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Services;

/// <summary>
/// Contains unit tests for <see cref="DiscountPolicy"/>: the quantity-based discount tiers of rule R1.
/// </summary>
public sealed class DiscountPolicyTests
{
    /// <summary>
    /// Tests each tier at both of its boundaries. Quantity 4 gets 10% (spec decision D3).
    /// </summary>
    /// <param name="quantity">The quantity of identical items in the line.</param>
    /// <param name="expectedPercentage">The discount percentage the tier gives.</param>
    [Theory(DisplayName = "Given a quantity from 1 to 20 When getting the discount percentage Then it follows the tiers 0, 10 and 20")]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 10)]
    [InlineData(9, 10)]
    [InlineData(10, 20)]
    [InlineData(20, 20)]
    public void Given_QuantityFrom1To20_When_GettingDiscountPercentage_Then_FollowsTiers(int quantity, int expectedPercentage)
    {
        // When
        var percentage = DiscountPolicy.GetDiscountPercentage(quantity);

        // Then
        percentage.Should().Be(expectedPercentage);
    }

    /// <summary>
    /// Tests that more than 20 identical items can't be sold, with the message rule R1 gives.
    /// </summary>
    /// <param name="quantity">A quantity above 20.</param>
    [Theory(DisplayName = "Given a quantity above 20 When getting the discount percentage Then it throws DomainException with R1's message")]
    [InlineData(21)]
    [InlineData(100)]
    public void Given_QuantityAbove20_When_GettingDiscountPercentage_Then_ThrowsWithR1Message(int quantity)
    {
        // When
        var act = () => DiscountPolicy.GetDiscountPercentage(quantity);

        // Then
        act.Should().Throw<DomainException>().WithMessage("It's not possible to sell above 20 identical items");
    }

    /// <summary>
    /// Tests that a line needs at least one item.
    /// </summary>
    /// <param name="quantity">A quantity below 1.</param>
    [Theory(DisplayName = "Given a quantity below 1 When getting the discount percentage Then it throws DomainException")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Given_QuantityBelow1_When_GettingDiscountPercentage_Then_ThrowsDomainException(int quantity)
    {
        // When
        var act = () => DiscountPolicy.GetDiscountPercentage(quantity);

        // Then
        act.Should().Throw<DomainException>().WithMessage("Quantity must be at least 1");
    }
}
