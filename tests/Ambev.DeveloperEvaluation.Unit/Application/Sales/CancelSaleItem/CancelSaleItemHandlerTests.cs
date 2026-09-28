using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.CancelSaleItem;

/// <summary>
/// Contains unit tests for <see cref="CancelSaleItemHandler"/>. The mapper is the real Sales profile.
/// </summary>
public sealed class CancelSaleItemHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly CancelSaleItemHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelSaleItemHandlerTests"/> class.
    /// </summary>
    public CancelSaleItemHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new CancelSaleItemHandler(_saleRepository, _publisher, mapper);
    }

    /// <summary>
    /// Tests that the handler cancels the line, saves the sale and returns it open, with the total over the other line.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active lines When cancelling one Then it saves the sale and returns it open with that line cancelled and the total dropped")]
    public async Task Given_SaleWithTwoActiveLines_When_CancellingOne_Then_SavesAndReturnsTotalDropped()
    {
        // Given
        var sale = GivenLoadedSale();
        var cancelled = sale.Items.First();
        var active = sale.Items.Last();

        // When
        var result = await _handler.Handle(new CancelSaleItemCommand(sale.Id, cancelled.Id), CancellationToken.None);

        // Then
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        result.UpdatedAt.Should().NotBeNull();
        result.Should().BeEquivalentTo(new
        {
            sale.Id,
            IsCancelled = false,
            TotalAmount = 30.00m,
            sale.UpdatedAt,
            Items = new[]
            {
                new { cancelled.Id, TotalAmount = 16.20m, IsCancelled = true },
                new { active.Id, TotalAmount = 30.00m, IsCancelled = false }
            }
        });
    }

    /// <summary>
    /// Tests that an unknown sale is a not-found error, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When cancelling an item Then it throws KeyNotFoundException and saves and publishes nothing")]
    public async Task Given_NoSaleWithId_When_CancellingItem_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var saleId = Guid.NewGuid();
        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(saleId, Guid.NewGuid()), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"The sale with ID {saleId} does not exist");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R9: an item that isn't in the sale is a not-found error, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given an item id that isn't in the sale When cancelling it Then it throws KeyNotFoundException and saves and publishes nothing")]
    public async Task Given_ItemIdNotInSale_When_CancellingIt_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var sale = GivenLoadedSale();
        var itemId = Guid.NewGuid();

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, itemId), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"The item with ID {itemId} does not exist in the sale with ID {sale.Id}");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a domain rejection (rule R9) propagates, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled line When cancelling it again Then DomainException propagates and nothing is saved or published")]
    public async Task Given_CancelledLine_When_CancellingAgain_Then_DomainExceptionAndNothingSaved()
    {
        // Given
        var sale = GivenLoadedSale();
        var item = sale.Items.First();
        sale.CancelItem(item.Id);
        sale.ClearDomainEvents();

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, item.Id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<DomainException>().WithMessage($"Item {item.Id} of sale {sale.SaleNumber} is already cancelled");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14: <see cref="ItemCancelledEvent"/> is published after the save, and then the events are cleared.
    /// </summary>
    [Fact(DisplayName = "Given a sale with two active lines When cancelling one Then it publishes ItemCancelledEvent after the save and clears the events")]
    public async Task Given_SaleWithTwoActiveLines_When_CancellingOne_Then_PublishesItemCancelledEventAfterSave()
    {
        // Given
        var sale = GivenLoadedSale();
        var item = sale.Items.First();
        var published = CapturePublishedEvents();

        // When
        await _handler.Handle(new CancelSaleItemCommand(sale.Id, item.Id), CancellationToken.None);

        // Then
        Received.InOrder(() =>
        {
            _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        });
        published.Should().ContainSingle().Which.Should().Be(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, item.Id, item.Product.Id, sale.UpdatedAt!.Value));
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rules R6 and R14: cancelling the last active line returns the sale cancelled with total 0, and publishes
    /// <see cref="ItemCancelledEvent"/> then <see cref="SaleCancelledEvent"/>, both after the save.
    /// </summary>
    [Fact(DisplayName = "Given a sale with one active line left When cancelling it Then it returns the sale cancelled and publishes ItemCancelledEvent then SaleCancelledEvent after the save")]
    public async Task Given_SaleWithOneActiveLineLeft_When_CancellingIt_Then_PublishesBothEventsInOrderAfterSave()
    {
        // Given
        var sale = GivenLoadedSale();
        sale.CancelItem(sale.Items.First().Id);
        sale.ClearDomainEvents();
        var last = sale.Items.Last();
        var published = CapturePublishedEvents();

        // When
        var result = await _handler.Handle(new CancelSaleItemCommand(sale.Id, last.Id), CancellationToken.None);

        // Then
        Received.InOrder(() =>
        {
            _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        });
        published.Should().Equal(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, last.Id, last.Product.Id, sale.UpdatedAt!.Value),
            new SaleCancelledEvent(sale.Id, sale.SaleNumber, sale.UpdatedAt!.Value));
        result.IsCancelled.Should().BeTrue();
        result.TotalAmount.Should().Be(0m);
    }

    /// <summary>
    /// Tests rule R14: when the save fails, nothing is published.
    /// </summary>
    [Fact(DisplayName = "Given the save throws When cancelling an item Then the exception propagates and nothing is published")]
    public async Task Given_SaveThrows_When_CancellingItem_Then_NothingIsPublished()
    {
        // Given
        var sale = GivenLoadedSale();
        _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The database is unavailable"));

        // When
        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, sale.Items.First().Id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("The database is unavailable");
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Makes the repository return an open sale as it would after loading it: no recorded events, and two lines,
    /// 4 × 4.50 at 10% (16.20) and 3 × 10.00 (30.00).
    /// </summary>
    private Sale GivenLoadedSale()
    {
        var sale = SaleTestData.CreateSale(SaleTestData.GenerateItem(4, 4.50m), SaleTestData.GenerateItem(3, 10.00m));
        sale.ClearDomainEvents();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        return sale;
    }

    /// <summary>
    /// Records every event the handler publishes, in order.
    /// </summary>
    private List<IDomainEvent> CapturePublishedEvents()
    {
        var published = new List<IDomainEvent>();
        _publisher.When(publisher => publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()))
            .Do(call => published.Add(call.Arg<IDomainEvent>()));
        return published;
    }
}
