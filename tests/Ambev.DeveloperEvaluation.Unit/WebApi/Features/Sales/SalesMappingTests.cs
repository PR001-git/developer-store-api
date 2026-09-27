using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

/// <summary>
/// Contains unit tests for the WebApi Sales profiles, <see cref="CreateSaleProfile"/> and <see cref="SaleContractProfile"/>.
/// The configuration holds only the Sales profiles: the API's whole configuration can't be validated,
/// because of the template's duplicate <c>CreateUserRequest</c> map (spec §9.2).
/// </summary>
public sealed class SalesMappingTests
{
    private readonly MapperConfiguration _configuration = new(config =>
    {
        config.AddProfile<CreateSaleProfile>();
        config.AddProfile<SaleContractProfile>();
    });

    /// <summary>
    /// Tests that every member of the commands and responses has a source.
    /// </summary>
    [Fact(DisplayName = "Given the WebApi Sales profiles When validating their configuration Then every command and response member is mapped")]
    public void Given_WebApiSalesProfiles_When_ValidatingConfiguration_Then_EveryMemberIsMapped()
    {
        // When
        var act = () => _configuration.AssertConfigurationIsValid();

        // Then
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the create request becomes a command with the same header and lines.
    /// </summary>
    [Fact(DisplayName = "Given a create-sale request When mapping it to CreateSaleCommand Then the header and every line are copied")]
    public void Given_CreateSaleRequest_When_MappingToCommand_Then_HeaderAndLinesAreCopied()
    {
        // Given
        var request = new CreateSaleRequest
        {
            SaleNumber = "S-000123",
            SaleDate = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Maria Silva",
            BranchId = Guid.NewGuid(),
            BranchName = "Filial Centro",
            Items =
            [
                new SaleItemRequest { ProductId = Guid.NewGuid(), ProductName = "Cerveja 350ml", Quantity = 5, UnitPrice = 4.50m },
                new SaleItemRequest { ProductId = Guid.NewGuid(), ProductName = "Refrigerante 2L", Quantity = 1, UnitPrice = 9.90m }
            ]
        };

        // When
        var command = _configuration.CreateMapper().Map<CreateSaleCommand>(request);

        // Then
        command.Should().BeEquivalentTo(request, options => options.WithStrictOrdering());
    }

    /// <summary>
    /// Tests that the response copies every field of the result, lines included.
    /// </summary>
    [Fact(DisplayName = "Given a SaleResult When mapping it to SaleResponse Then every field and line is copied")]
    public void Given_SaleResult_When_MappingToSaleResponse_Then_EveryFieldIsCopied()
    {
        // Given
        var result = new SaleResult
        {
            Id = Guid.NewGuid(),
            SaleNumber = "S-000123",
            SaleDate = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Maria Silva",
            BranchId = Guid.NewGuid(),
            BranchName = "Filial Centro",
            TotalAmount = 20.25m,
            CreatedAt = new DateTime(2026, 9, 24, 14, 31, 2, DateTimeKind.Utc),
            Items =
            [
                new SaleItemResult
                {
                    Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Cerveja 350ml", Quantity = 5,
                    UnitPrice = 4.50m, DiscountPercentage = 10m, DiscountAmount = 2.25m, TotalAmount = 20.25m
                }
            ]
        };

        // When
        var response = _configuration.CreateMapper().Map<SaleResponse>(result);

        // Then
        response.Should().BeEquivalentTo(result);
    }
}
