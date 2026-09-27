using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for <see cref="SaleRepository"/> against the migrated PostgreSQL schema.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleRepositoryTests
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRepositoryTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests that a saved sale reads back unchanged from a fresh context: the owned customer, branch and products,
    /// every item through the backing field, the flags, and the amounts with their cents.
    /// </summary>
    [Fact(DisplayName = "Given a new sale When it is saved Then a new context reads it back with its identities, items, flags and amounts")]
    public async Task Given_NewSale_When_Saved_Then_NewContextReadsItBackUnchanged()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale();

        // When
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(sale);
        }

        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);

        // Then (timestamptz keeps microseconds; DateTime keeps 100 ns ticks. The events aren't stored.)
        saved.Should().BeEquivalentTo(sale, options => options
            .Excluding(expected => expected.DomainEvents)
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMicroseconds(1)))
            .WhenTypeIs<DateTime>());
    }

    /// <summary>
    /// Tests that the loaded sale and its items are tracked, so a handler can change the sale and save it.
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When loading it by id Then the context tracks the sale and each of its items")]
    public async Task Given_SavedSale_When_LoadingById_Then_ContextTracksSaleAndItems()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(sale);
        }

        // When
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);

        // Then
        saved.Should().NotBeNull();
        readContext.Entry(saved!).State.Should().Be(EntityState.Unchanged);
        readContext.ChangeTracker.Entries<SaleItem>().Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that an unknown id finds nothing.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When loading it by id Then it returns null")]
    public async Task Given_NoSaleWithId_When_LoadingById_Then_ReturnsNull()
    {
        // Given
        await using var context = _database.CreateContext();

        // When
        var saved = await new SaleRepository(context).GetByIdAsync(Guid.NewGuid());

        // Then
        saved.Should().BeNull();
    }

    /// <summary>
    /// Tests that a saved sale's exact number counts as taken.
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When checking its exact number Then the number exists")]
    public async Task Given_SavedSale_When_CheckingExactNumber_Then_NumberExists()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale();
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(sale);
        }

        // When
        await using var readContext = _database.CreateContext();
        var exists = await new SaleRepository(readContext).ExistsBySaleNumberAsync(sale.SaleNumber);

        // Then
        exists.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the comparison is exact (spec §12): the same number in another case isn't taken.
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When checking its number in another case Then the number doesn't exist")]
    public async Task Given_SavedSale_When_CheckingNumberInAnotherCase_Then_NumberDoesNotExist()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(saleNumber: $"S-CASE-{Guid.NewGuid():N}".ToUpperInvariant());
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(sale);
        }

        // When
        await using var readContext = _database.CreateContext();
        var exists = await new SaleRepository(readContext).ExistsBySaleNumberAsync(sale.SaleNumber.ToLowerInvariant());

        // Then
        exists.Should().BeFalse();
    }

    /// <summary>
    /// Tests spec §8.2: when two requests pass the existence check with the same number, the unique index stops the
    /// second save, and the repository reports it as <see cref="DomainException"/> (a 409), not as a database error (a 500).
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When saving another sale with the same number Then it throws DomainException and only the first is stored")]
    public async Task Given_SavedSale_When_SavingAnotherWithSameNumber_Then_ThrowsDomainException()
    {
        // Given
        var first = SaleTestData.GenerateValidSale();
        await using (var writeContext = _database.CreateContext())
        {
            await new SaleRepository(writeContext).CreateAsync(first);
        }

        var second = SaleTestData.GenerateValidSale(saleNumber: first.SaleNumber);

        // When
        await using var context = _database.CreateContext();
        var act = () => new SaleRepository(context).CreateAsync(second);

        // Then
        var thrown = await act.Should().ThrowAsync<DomainException>();
        thrown.WithMessage($"Sale number {first.SaleNumber} already exists").WithInnerException<DbUpdateException>();
        await using var readContext = _database.CreateContext();
        (await readContext.Sales.CountAsync(sale => sale.SaleNumber == first.SaleNumber)).Should().Be(1);
    }
}
