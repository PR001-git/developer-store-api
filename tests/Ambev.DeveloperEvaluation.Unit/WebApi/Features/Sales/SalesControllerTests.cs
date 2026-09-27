using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

/// <summary>
/// Contains unit tests for <see cref="SalesController"/>. The mapper holds the real WebApi Sales profiles,
/// and each body is serialized the way MVC does, so the tests check the exact JSON a client gets.
/// </summary>
public sealed class SalesControllerTests
{
    /// <summary>
    /// The sale of the spec's example (§7.2), as the body of every success response carries it.
    /// </summary>
    private const string ExampleSaleJson =
        """{"id":"7f9c2a44-5555-4d1e-8a3b-000000000010","saleNumber":"S-000123","saleDate":"2026-09-24T14:30:00Z","customerId":"3f2b8c1e-1111-4a5b-9c2d-000000000001","customerName":"Maria Silva","branchId":"3f2b8c1e-2222-4a5b-9c2d-000000000002","branchName":"Filial Centro","totalAmount":20.25,"isCancelled":false,"createdAt":"2026-09-24T14:31:02Z","updatedAt":null,"items":[{"id":"7f9c2a44-6666-4d1e-8a3b-000000000011","productId":"3f2b8c1e-3333-4a5b-9c2d-000000000003","productName":"Cerveja 350ml","quantity":5,"unitPrice":4.50,"discountPercentage":10,"discountAmount":2.25,"totalAmount":20.25,"isCancelled":false}]}""";

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly SalesController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesControllerTests"/> class.
    /// </summary>
    public SalesControllerTests()
    {
        var mapper = new MapperConfiguration(config =>
        {
            config.AddProfile<CreateSaleProfile>();
            config.AddProfile<SaleContractProfile>();
        }).CreateMapper();
        _controller = new SalesController(_mediator, mapper);
    }

    /// <summary>
    /// Tests that creating a sale sends the command built from the request, and returns 201 pointing at
    /// <c>GetSale</c> with the sale in the envelope, built once.
    /// </summary>
    [Fact(DisplayName = "Given a create-sale request When creating the sale Then it sends the command and returns 201 at GetSale with the sale")]
    public async Task Given_CreateSaleRequest_When_CreatingSale_Then_Returns201AtGetSaleWithSale()
    {
        // Given
        var request = new CreateSaleRequest
        {
            SaleNumber = "S-000123",
            SaleDate = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc),
            CustomerId = Guid.Parse("3f2b8c1e-1111-4a5b-9c2d-000000000001"),
            CustomerName = "Maria Silva",
            BranchId = Guid.Parse("3f2b8c1e-2222-4a5b-9c2d-000000000002"),
            BranchName = "Filial Centro",
            Items = [new SaleItemRequest { ProductId = Guid.Parse("3f2b8c1e-3333-4a5b-9c2d-000000000003"), ProductName = "Cerveja 350ml", Quantity = 5, UnitPrice = 4.50m }]
        };
        CreateSaleCommand? sent = null;
        _mediator.Send(Arg.Do<CreateSaleCommand>(command => sent = command), Arg.Any<CancellationToken>())
            .Returns(ExampleResult());

        // When
        var response = await _controller.CreateSale(request, CancellationToken.None);

        // Then
        sent.Should().BeEquivalentTo(request);
        var created = response.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(SalesController.GetSale));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(ExampleResult().Id);
        MvcJson.Serialize(created.Value).Should().Be(
            $$"""{"success":true,"message":"Sale created successfully","data":{{ExampleSaleJson}}}""");
    }

    /// <summary>
    /// Tests that getting a sale sends the query for the route id and returns 200 with the sale in the envelope.
    /// </summary>
    [Fact(DisplayName = "Given a sale id When getting the sale Then it sends the query and returns 200 with the sale")]
    public async Task Given_SaleId_When_GettingSale_Then_Returns200WithSale()
    {
        // Given
        var result = ExampleResult();
        _mediator.Send(new GetSaleQuery(result.Id), Arg.Any<CancellationToken>()).Returns(result);

        // When
        var response = await _controller.GetSale(result.Id, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            $$"""{"success":true,"message":"Sale retrieved successfully","data":{{ExampleSaleJson}}}""");
    }

    private static SaleResult ExampleResult() => new()
    {
        Id = Guid.Parse("7f9c2a44-5555-4d1e-8a3b-000000000010"),
        SaleNumber = "S-000123",
        SaleDate = new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Utc),
        CustomerId = Guid.Parse("3f2b8c1e-1111-4a5b-9c2d-000000000001"),
        CustomerName = "Maria Silva",
        BranchId = Guid.Parse("3f2b8c1e-2222-4a5b-9c2d-000000000002"),
        BranchName = "Filial Centro",
        TotalAmount = 20.25m,
        IsCancelled = false,
        CreatedAt = new DateTime(2026, 9, 24, 14, 31, 2, DateTimeKind.Utc),
        UpdatedAt = null,
        Items =
        [
            new SaleItemResult
            {
                Id = Guid.Parse("7f9c2a44-6666-4d1e-8a3b-000000000011"),
                ProductId = Guid.Parse("3f2b8c1e-3333-4a5b-9c2d-000000000003"),
                ProductName = "Cerveja 350ml",
                Quantity = 5,
                UnitPrice = 4.50m,
                DiscountPercentage = 10m,
                DiscountAmount = 2.25m,
                TotalAmount = 20.25m,
                IsCancelled = false
            }
        ]
    };
}
