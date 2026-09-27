using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.GetSale;

/// <summary>
/// Contains unit tests for <see cref="GetSaleHandler"/>.
/// </summary>
public sealed class GetSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly GetSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSaleHandlerTests"/> class.
    /// </summary>
    public GetSaleHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new GetSaleHandler(_saleRepository, mapper);
    }

    /// <summary>
    /// Tests that an existing sale is returned with its lines.
    /// </summary>
    [Fact(DisplayName = "Given an existing sale When getting it by id Then it returns the sale with its items")]
    public async Task Given_ExistingSale_When_GettingById_Then_ReturnsSaleWithItems()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m));
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        // When
        var result = await _handler.Handle(new GetSaleQuery(sale.Id), CancellationToken.None);

        // Then
        result.Should().BeEquivalentTo(new
        {
            sale.Id,
            sale.SaleNumber,
            CustomerName = sale.Customer.Name,
            TotalAmount = 16.20m,
            Items = new[] { new { sale.Items.Single().Id, DiscountPercentage = 10m, DiscountAmount = 1.80m } }
        });
    }

    /// <summary>
    /// Tests that an unknown id is a not-found error, which the API returns as 404 <c>ResourceNotFound</c>.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When getting it by id Then it throws KeyNotFoundException")]
    public async Task Given_NoSaleWithId_When_GettingById_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var id = Guid.NewGuid();
        _saleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(new GetSaleQuery(id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"The sale with ID {id} does not exist");
    }
}
