using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.ORM.Repositories;

/// <summary>
/// Contains unit tests for <see cref="LikePattern"/>, the translation from the <c>*</c> wildcards of spec §7.3
/// to an ILIKE pattern whose escape character is <c>\</c>.
/// </summary>
public sealed class LikePatternTests
{
    /// <summary>
    /// Tests the four wildcard forms, the literal characters (an inner <c>*</c>, <c>%</c>, <c>_</c>, <c>\</c>)
    /// and the bare wildcards.
    /// </summary>
    [Theory(DisplayName = "Given a text filter When building its pattern Then only a leading or trailing * is a wildcard and the rest is literal")]
    [InlineData("Mar*", "Mar%")]
    [InlineData("*Maria", "%Maria")]
    [InlineData("*ari*", "%ari%")]
    [InlineData("Maria", "Maria")]
    [InlineData("Ma*ria", "Ma*ria")]
    [InlineData("*a*b*", "%a*b%")]
    [InlineData("*50%*", @"%50\%%")]
    [InlineData("S_5", @"S\_5")]
    [InlineData(@"Filial A\B", @"Filial A\\B")]
    [InlineData(@"a\*", @"a\\%")]
    [InlineData("*", "%")]
    [InlineData("**", "%%")]
    [InlineData("***", "%*%")]
    public void Given_TextFilter_When_BuildingPattern_Then_OnlyEdgeStarsAreWildcards(string text, string expected)
    {
        // When
        var pattern = LikePattern.FromWildcards(text);

        // Then
        pattern.Should().Be(expected);
    }
}
