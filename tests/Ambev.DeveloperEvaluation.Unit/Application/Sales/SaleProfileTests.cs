using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

/// <summary>
/// Contains unit tests for <see cref="SaleProfile"/>, which maps the <see cref="Sale"/> aggregate to the flat <see cref="SaleResult"/>.
/// </summary>
public sealed class SaleProfileTests
{
    private readonly MapperConfiguration _configuration = new(config => config.AddProfile<SaleProfile>());

    /// <summary>
    /// Tests that every member of the results has a source, so nothing is left at its default by mistake.
    /// </summary>
    [Fact(DisplayName = "Given the sale profile When validating its configuration Then every result member is mapped")]
    public void Given_SaleProfile_When_ValidatingConfiguration_Then_EveryResultMemberIsMapped()
    {
        // When
        var act = () => _configuration.AssertConfigurationIsValid();

        // Then
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the external identities are flattened into id and name fields (spec decision D12).
    /// </summary>
    [Fact(DisplayName = "Given a sale When mapping it to SaleResult Then customer, branch and product are flattened into id and name fields")]
    public void Given_Sale_When_MappingToSaleResult_Then_IdentitiesAreFlattened()
    {
        // Given
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));

        // When
        var result = _configuration.CreateMapper().Map<SaleResult>(sale);

        // Then
        result.Should().BeEquivalentTo(new
        {
            sale.Id,
            sale.SaleNumber,
            sale.SaleDate,
            CustomerId = sale.Customer.Id,
            CustomerName = sale.Customer.Name,
            BranchId = sale.Branch.Id,
            BranchName = sale.Branch.Name,
            sale.TotalAmount,
            sale.IsCancelled,
            sale.CreatedAt,
            sale.UpdatedAt,
            Items = sale.Items.Select(item => new
            {
                item.Id,
                ProductId = item.Product.Id,
                ProductName = item.Product.Name,
                item.Quantity,
                item.UnitPrice,
                item.DiscountPercentage,
                item.DiscountAmount,
                item.TotalAmount,
                item.IsCancelled
            })
        }, options => options.WithStrictOrdering());
    }
}
