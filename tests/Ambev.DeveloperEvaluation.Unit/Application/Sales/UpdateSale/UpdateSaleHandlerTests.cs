using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.UpdateSale;

/// <summary>
/// Contains unit tests for <see cref="UpdateSaleHandler"/>. The mapper is the real Sales profile.
/// </summary>
public sealed class UpdateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly UpdateSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleHandlerTests"/> class.
    /// </summary>
    public UpdateSaleHandlerTests()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<SaleProfile>()).CreateMapper();
        _handler = new UpdateSaleHandler(_saleRepository, _publisher, mapper);
    }

    /// <summary>
    /// Tests that the handler updates the loaded sale with the command's header and lines, saves it, and returns it
    /// reconciled: the changed line at its new tier, the new line, and the dropped line cancelled.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale When updating it with a new header, a changed line and a new line Then it saves the sale and returns it with the dropped line cancelled")]
    public async Task Given_LoadedSale_When_Updating_Then_SavesAndReturnsReconciledSale()
    {
        // Given (the first line goes to 10 × 4.50 at 20%, the second is dropped, and 2 × 8.00 is added)
        var sale = GivenLoadedSale();
        var changed = sale.Items.First();
        var dropped = sale.Items.Last();
        var added = UpdateSaleCommandTestData.GenerateItem(Guid.NewGuid(), 2, 8.00m);
        var command = UpdateSaleCommandTestData.GenerateValidCommand(sale.Id,
            UpdateSaleCommandTestData.GenerateItem(changed.Product.Id, 10, 4.50m), added);

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then (36.00 + 16.00; the dropped line keeps its 30.00 but doesn't count)
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        result.UpdatedAt.Should().NotBeNull();
        result.Should().BeEquivalentTo(new
        {
            sale.Id,
            sale.SaleNumber,
            command.SaleDate,
            command.CustomerId,
            command.CustomerName,
            command.BranchId,
            command.BranchName,
            TotalAmount = 52.00m,
            IsCancelled = false,
            sale.UpdatedAt,
            Items = new[]
            {
                new { ProductId = changed.Product.Id, Quantity = 10, TotalAmount = 36.00m, IsCancelled = false },
                new { ProductId = dropped.Product.Id, Quantity = 3, TotalAmount = 30.00m, IsCancelled = true },
                new { added.ProductId, Quantity = 2, TotalAmount = 16.00m, IsCancelled = false }
            }
        });
    }

    /// <summary>
    /// Tests that an unknown sale is a not-found error, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When updating it Then it throws KeyNotFoundException and saves and publishes nothing")]
    public async Task Given_NoSaleWithId_When_Updating_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var saleId = Guid.NewGuid();
        _saleRepository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(UpdateSaleCommandTestData.GenerateValidCommand(saleId), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"The sale with ID {saleId} does not exist");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R7: the domain's rejection of a cancelled sale propagates, and nothing is saved or published.
    /// </summary>
    [Fact(DisplayName = "Given a cancelled sale When updating it Then DomainException propagates and nothing is saved or published")]
    public async Task Given_CancelledSale_When_Updating_Then_DomainExceptionAndNothingSaved()
    {
        // Given
        var sale = GivenLoadedSale();
        sale.Cancel();
        sale.ClearDomainEvents();

        // When
        var act = () => _handler.Handle(UpdateSaleCommandTestData.GenerateValidCommand(sale.Id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<DomainException>().WithMessage($"Sale {sale.SaleNumber} is cancelled and cannot be modified");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _publisher.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14 and §5.3: <see cref="ItemCancelledEvent"/> for the dropped line, then
    /// <see cref="SaleModifiedEvent"/>, are published after the save, and then the events are cleared.
    /// </summary>
    [Fact(DisplayName = "Given a loaded sale When updating it without its first product Then it publishes ItemCancelledEvent then SaleModifiedEvent after the save and clears the events")]
    public async Task Given_LoadedSale_When_UpdatingWithoutFirstProduct_Then_PublishesBothEventsInOrderAfterSave()
    {
        // Given
        var sale = GivenLoadedSale();
        var dropped = sale.Items.First();
        var kept = sale.Items.Last();
        var command = UpdateSaleCommandTestData.GenerateValidCommand(sale.Id,
            UpdateSaleCommandTestData.GenerateItem(kept.Product.Id, 3, 10.00m));
        var published = CapturePublishedEvents();

        // When
        await _handler.Handle(command, CancellationToken.None);

        // Then
        Received.InOrder(() =>
        {
            _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
            _publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        });
        published.Should().Equal(
            new ItemCancelledEvent(sale.Id, sale.SaleNumber, dropped.Id, dropped.Product.Id, sale.UpdatedAt!.Value),
            new SaleModifiedEvent(sale.Id, sale.SaleNumber, 30.00m, sale.UpdatedAt!.Value));
        sale.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Tests rule R14: when the save fails, nothing is published.
    /// </summary>
    [Fact(DisplayName = "Given the save throws When updating a sale Then the exception propagates and nothing is published")]
    public async Task Given_SaveThrows_When_Updating_Then_NothingIsPublished()
    {
        // Given
        var sale = GivenLoadedSale();
        _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("The database is unavailable"));

        // When
        var act = () => _handler.Handle(UpdateSaleCommandTestData.GenerateValidCommand(sale.Id), CancellationToken.None);

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
