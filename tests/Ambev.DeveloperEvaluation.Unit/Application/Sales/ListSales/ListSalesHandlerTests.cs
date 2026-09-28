using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.ListSales;

/// <summary>
/// Contains unit tests for <see cref="ListSalesHandler"/>. The mapper is the real Sales profile.
/// </summary>
public sealed class ListSalesHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ListSalesHandler _handler;
    private SaleListQuery? _sent;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesHandlerTests"/> class. The repository records the query
    /// it gets and returns an empty page.
    /// </summary>
    public ListSalesHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new ListSalesHandler(_saleRepository, mapper);
        _saleRepository.ListAsync(Arg.Do<SaleListQuery>(query => _sent = query), Arg.Any<CancellationToken>())
            .Returns(new SalePage([], 0));
    }

    /// <summary>
    /// Tests the defaults: page 1, size 10, <c>saleDate desc</c>.
    /// </summary>
    [Fact(DisplayName = "Given no paging or ordering When listing Then it asks the repository for page 1 of 10, newest first")]
    public async Task Given_NoParameters_When_Listing_Then_AsksForFirstPageOfTenNewestFirst()
    {
        // When
        await _handler.Handle(new ListSalesQuery(), CancellationToken.None);

        // Then
        _sent.Should().NotBeNull();
        _sent!.Page.Should().Be(1);
        _sent.Size.Should().Be(10);
        _sent.Sorts.Should().Equal(new SaleSort(SaleSortField.SaleDate, Descending: true));
    }

    /// <summary>
    /// Tests that the requested page, size and parsed order reach the repository.
    /// </summary>
    [Fact(DisplayName = "Given a page, a size and an order When listing Then it asks the repository for exactly those")]
    public async Task Given_PageSizeAndOrder_When_Listing_Then_AsksForExactlyThose()
    {
        // When
        await _handler.Handle(
            new ListSalesQuery { Page = 3, Size = 5, Order = "\"customerName, totalAmount desc\"" }, CancellationToken.None);

        // Then
        _sent.Should().NotBeNull();
        _sent!.Page.Should().Be(3);
        _sent.Size.Should().Be(5);
        _sent.Sorts.Should().Equal(
            new SaleSort(SaleSortField.CustomerName, Descending: false),
            new SaleSort(SaleSortField.TotalAmount, Descending: true));
    }

    /// <summary>
    /// Tests that the page comes back mapped, with the page, size and total count.
    /// </summary>
    [Fact(DisplayName = "Given a page of sales from the repository When listing Then it returns them mapped with the page, size and total")]
    public async Task Given_PageFromRepository_When_Listing_Then_ReturnsItMapped()
    {
        // Given (4 × 4.50 at 10% = 16.20)
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        _saleRepository.ListAsync(Arg.Any<SaleListQuery>(), Arg.Any<CancellationToken>()).Returns(new SalePage([sale], 7));

        // When
        var result = await _handler.Handle(new ListSalesQuery { Page = 2, Size = 5 }, CancellationToken.None);

        // Then
        result.Page.Should().Be(2);
        result.Size.Should().Be(5);
        result.TotalCount.Should().Be(7);
        result.Sales.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            sale.Id,
            sale.SaleNumber,
            TotalAmount = 16.20m,
            Items = new[] { new { sale.Items.Single().Id, TotalAmount = 16.20m } }
        });
    }

    /// <summary>
    /// Tests that without filters the repository gets no criteria.
    /// </summary>
    [Fact(DisplayName = "Given no filters When listing Then the repository gets no criteria")]
    public async Task Given_NoFilters_When_Listing_Then_RepositoryGetsNoCriteria()
    {
        // When
        await _handler.Handle(new ListSalesQuery(), CancellationToken.None);

        // Then
        _sent.Should().NotBeNull();
        _sent!.Filter.Should().Be(SaleListFilter.None);
    }

    /// <summary>
    /// Tests that every filter reaches the repository: text and exact values as sent, dates as inclusive UTC bounds.
    /// </summary>
    [Fact(DisplayName = "Given every filter When listing Then the repository gets them, with the dates as inclusive UTC bounds")]
    public async Task Given_EveryFilter_When_Listing_Then_RepositoryGetsThemWithUtcDateBounds()
    {
        // Given
        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        // When
        await _handler.Handle(new ListSalesQuery
        {
            SaleNumber = "S-*",
            CustomerName = "*maria",
            BranchName = "*50%*",
            CustomerId = customerId,
            BranchId = branchId,
            IsCancelled = false,
            MinSaleDate = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified),
            MaxSaleDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Unspecified),
            MinTotalAmount = 10.00m,
            MaxTotalAmount = 99.99m
        }, CancellationToken.None);

        // Then
        _sent.Should().NotBeNull();
        _sent!.Filter.Should().Be(new SaleListFilter
        {
            SaleNumber = "S-*",
            CustomerName = "*maria",
            BranchName = "*50%*",
            CustomerId = customerId,
            BranchId = branchId,
            IsCancelled = false,
            MinSaleDate = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            MaxSaleDate = new DateTime(2026, 1, 31, 23, 59, 59, 999, 999, DateTimeKind.Utc),
            MinTotalAmount = 10.00m,
            MaxTotalAmount = 99.99m
        });
        _sent.Filter.MinSaleDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        _sent.Filter.MaxSaleDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }
}
