using Ambev.DeveloperEvaluation.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.ORM;

/// <summary>
/// Contains unit tests for the options that <see cref="DefaultContext"/> sets on itself. No database is opened.
/// </summary>
public sealed class DefaultContextTests
{
    /// <summary>
    /// Tests spec §8.2: <c>Sale</c> has the soft-delete filter and <c>SaleItem</c> doesn't, and the warning about that
    /// is ignored, because items are only ever loaded through their sale.
    /// </summary>
    [Fact(DisplayName = "Given a DefaultContext When reading its warning settings Then the required-navigation query-filter warning is ignored")]
    public void Given_DefaultContext_When_ReadingWarningSettings_Then_QueryFilterNavigationWarningIsIgnored()
    {
        // Given
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options;
        using var context = new DefaultContext(options);

        // When
        var warnings = context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()!.WarningsConfiguration;

        // Then
        warnings.GetBehavior(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)
            .Should().Be(WarningBehavior.Ignore);
    }
}
