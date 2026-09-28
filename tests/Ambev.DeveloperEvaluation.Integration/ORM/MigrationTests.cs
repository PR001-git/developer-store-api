using Ambev.DeveloperEvaluation.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for the EF Core migrations on PostgreSQL 13.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class MigrationTests
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public MigrationTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests that the fixture applied every migration in the ORM assembly, in order.
    /// </summary>
    [Fact(DisplayName = "Given the PostgreSQL fixture When reading the migration history Then every migration is applied")]
    public async Task Given_PostgreSqlFixture_When_ReadingMigrationHistory_Then_EveryMigrationIsApplied()
    {
        // Given
        await using var context = _database.CreateContext();

        // When
        var applied = await context.Database.GetAppliedMigrationsAsync();

        // Then
        applied.Should().Equal(context.Database.GetMigrations());
    }
}
