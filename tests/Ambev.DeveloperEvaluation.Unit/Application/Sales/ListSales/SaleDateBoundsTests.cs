using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.ListSales;

/// <summary>
/// Contains unit tests for <see cref="SaleDateBounds"/>, which reads <c>_minSaleDate</c> and <c>_maxSaleDate</c>
/// as inclusive UTC bounds.
/// </summary>
public sealed class SaleDateBoundsTests
{
    private static readonly DateTime LastMicrosecondOfJanuary31 = new(2026, 1, 31, 23, 59, 59, 999, 999, DateTimeKind.Utc);

    /// <summary>
    /// Tests that a min without an offset is read as UTC, and a UTC min stays as it is.
    /// </summary>
    [Theory(DisplayName = "Given a min date without an offset or in UTC When reading its lower bound Then it is that clock time in UTC")]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    public void Given_UnspecifiedOrUtcMinDate_When_ReadingLowerBound_Then_SameClockTimeInUtc(DateTimeKind kind)
    {
        // When
        var bound = SaleDateBounds.LowerBound(new DateTime(2026, 1, 31, 10, 0, 0, kind));

        // Then
        bound.Should().Be(new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc));
        bound.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that a local min is converted to UTC. On a machine whose time zone is UTC this can't tell a
    /// conversion from none; the kind check still holds.
    /// </summary>
    [Fact(DisplayName = "Given a local min date When reading its lower bound Then it is converted to UTC")]
    public void Given_LocalMinDate_When_ReadingLowerBound_Then_ConvertedToUtc()
    {
        // Given
        var instant = new DateTime(2026, 1, 31, 13, 0, 0, DateTimeKind.Utc);

        // When
        var bound = SaleDateBounds.LowerBound(instant.ToLocalTime());

        // Then
        bound.Should().Be(instant);
        bound.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests the whole-day rule: a max at exactly midnight UTC covers that day, up to its last microsecond.
    /// </summary>
    [Theory(DisplayName = "Given a max date at exactly midnight When reading its upper bound Then it is that day's last microsecond in UTC")]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    public void Given_MidnightMaxDate_When_ReadingUpperBound_Then_LastMicrosecondOfThatDay(DateTimeKind kind)
    {
        // When
        var bound = SaleDateBounds.UpperBound(new DateTime(2026, 1, 31, 0, 0, 0, kind));

        // Then
        bound.Should().Be(LastMicrosecondOfJanuary31);
        bound.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that a max with any time after midnight is that instant, not a whole day.
    /// </summary>
    [Theory(DisplayName = "Given a max date after midnight When reading its upper bound Then it is that instant in UTC")]
    [InlineData(TimeSpan.TicksPerMicrosecond)]
    [InlineData(TimeSpan.TicksPerSecond)]
    [InlineData(TimeSpan.TicksPerHour * 12)]
    public void Given_MaxDateAfterMidnight_When_ReadingUpperBound_Then_ThatInstant(long ticksAfterMidnight)
    {
        // Given
        var maxSaleDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Unspecified).AddTicks(ticksAfterMidnight);

        // When
        var bound = SaleDateBounds.UpperBound(maxSaleDate);

        // Then
        bound.Should().Be(DateTime.SpecifyKind(maxSaleDate, DateTimeKind.Utc));
        bound.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that the last representable day still ends at its last microsecond, without overflowing.
    /// </summary>
    [Fact(DisplayName = "Given 9999-12-31 as the max date When reading its upper bound Then it ends that day without overflowing")]
    public void Given_LastRepresentableDay_When_ReadingUpperBound_Then_EndsThatDayWithoutOverflow()
    {
        // When
        var bound = SaleDateBounds.UpperBound(new DateTime(9999, 12, 31));

        // Then
        bound.Should().Be(new DateTime(9999, 12, 31, 23, 59, 59, 999, 999, DateTimeKind.Utc));
    }
}
