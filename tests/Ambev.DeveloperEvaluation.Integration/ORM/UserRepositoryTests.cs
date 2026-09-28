using Ambev.DeveloperEvaluation.Domain.Exceptions;
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

    /// <summary>
    /// Tests that the same email, saved with different casing and surrounding whitespace, is stored normalized.
    /// </summary>
    [Fact(DisplayName = "Given an email with mixed case and whitespace When saved Then it is stored trimmed and lower-invariant")]
    public async Task Given_MixedCaseEmailWithWhitespace_When_Saved_Then_StoredTrimmedAndLowerInvariant()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        user.Email = $"  {user.Email.ToUpperInvariant()}  ";
        var expectedEmail = user.Email.Trim().ToLowerInvariant();

        // When
        await using var context = _database.CreateContext();
        await new UserRepository(context).CreateAsync(user);

        // Then
        user.Email.Should().Be(expectedEmail);
    }

    /// <summary>
    /// Tests that a lookup by email is case-insensitive, so a login attempt matches regardless of the casing used at sign-up.
    /// </summary>
    [Fact(DisplayName = "Given a saved user When looking it up by email in a different case Then it is found")]
    public async Task Given_SavedUser_When_LookingUpByEmailInDifferentCase_Then_ItIsFound()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        await using (var writeContext = _database.CreateContext())
        {
            await new UserRepository(writeContext).CreateAsync(user);
        }

        // When
        await using var readContext = _database.CreateContext();
        var found = await new UserRepository(readContext).GetByEmailAsync(user.Email.ToUpperInvariant());

        // Then
        found.Should().NotBeNull();
        found!.Id.Should().Be(user.Id);
    }

    /// <summary>
    /// Tests that the unique index on Users.Email rejects a second insert for the same email even when the
    /// casing differs, turning what would be a raw Postgres 23505 violation into the same DomainException
    /// the caller's own pre-check throws.
    /// </summary>
    [Fact(DisplayName = "Given a saved user When creating another with the same email in a different case Then throws DomainException")]
    public async Task Given_SavedUser_When_CreatingAnotherWithSameEmailInDifferentCase_Then_ThrowsDomainException()
    {
        // Given
        var first = UserTestData.GenerateValidUser();
        await using (var writeContext = _database.CreateContext())
        {
            await new UserRepository(writeContext).CreateAsync(first);
        }

        var second = UserTestData.GenerateValidUser();
        second.Email = first.Email.ToUpperInvariant();

        // When
        await using var secondContext = _database.CreateContext();
        var act = () => new UserRepository(secondContext).CreateAsync(second);

        // Then
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"User with email {second.Email} already exists");
    }
}
