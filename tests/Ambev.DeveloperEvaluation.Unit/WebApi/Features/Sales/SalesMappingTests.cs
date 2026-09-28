using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

/// <summary>
/// Contains unit tests for the WebApi Sales profiles, <see cref="CreateSaleProfile"/>, <see cref="UpdateSaleProfile"/>
/// and <see cref="SaleContractProfile"/>. The configuration holds only the Sales profiles: the API's whole
/// configuration can't be validated, because of the template's duplicate <c>CreateUserRequest</c> map (spec §9.2).
/// </summary>
public sealed class SalesMappingTests
{
    private readonly MapperConfiguration _configuration = new(config =>
    {
        config.AddProfile<CreateSaleProfile>();
        config.AddProfile<UpdateSaleProfile>();
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
    /// Tests that the update request becomes a command with the same header and lines. The id stays empty: the
    /// controller takes it from the route.
    /// </summary>
    [Fact(DisplayName = "Given an update-sale request When mapping it to UpdateSaleCommand Then the header and every line are copied and the id is left empty")]
    public void Given_UpdateSaleRequest_When_MappingToCommand_Then_HeaderAndLinesAreCopied()
    {
        // Given
        var request = new UpdateSaleRequest
        {
            SaleDate = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Maria Silva",
            BranchId = Guid.NewGuid(),
            BranchName = "Filial Norte",
            Items =
            [
                new SaleItemRequest { ProductId = Guid.NewGuid(), ProductName = "Cerveja 350ml", Quantity = 10, UnitPrice = 4.50m },
                new SaleItemRequest { ProductId = Guid.NewGuid(), ProductName = "Refrigerante 2L", Quantity = 2, UnitPrice = 8.00m }
            ]
        };

        // When
        var command = _configuration.CreateMapper().Map<UpdateSaleCommand>(request);

        // Then
        command.Should().BeEquivalentTo(request, options => options.WithStrictOrdering());
        command.Id.Should().BeEmpty();
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

    /// <summary>
    /// Tests that a request without a sale number becomes a command without one, so the handler generates it (rule R12).
    /// </summary>
    [Fact(DisplayName = "Given a create-sale request without a sale number When mapping it to CreateSaleCommand Then the command has no sale number")]
    public void Given_CreateSaleRequestWithoutSaleNumber_When_MappingToCommand_Then_CommandHasNoSaleNumber()
    {
        // When
        var command = _configuration.CreateMapper().Map<CreateSaleCommand>(new CreateSaleRequest());

        // Then
        command.SaleNumber.Should().BeNull();
    }
}
