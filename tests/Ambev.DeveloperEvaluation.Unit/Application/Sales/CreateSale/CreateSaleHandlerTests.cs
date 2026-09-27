using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using AutoMapper;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CreateSale;

/// <summary>
/// Contains unit tests for <see cref="CreateSaleHandler"/>. The mapper is the real Sales profile, so the result
/// shows what the domain computed.
/// </summary>
public sealed class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly CreateSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSaleHandlerTests"/> class.
    /// </summary>
    public CreateSaleHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new CreateSaleHandler(_saleRepository, _publisher, mapper);
    }

    /// <summary>
    /// Tests that the handler saves the sale the domain built from the command and returns it,
    /// with the discount the domain computed (5 × 4.45 gets 10%, 2.23).
    /// </summary>
    [Fact(DisplayName = "Given a valid command When handling Then it saves the sale built from it and returns it with the domain's discount")]
    public async Task Given_ValidCommand_When_Handling_Then_SavesSaleAndReturnsItWithDiscount()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand(itemCount: 1);
        command.Items[0].Quantity = 5;
        command.Items[0].UnitPrice = 4.45m;
        Sale? saved = null;
        _saleRepository.When(repository => repository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<Sale>());

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        saved.Should().NotBeNull();
        result.Should().BeEquivalentTo(new
        {
            saved!.Id,
            command.SaleNumber,
            command.SaleDate,
            command.CustomerId,
            command.CustomerName,
            command.BranchId,
            command.BranchName,
            TotalAmount = 20.02m,
            IsCancelled = false,
            Items = new[]
            {
                new
                {
                    command.Items[0].ProductId,
                    command.Items[0].ProductName,
                    Quantity = 5,
                    UnitPrice = 4.45m,
                    DiscountPercentage = 10m,
                    DiscountAmount = 2.23m,
                    TotalAmount = 20.02m
                }
            }
        });
    }

    /// <summary>
    /// Tests rule R14 and spec decision D8: the recorded event is published after the save, and then cleared.
    /// Call order is the behaviour under test here.
    /// </summary>
    [Fact(DisplayName = "Given a valid command When handling Then it publishes SaleCreatedEvent after the save and clears the events")]
    public async Task Given_ValidCommand_When_Handling_Then_PublishesSaleCreatedEventAfterSave()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        Sale? saved = null;
        _saleRepository.When(repository => repository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>()))
            .Do(call => saved = call.Arg<Sale>());
        var published = new List<IDomainEvent>();
        _publisher.When(publisher => publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()))
            .Do(call => published.Add(call.Arg<IDomainEvent>()));

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        Received.InOrder(() =>
        {
            _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        });
        published.Should().ContainSingle().Which.Should().BeOfType<SaleCreatedEvent>()
            .Which.SaleId.Should().Be(saved!.Id);
        saved.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14: when the save fails, nothing is published.
    /// </summary>
    [Fact(DisplayName = "Given the save throws When handling Then the exception propagates and nothing is published")]
    public async Task Given_SaveThrows_When_Handling_Then_NothingIsPublished()
    {
        // Given
        var command = CreateSaleCommandTestData.GenerateValidCommand();
        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The database is unavailable"));

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The database is unavailable");
        _publisher.ReceivedCalls().Should().BeEmpty();
    }
}
