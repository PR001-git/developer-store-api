using System.Globalization;
using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

/// <summary>
/// Contains integration tests for the filters of the sales list: <see cref="ListSalesHandler"/> over a real
/// <see cref="SaleRepository"/> on PostgreSQL. Each test starts from empty tables and these six sales, one line each
/// (quantity 1, so the total is the unit price), each with its own customer and branch ids:
/// <code>
/// sale  saleNumber  saleDate (UTC)              customerName   branchName     total  cancelled
/// A     S-1         2026-01-01 00:00:00         Maria Silva    Filial Centro  10.00  no
/// B     S-2         2026-01-15 12:00:00         Mariana Souza  Loja 50% Off   20.00  yes
/// C     S-3         2026-01-31 00:00:00         Ana Maria      Loja 500 Off   30.00  no
/// D     S-4         2026-01-31 23:59:59.999999  Joana Maria    Loja*Off       40.00  no
/// E     S_5         2026-02-01 00:00:00         Bruno Costa    Filial A\B     50.00  yes
/// F     S-5         2026-01-31 02:00:00         Carla Dias     Filial AB      60.00  no
/// </code>
/// Matches are compared as sorted letters, so the text collation plays no part. Every test also checks the count.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleListFilterTests : IAsyncLifetime
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();

    private readonly PostgreSqlFixture _database;
    private readonly Dictionary<char, Sale> _sales = new()
    {
        ['A'] = NewSale("S-1", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Maria Silva", "Filial Centro", 10.00m, cancelled: false),
        ['B'] = NewSale("S-2", new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), "Mariana Souza", "Loja 50% Off", 20.00m, cancelled: true),
        ['C'] = NewSale("S-3", new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), "Ana Maria", "Loja 500 Off", 30.00m, cancelled: false),
        ['D'] = NewSale("S-4", new DateTime(2026, 1, 31, 23, 59, 59, 999, 999, DateTimeKind.Utc), "Joana Maria", "Loja*Off", 40.00m, cancelled: false),
        ['E'] = NewSale("S_5", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), "Bruno Costa", @"Filial A\B", 50.00m, cancelled: true),
        ['F'] = NewSale("S-5", new DateTime(2026, 1, 31, 2, 0, 0, DateTimeKind.Utc), "Carla Dias", "Filial AB", 60.00m, cancelled: false)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleListFilterTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleListFilterTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Empties the tables, then saves the six sales, so every count is exact.
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
    /// Tests the wildcard forms on the sale number, case-insensitively, with <c>_</c> matched literally.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by sale number Then only the matching sales count")]
    [InlineData("s-*", "ABCDF")]
    [InlineData("*5", "EF")]
    [InlineData("*_*", "E")]
    [InlineData("S_5", "E")]
    [InlineData("s-1", "A")]
    public async Task Given_SixSales_When_FilteringBySaleNumber_Then_OnlyMatchesCount(string saleNumber, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { SaleNumber = saleNumber });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the four wildcard forms on the customer name, case-insensitively. No <c>*</c> means equals, not contains.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by customer name Then starts with, ends with, contains and equals match")]
    [InlineData("mar*", "AB")]
    [InlineData("*MARIA", "CD")]
    [InlineData("*ana*", "BCD")]
    [InlineData("ana maria", "C")]
    [InlineData("maria", "")]
    public async Task Given_SixSales_When_FilteringByCustomerName_Then_OnlyMatchesCount(string customerName, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { CustomerName = customerName });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the branch name, with <c>%</c>, an inner <c>*</c> and <c>\</c> matched literally.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by branch name Then %, an inner * and a backslash match themselves")]
    [InlineData("loja*", "BCD")]
    [InlineData("*b", "EF")]
    [InlineData("*50%*", "B")]
    [InlineData("loja*off", "D")]
    [InlineData(@"filial a\b", "E")]
    public async Task Given_SixSales_When_FilteringByBranchName_Then_OnlyMatchesCount(string branchName, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { BranchName = branchName });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the exact customer-id match.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When filtering by C's customer id Then only C counts")]
    public async Task Given_SixSales_When_FilteringByCustomerId_Then_OnlyThatCustomersSaleCounts()
    {
        // When
        var result = await ListAsync(new ListSalesQuery { CustomerId = _sales['C'].Customer.Id });

        // Then
        Matches(result).Should().Be("C");
        result.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests the exact branch-id match.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When filtering by E's branch id Then only E counts")]
    public async Task Given_SixSales_When_FilteringByBranchId_Then_OnlyThatBranchsSaleCounts()
    {
        // When
        var result = await ListAsync(new ListSalesQuery { BranchId = _sales['E'].Branch.Id });

        // Then
        Matches(result).Should().Be("E");
        result.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that an id no sale has matches nothing.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When filtering by an unknown customer id Then no sale counts")]
    public async Task Given_SixSales_When_FilteringByUnknownCustomerId_Then_NoSaleCounts()
    {
        // When
        var result = await ListAsync(new ListSalesQuery { CustomerId = Guid.NewGuid() });

        // Then
        result.Sales.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Tests the cancelled-status match both ways.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by isCancelled Then only sales with that status count")]
    [InlineData(true, "BE")]
    [InlineData(false, "ACDF")]
    public async Task Given_SixSales_When_FilteringByIsCancelled_Then_OnlyThatStatusCounts(bool isCancelled, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { IsCancelled = isCancelled });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the sale-date range: inclusive bounds, a value without an offset read as UTC, offsets converted,
    /// and a max at exactly midnight UTC covering its whole day (D, at its last microsecond).
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by sale date Then the bounds are inclusive UTC and a midnight max covers its day")]
    [InlineData("2026-01-15T12:00:00Z", null, "BCDEF")]
    [InlineData(null, "2026-01-15T12:00:00Z", "AB")]
    [InlineData(null, "2026-01-31", "ABCDF")]
    [InlineData(null, "2026-01-31T00:00:00Z", "ABCDF")]
    [InlineData(null, "2026-01-31T12:00:00", "ABCF")]
    [InlineData(null, "2026-01-30T23:00:00-03:00", "ABCF")]
    [InlineData(null, "2026-01-30T22:59:59-03:00", "ABC")]
    [InlineData(null, "2026-01-31T00:00:00-03:00", "ABCF")]
    [InlineData("2026-01-31T00:00:00-03:00", null, "DE")]
    [InlineData("2026-01-31", "2026-01-31", "CDF")]
    public async Task Given_SixSales_When_FilteringBySaleDate_Then_InclusiveUtcBounds(string? min, string? max, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { MinSaleDate = QueryDate(min), MaxSaleDate = QueryDate(max) });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests the total-amount range, both bounds inclusive.
    /// </summary>
    [Theory(DisplayName = "Given the six sales When filtering by total amount Then both bounds are inclusive")]
    [InlineData("30", null, "CDEF")]
    [InlineData(null, "30", "ABC")]
    [InlineData("20", "40", "BCD")]
    [InlineData("30", "30", "C")]
    public async Task Given_SixSales_When_FilteringByTotalAmount_Then_InclusiveBounds(string? min, string? max, string expected)
    {
        // When
        var result = await ListAsync(new ListSalesQuery { MinTotalAmount = Amount(min), MaxTotalAmount = Amount(max) });

        // Then
        Matches(result).Should().Be(expected);
        result.TotalCount.Should().Be(expected.Length);
    }

    /// <summary>
    /// Tests that filters combine: <c>*ana*</c> and <c>loja*</c> keep B, C and D; open drops B; 35 or more drops C;
    /// the whole of 2026-01-31 keeps D.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When combining text, status, amount and whole-day date filters Then only the sale matching all of them counts")]
    public async Task Given_SixSales_When_CombiningFilters_Then_OnlySaleMatchingAllCounts()
    {
        // When
        var result = await ListAsync(new ListSalesQuery
        {
            CustomerName = "*ana*",
            BranchName = "loja*",
            IsCancelled = false,
            MinTotalAmount = 35m,
            MaxSaleDate = QueryDate("2026-01-31")
        });

        // Then
        Matches(result).Should().Be("D");
        result.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that a filter combines with paging and ordering: the open sales by highest total are F, D, C, A.
    /// </summary>
    [Fact(DisplayName = "Given the six sales When reading page 2 of 2 of the open sales by highest total Then it holds C then A and the count is 4")]
    public async Task Given_SixSales_When_ReadingSecondPageOfOpenSalesByTotal_Then_HoldsCThenAWithCountFour()
    {
        // When
        var result = await ListAsync(new ListSalesQuery { IsCancelled = false, Order = "totalAmount desc", Page = 2, Size = 2 });

        // Then
        Letters(result).Should().Be("CA");
        result.TotalCount.Should().Be(4);
    }

    private static Sale NewSale(string saleNumber, DateTime saleDate, string customerName, string branchName, decimal total, bool cancelled)
    {
        var sale = Sale.Create(
            saleNumber,
            saleDate,
            new ExternalIdentity(Guid.NewGuid(), customerName),
            new ExternalIdentity(Guid.NewGuid(), branchName),
            [new SaleItemData(new ExternalIdentity(Guid.NewGuid(), $"Product {saleNumber}"), 1, total)]);
        if (cancelled)
            sale.Cancel();
        return sale;
    }

    // ASP.NET Core's DateTimeModelBinder parses a query value this way: no offset gives Unspecified, an offset or Z gives UTC.
    private static DateTime? QueryDate(string? value) =>
        value is null
            ? null
            : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces);

    private static decimal? Amount(string? value) =>
        value is null ? null : decimal.Parse(value, CultureInfo.InvariantCulture);

    private string Letters(ListSalesResult result) =>
        string.Concat(result.Sales.Select(sale => _sales.Single(pair => pair.Value.Id == sale.Id).Key));

    private string Matches(ListSalesResult result) => string.Concat(Letters(result).Order());

    private async Task<ListSalesResult> ListAsync(ListSalesQuery query)
    {
        await using var context = _database.CreateContext();
        return await new ListSalesHandler(new SaleRepository(context), Mapper).Handle(query, CancellationToken.None);
    }
}
