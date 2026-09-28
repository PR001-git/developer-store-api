using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Ambev.DeveloperEvaluation.ORM.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for soft delete against PostgreSQL (spec §8.2). The global query filter hides a deleted
/// sale from <see cref="SaleRepository.GetByIdAsync"/> and <see cref="SaleRepository.ListAsync"/>, its rows stay, and
/// its number stays taken (rule R12). Each test starts from empty tables and a restarted <c>sale_number_seq</c>.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleSoftDeleteTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleSoftDeleteTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleSoftDeleteTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Empties the tables and restarts the sequence, so counts and generated numbers are exact.
    /// </summary>
    public Task InitializeAsync() => _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Tests rule R11: the filter hides a deleted sale from <see cref="SaleRepository.GetByIdAsync"/>, while its row
    /// and its items stay in the database.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When loading it by id Then it returns null, while IgnoreQueryFilters still finds the row and its items")]
    public async Task Given_DeletedSale_When_LoadingById_Then_ReturnsNullButRowAndItemsRemain()
    {
        // Given
        var deleted = await SaveDeletedSaleAsync(SaleTestData.GenerateValidSale(itemCount: 2));

        // When
        await using var context = _database.CreateContext();
        var loaded = await new SaleRepository(context).GetByIdAsync(deleted.Id);

        // Then
        loaded.Should().BeNull();
        var row = await context.Sales
            .IgnoreQueryFilters()
            .Include(sale => sale.Items)
            .SingleAsync(sale => sale.Id == deleted.Id);
        row.IsDeleted.Should().BeTrue();
        row.DeletedAt.Should().BeCloseTo(deleted.DeletedAt!.Value, TimeSpan.FromMicroseconds(1));
        row.Items.Select(item => item.Id).Should().BeEquivalentTo(deleted.Items.Select(item => item.Id));
    }

    /// <summary>
    /// Tests rule R11: a deleted sale is neither on the page nor in the count.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale and a kept one When listing Then only the kept sale is on the page and in the count")]
    public async Task Given_DeletedAndKeptSales_When_Listing_Then_OnlyKeptSaleIsListedAndCounted()
    {
        // Given
        var kept = SaleTestData.GenerateValidSale();
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(kept);
        }

        await SaveDeletedSaleAsync(SaleTestData.GenerateValidSale());

        // When
        await using var context = _database.CreateContext();
        var page = await new SaleRepository(context).ListAsync(new SaleListQuery(1, 10, []));

        // Then
        page.Sales.Select(sale => sale.Id).Should().Equal(kept.Id);
        page.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests rule R12: a deleted sale's number still counts as taken.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale When checking its number Then the number still exists")]
    public async Task Given_DeletedSale_When_CheckingItsNumber_Then_NumberStillExists()
    {
        // Given
        var deleted = await SaveDeletedSaleAsync(SaleTestData.GenerateValidSale());

        // When
        await using var context = _database.CreateContext();
        var exists = await new SaleRepository(context).ExistsBySaleNumberAsync(deleted.SaleNumber);

        // Then
        exists.Should().BeTrue();
    }

    /// <summary>
    /// Tests step 3 of spec §8.3 with a deleted sale: the generator skips its number instead of reusing it.
    /// </summary>
    [Fact(DisplayName = "Given a deleted sale numbered S-000001 When generating a number Then it skips S-000001 and returns S-000002")]
    public async Task Given_DeletedSaleHoldsFirstNumber_When_Generating_Then_SkipsIt()
    {
        // Given
        await SaveDeletedSaleAsync(SaleTestData.GenerateValidSale(saleNumber: "S-000001"));

        // When
        await using var context = _database.CreateContext();
        var number = await new SaleNumberGenerator(context, new SaleRepository(context)).NextAsync();

        // Then
        number.Should().Be("S-000002");
    }

    /// <summary>
    /// Saves the sale, then deletes it the way the delete handler does: load it, <see cref="Sale.Delete"/>, update it.
    /// </summary>
    /// <param name="sale">A new sale.</param>
    /// <returns>The deleted sale, as it was saved.</returns>
    private async Task<Sale> SaveDeletedSaleAsync(Sale sale)
    {
        await using (var createContext = _database.CreateContext())
        {
            await new SaleRepository(createContext).CreateAsync(sale);
        }

        await using var deleteContext = _database.CreateContext();
        var repository = new SaleRepository(deleteContext);
        var loaded = await repository.GetByIdAsync(sale.Id);
        loaded!.Delete();
        await repository.UpdateAsync(loaded);
        return loaded;
    }
}
