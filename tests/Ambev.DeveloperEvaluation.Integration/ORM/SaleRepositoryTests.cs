using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
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

    /// <summary>
    /// Tests that <see cref="SaleRepository.UpdateAsync"/> saves the changes made to a loaded sale: a new context
    /// reads it cancelled with its <c>UpdatedAt</c>, and its items and total stay as they were (rule R8).
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale that was cancelled When updating it Then a new context reads it cancelled with its items and total unchanged")]
    public async Task Given_LoadedSaleCancelled_When_Updating_Then_NewContextReadsItCancelled()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var updateContext = _database.CreateContext();
        var repository = new SaleRepository(updateContext);
        var loaded = await repository.GetByIdAsync(sale.Id);
        loaded!.Cancel();

        // When
        await repository.UpdateAsync(loaded);

        // Then
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);
        saved.Should().NotBeNull();
        saved!.IsCancelled.Should().BeTrue();
        saved.UpdatedAt.Should().BeCloseTo(loaded.UpdatedAt!.Value, TimeSpan.FromMicroseconds(1));
        saved.TotalAmount.Should().Be(sale.TotalAmount);
        saved.Items.Should().BeEquivalentTo(sale.Items);
    }

    /// <summary>
    /// Tests that a sale the context doesn't track is refused instead of silently not saved.
    /// </summary>
    [Fact(DisplayName = "Given a sale the context doesn't track When updating it Then it throws InvalidOperationException")]
    public async Task Given_UntrackedSale_When_Updating_Then_ThrowsInvalidOperationException()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale();
        await using var context = _database.CreateContext();

        // When
        var act = () => new SaleRepository(context).UpdateAsync(sale);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a sale loaded with GetByIdAsync can be updated");
    }

    /// <summary>
    /// Tests that cancelling an item of a loaded sale is saved: a new context reads that item cancelled,
    /// the total over the active item, and <c>UpdatedAt</c> set.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale with an item cancelled When updating it Then a new context reads the item cancelled and the total dropped")]
    public async Task Given_LoadedSaleWithItemCancelled_When_Updating_Then_NewContextReadsItemCancelled()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        var cancelledId = sale.Items.First().Id;
        var active = sale.Items.Last();
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var updateContext = _database.CreateContext();
        var repository = new SaleRepository(updateContext);
        var loaded = await repository.GetByIdAsync(sale.Id);
        loaded!.CancelItem(cancelledId);

        // When
        await repository.UpdateAsync(loaded);

        // Then
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);
        saved.Should().NotBeNull();
        saved!.IsCancelled.Should().BeFalse();
        saved.TotalAmount.Should().Be(active.TotalAmount);
        saved.UpdatedAt.Should().BeCloseTo(loaded.UpdatedAt!.Value, TimeSpan.FromMicroseconds(1));
        saved.Items.Should().ContainSingle(item => item.IsCancelled).Which.Id.Should().Be(cancelledId);
    }

    /// <summary>
    /// Tests spec decision D9: two requests load the same sale and each cancel a different item. The first save wins.
    /// The second is refused by the <c>xmin</c> token on the <c>Sales</c> row, which every mutation writes, and rolls
    /// back whole. Without the token both lines would be cancelled while the sale stayed open, breaking rule R6.
    /// </summary>
    [Fact(DisplayName = "Given two contexts that loaded the same sale When each cancels a different item and saves Then the second save throws DbUpdateConcurrencyException and only the first change is stored")]
    public async Task Given_TwoContextsLoadedSameSale_When_EachCancelsDifferentItem_Then_SecondSaveThrowsConcurrencyException()
    {
        // Given
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        var firstItemId = sale.Items.First().Id;
        var secondItem = sale.Items.Last();
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var firstContext = _database.CreateContext();
        await using var secondContext = _database.CreateContext();
        var firstRepository = new SaleRepository(firstContext);
        var secondRepository = new SaleRepository(secondContext);
        var firstLoad = await firstRepository.GetByIdAsync(sale.Id);
        var secondLoad = await secondRepository.GetByIdAsync(sale.Id);
        firstLoad!.CancelItem(firstItemId);
        secondLoad!.CancelItem(secondItem.Id);
        await firstRepository.UpdateAsync(firstLoad);

        // When
        var act = () => secondRepository.UpdateAsync(secondLoad);

        // Then
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);
        saved!.IsCancelled.Should().BeFalse();
        saved.TotalAmount.Should().Be(secondItem.TotalAmount);
        saved.Items.Should().ContainSingle(item => item.IsCancelled).Which.Id.Should().Be(firstItemId);
    }

    /// <summary>
    /// Tests rule R10 against PostgreSQL: an update of a loaded sale is saved whole. The new line is inserted with
    /// the id the domain gave it, which <c>ValueGeneratedNever</c> makes possible. The kept line gets its new
    /// quantity and renamed product, the dropped line is stored cancelled, and the owned customer and branch are
    /// replaced.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale updated with a new header, a changed line, a new line and a dropped line When updating it Then a new context reads every change")]
    public async Task Given_LoadedSaleUpdated_When_Updating_Then_NewContextReadsEveryChange()
    {
        // Given (kept and dropped are the lines as created; the update context loads its own instances)
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        var kept = sale.Items.First();
        var dropped = sale.Items.Last();
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var updateContext = _database.CreateContext();
        var repository = new SaleRepository(updateContext);
        var loaded = await repository.GetByIdAsync(sale.Id);
        var saleDate = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        var customer = new ExternalIdentity(Guid.NewGuid(), "Updated customer");
        var branch = new ExternalIdentity(Guid.NewGuid(), "Updated branch");
        var renamed = new ExternalIdentity(kept.Product.Id, "Renamed product");
        var added = new SaleItemData(new ExternalIdentity(Guid.NewGuid(), "Added product"), 2, 8.00m);
        loaded!.Update(saleDate, customer, branch, [new SaleItemData(renamed, 10, 4.50m), added]);
        var addedId = loaded.Items.Single(item => item.Product.Id == added.Product.Id).Id;

        // When
        await repository.UpdateAsync(loaded);

        // Then (10 × 4.50 at 20% = 36.00, plus 2 × 8.00 = 16.00)
        await using var readContext = _database.CreateContext();
        var saved = await new SaleRepository(readContext).GetByIdAsync(sale.Id);
        saved.Should().NotBeNull();
        saved!.Should().BeEquivalentTo(new
        {
            sale.SaleNumber,
            SaleDate = saleDate,
            Customer = customer,
            Branch = branch,
            TotalAmount = 52.00m,
            IsCancelled = false
        });
        saved!.UpdatedAt.Should().BeCloseTo(loaded.UpdatedAt!.Value, TimeSpan.FromMicroseconds(1));
        saved.Items.Should().BeEquivalentTo(new[]
        {
            new
            {
                kept.Id, Product = renamed, Quantity = 10, UnitPrice = 4.50m,
                DiscountPercentage = 20m, DiscountAmount = 9.00m, TotalAmount = 36.00m, IsCancelled = false
            },
            new
            {
                dropped.Id, dropped.Product, dropped.Quantity, dropped.UnitPrice,
                dropped.DiscountPercentage, dropped.DiscountAmount, dropped.TotalAmount, IsCancelled = true
            },
            new
            {
                Id = addedId, added.Product, added.Quantity, added.UnitPrice,
                DiscountPercentage = 0m, DiscountAmount = 0.00m, TotalAmount = 16.00m, IsCancelled = false
            }
        });
    }
}
