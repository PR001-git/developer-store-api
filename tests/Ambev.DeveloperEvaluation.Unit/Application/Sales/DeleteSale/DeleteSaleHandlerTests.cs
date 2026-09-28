using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.DeleteSale;

/// <summary>
/// Contains unit tests for <see cref="DeleteSaleHandler"/>.
/// </summary>
public sealed class DeleteSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly DeleteSaleHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSaleHandlerTests"/> class.
    /// </summary>
    public DeleteSaleHandlerTests()
    {
        _handler = new DeleteSaleHandler(_saleRepository);
    }

    /// <summary>
    /// Tests that the handler deletes the loaded sale before it saves it.
    /// </summary>
    [Fact(DisplayName = "Given a sale When deleting it Then it saves the sale already marked deleted")]
    public async Task Given_Sale_When_Deleting_Then_SavesItMarkedDeleted()
    {
        // Given
        var sale = SaleTestData.CreateSale();
        _saleRepository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        var deletedWhenSaved = false;
        _saleRepository
            .When(repository => repository.UpdateAsync(sale, Arg.Any<CancellationToken>()))
            .Do(_ => deletedWhenSaved = sale.IsDeleted);

        // When
        await _handler.Handle(new DeleteSaleCommand(sale.Id), CancellationToken.None);

        // Then
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        deletedWhenSaved.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a missing sale is a not-found error and nothing is saved. A deleted sale takes the same path,
    /// because the query filter makes <see cref="ISaleRepository.GetByIdAsync"/> return <c>null</c> for it.
    /// </summary>
    [Fact(DisplayName = "Given no sale with the id When deleting Then it throws KeyNotFoundException and saves nothing")]
    public async Task Given_NoSaleWithId_When_Deleting_Then_ThrowsKeyNotFoundException()
    {
        // Given
        var id = Guid.NewGuid();
        _saleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        // When
        var act = () => _handler.Handle(new DeleteSaleCommand(id), CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"The sale with ID {id} does not exist");
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }
}
