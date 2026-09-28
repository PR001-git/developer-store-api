using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;

/// <summary>
/// Generates valid <see cref="UpdateSaleCommand"/> instances with Bogus. Every generated value passes the validator
/// and the domain rules, so a test changes only the value it is about.
/// </summary>
public static class UpdateSaleCommandTestData
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a valid command for the sale with the given id, with a new header.
    /// </summary>
    /// <param name="id">The id of the sale to update.</param>
    /// <param name="items">The lines the sale must have. With none, the command gets two lines for new products.</param>
    /// <returns>A valid command.</returns>
    public static UpdateSaleCommand GenerateValidCommand(Guid id, params SaleItemInput[] items) => new()
    {
        Id = id,
        SaleDate = Faker.Date.Recent().ToUniversalTime(),
        CustomerId = Guid.NewGuid(),
        CustomerName = Faker.Name.FullName(),
        BranchId = Guid.NewGuid(),
        BranchName = $"Filial {Faker.Address.City()}",
        Items = items.Length > 0 ? items : [CreateSaleCommandTestData.GenerateValidItem(), CreateSaleCommandTestData.GenerateValidItem()]
    };

    /// <summary>
    /// Generates a line for the given product, with a random product name. The id of an existing line's product
    /// changes that line (rule R10).
    /// </summary>
    /// <param name="productId">The id of the product.</param>
    /// <param name="quantity">The quantity of identical items.</param>
    /// <param name="unitPrice">The price of one item.</param>
    /// <returns>The line.</returns>
    public static SaleItemInput GenerateItem(Guid productId, int quantity, decimal unitPrice) => new()
    {
        ProductId = productId,
        ProductName = Faker.Commerce.ProductName(),
        Quantity = quantity,
        UnitPrice = unitPrice
    };
}
