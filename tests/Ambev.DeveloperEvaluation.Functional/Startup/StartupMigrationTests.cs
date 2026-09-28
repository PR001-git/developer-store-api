using Ambev.DeveloperEvaluation.Functional.Fixtures;
using Ambev.DeveloperEvaluation.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Startup;

/// <summary>
/// Contains functional tests for the migrations the API applies when it starts in Development.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class StartupMigrationTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupMigrationTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public StartupMigrationTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that starting the API in Development applied every migration to the empty database.
    /// </summary>
    [Fact(DisplayName = "Given the API started in Development When reading the migration history Then every migration is applied")]
    public async Task Given_ApiStartedInDevelopment_When_ReadingMigrationHistory_Then_EveryMigrationIsApplied()
    {
        // Given the API started in Development against an empty database (ApiFixture)
        await using var scope = _api.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        // When
        var applied = await context.Database.GetAppliedMigrationsAsync();

        // Then
        applied.Should().Equal(context.Database.GetMigrations());
    }
}
