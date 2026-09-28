using System.Data.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for <see cref="SaleRepository.ListAsync"/> against PostgreSQL. Each test starts from
/// empty tables and these four sales, one line each (quantity 1, so the total is the unit price):
/// <code>
/// sale  number  saleDate    customerName  branchName    totalAmount  isCancelled
/// A     S-A     2026-01-03  Carla Dias    Filial Beta   10.00        true
/// B     S-B     2026-01-01  Ana Lima      Filial Delta  40.00        false
/// C     S-C     2026-01-04  Bruno Souza   Filial Alfa   20.00        false
/// D     S-D     2026-01-02  Diego Rocha   Filial Gama   30.00        true
/// </code>
/// Every field orders them differently. Names start with distinct capitals, so the collation can't change the order.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleRepositoryListTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _database;
    private readonly Dictionary<char, Sale> _sales = new()
    {
        ['A'] = NewSale('A', 3, "Carla Dias", "Filial Beta", 10.00m, cancelled: true),
        ['B'] = NewSale('B', 1, "Ana Lima", "Filial Delta", 40.00m, cancelled: false),
        ['C'] = NewSale('C', 4, "Bruno Souza", "Filial Alfa", 20.00m, cancelled: false),
        ['D'] = NewSale('D', 2, "Diego Rocha", "Filial Gama", 30.00m, cancelled: true)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRepositoryListTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleRepositoryListTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Empties the tables, then saves the four sales, so every count is exact.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        await using var context = _database.CreateContext();
        var repository = new SaleRepository(context);
        foreach (var sale in _sales.Values)
            await repository.CreateAsync(sale);
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Tests every sortable field except <c>isCancelled</c> (which has ties) in both directions.
    /// </summary>
    [Theory(DisplayName = "Given the four sales When ordering by one field Then they come in that field's order")]
    [InlineData(SaleSortField.SaleNumber, false, "ABCD")]
    [InlineData(SaleSortField.SaleNumber, true, "DCBA")]
    [InlineData(SaleSortField.SaleDate, false, "BDAC")]
    [InlineData(SaleSortField.SaleDate, true, "CADB")]
    [InlineData(SaleSortField.CustomerName, false, "BCAD")]
    [InlineData(SaleSortField.CustomerName, true, "DACB")]
    [InlineData(SaleSortField.BranchName, false, "CABD")]
    [InlineData(SaleSortField.BranchName, true, "DBAC")]
    [InlineData(SaleSortField.TotalAmount, false, "ACDB")]
    [InlineData(SaleSortField.TotalAmount, true, "BDCA")]
    public async Task Given_FourSales_When_OrderingByOneField_Then_TheyComeInThatOrder(SaleSortField field, bool descending, string expected)
    {
        // When
        var page = await ListAsync(1, 10, new SaleSort(field, descending));

        // Then
        Letters(page).Should().Be(expected);
    }

    /// <summary>
    /// Tests <c>isCancelled</c> in both directions with open and cancelled sales, each group ordered by the second field.
    /// </summary>
    [Theory(DisplayName = "Given open and cancelled sales When ordering by isCancelled then totalAmount Then each group follows its total")]
    [InlineData(false, true, "BCDA")]
    [InlineData(true, false, "ADCB")]
    public async Task Given_OpenAndCancelledSales_When_OrderingByIsCancelledThenTotal_Then_EachGroupFollowsItsTotal(
        bool cancelledDescending, bool totalDescending, string expected)
    {
        // When
        var page = await ListAsync(1, 10,
            new SaleSort(SaleSortField.IsCancelled, cancelledDescending),
            new SaleSort(SaleSortField.TotalAmount, totalDescending));

        // Then
        Letters(page).Should().Be(expected);
    }

    /// <summary>
    /// Tests the tie-breaker: sales with equal keys follow their id, so reading one sale per page returns each once.
    /// </summary>
    [Theory(DisplayName = "Given equal isCancelled keys When reading one sale per page Then the id breaks the ties across pages")]
    [InlineData(false, "BC", "AD")]
    [InlineData(true, "AD", "BC")]
    public async Task Given_EqualKeys_When_ReadingOneSalePerPage_Then_IdBreaksTiesAcrossPages(
        bool descending, string firstGroup, string secondGroup)
    {
        // Given
        var expected = InIdOrder(firstGroup).Concat(InIdOrder(secondGroup));

        // When
        var read = new List<Guid>();
        for (var number = 1; number <= 4; number++)
            read.AddRange((await ListAsync(number, 1, new SaleSort(SaleSortField.IsCancelled, descending))).Sales.Select(sale => sale.Id));

        // Then
        read.Should().Equal(expected);
    }

    /// <summary>
    /// Tests that with no sort the id alone orders the sales.
    /// </summary>
    [Fact(DisplayName = "Given the four sales When listing with no sort Then they come in id order")]
    public async Task Given_FourSales_When_ListingWithNoSort_Then_TheyComeInIdOrder()
    {
        // When
        var page = await ListAsync(1, 10);

        // Then
        page.Sales.Select(sale => sale.Id).Should().Equal(InIdOrder("ABCD"));
    }

    /// <summary>
    /// Tests paging: each page holds its slice, a page past the end is empty, and the total is always every sale.
    /// </summary>
    [Theory(DisplayName = "Given the four sales When reading a page by sale number Then it holds its slice and the total is 4")]
    [InlineData(1, 3, "ABC")]
    [InlineData(2, 3, "D")]
    [InlineData(3, 3, "")]
    [InlineData(int.MaxValue, 100, "")]
    public async Task Given_FourSales_When_ReadingPage_Then_ItHoldsItsSliceAndTotalIsFour(int number, int size, string expected)
    {
        // When
        var page = await ListAsync(number, size, new SaleSort(SaleSortField.SaleNumber, Descending: false));

        // Then
        Letters(page).Should().Be(expected);
        page.TotalCount.Should().Be(4);
    }

    /// <summary>
    /// Tests that each listed sale comes with its items and the values it was saved with.
    /// Timestamps are left out: ticket 05's round-trip test covers their precision.
    /// </summary>
    [Fact(DisplayName = "Given the four sales When listing them Then each comes with its items and saved values")]
    public async Task Given_FourSales_When_Listing_Then_EachComesWithItsItems()
    {
        // When
        var page = await ListAsync(1, 10, new SaleSort(SaleSortField.SaleNumber, Descending: false));

        // Then
        page.Sales.Should().BeEquivalentTo(_sales.Values, options => options
            .Excluding(sale => sale.DomainEvents)
            .Excluding(sale => sale.CreatedAt)
            .Excluding(sale => sale.UpdatedAt));
    }

    /// <summary>
    /// Tests the query shape: a count, the page with LIMIT and OFFSET, then the items in a separate query
    /// (split query), with nothing tracked.
    /// </summary>
    [Fact(DisplayName = "Given the four sales When reading page 2 of 2 Then it counts, reads the page, reads its items separately and tracks nothing")]
    public async Task Given_FourSales_When_ReadingPage_Then_UsesSplitQueryWithoutTracking()
    {
        // Given
        var recorder = new CommandRecorder();
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(_database.ConnectionString)
            .AddInterceptors(recorder)
            .Options;
        await using var context = new DefaultContext(options);

        // When
        var page = await new SaleRepository(context).ListAsync(
            new SaleListQuery(2, 2, [new SaleSort(SaleSortField.SaleNumber, Descending: false)]));

        // Then
        Letters(page).Should().Be("CD");
        recorder.CommandTexts.Should().HaveCount(3);
        recorder.CommandTexts[0].Should().Contain("count(*)");
        recorder.CommandTexts[1].Should().Contain("LIMIT").And.Contain("OFFSET").And.NotContain("\"SaleItems\"");
        recorder.CommandTexts[2].Should().Contain("\"SaleItems\"");
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    private static Sale NewSale(char letter, int day, string customerName, string branchName, decimal total, bool cancelled)
    {
        var sale = Sale.Create(
            $"S-{letter}",
            new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc),
            new ExternalIdentity(Guid.NewGuid(), customerName),
            new ExternalIdentity(Guid.NewGuid(), branchName),
            [new SaleItemData(new ExternalIdentity(Guid.NewGuid(), $"Product {letter}"), 1, total)]);
        if (cancelled)
            sale.Cancel();
        return sale;
    }

    private static string Letters(SalePage page) => string.Concat(page.Sales.Select(sale => sale.SaleNumber[^1]));

    // PostgreSQL orders uuid values like their lower-case text.
    private IEnumerable<Guid> InIdOrder(string letters) =>
        letters.Select(letter => _sales[letter].Id).OrderBy(id => id.ToString(), StringComparer.Ordinal);

    private async Task<SalePage> ListAsync(int number, int size, params SaleSort[] sorts)
    {
        await using var context = _database.CreateContext();
        return await new SaleRepository(context).ListAsync(new SaleListQuery(number, size, sorts));
    }

    /// <summary>
    /// Records the text of every command EF Core sends, so a test can see the round trips.
    /// </summary>
    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<string> CommandTexts { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CommandTexts.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            CommandTexts.Add(command.CommandText);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
