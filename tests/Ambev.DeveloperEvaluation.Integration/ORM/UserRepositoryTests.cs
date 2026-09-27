using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for <see cref="UserRepository"/> against the migrated PostgreSQL schema.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class UserRepositoryTests
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepositoryTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public UserRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests that a saved user, including its timestamps, reads back unchanged from a fresh context.
    /// </summary>
    [Fact(DisplayName = "Given a valid user When it is saved Then a new context reads it back unchanged")]
    public async Task Given_ValidUser_When_Saved_Then_NewContextReadsItBackUnchanged()
    {
        // Given
        var user = UserTestData.GenerateValidUser();

        // When
        await using (var writeContext = _database.CreateContext())
        {
            await new UserRepository(writeContext).CreateAsync(user);
        }

        await using var readContext = _database.CreateContext();
        var saved = await new UserRepository(readContext).GetByIdAsync(user.Id);

        // Then (timestamptz keeps microseconds; DateTime keeps 100 ns ticks)
        saved.Should().BeEquivalentTo(user, options => options
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMicroseconds(1)))
            .WhenTypeIs<DateTime>());
    }
}
