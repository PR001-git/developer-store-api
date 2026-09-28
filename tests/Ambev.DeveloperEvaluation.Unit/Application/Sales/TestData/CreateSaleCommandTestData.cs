using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;

/// <summary>
/// Generates valid <see cref="CreateSaleCommand"/> instances with Bogus. Every generated value passes the
/// validator and the domain rules, so a test changes only the value it is about.
/// </summary>
public static class CreateSaleCommandTestData
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a valid command whose lines are all for different products.
    /// </summary>
    /// <param name="itemCount">The number of lines.</param>
    /// <returns>A valid command.</returns>
    public static CreateSaleCommand GenerateValidCommand(int itemCount = 2) => new()
    {
        SaleNumber = $"S-{Faker.Random.Number(100000, 999999)}",
        SaleDate = Faker.Date.Recent().ToUniversalTime(),
        CustomerId = Guid.NewGuid(),
        CustomerName = Faker.Name.FullName(),
        BranchId = Guid.NewGuid(),
        BranchName = $"Filial {Faker.Address.City()}",
        Items = Enumerable.Range(0, itemCount).Select(_ => GenerateValidItem()).ToList()
    };

    /// <summary>
    /// Generates a valid line for a new product, with a quantity from 1 to 20 and a price with 2 decimal places.
    /// </summary>
    /// <returns>A valid line.</returns>
    public static SaleItemInput GenerateValidItem() => new()
    {
        ProductId = Guid.NewGuid(),
        ProductName = Faker.Commerce.ProductName(),
        Quantity = Faker.Random.Int(1, 20),
        UnitPrice = Math.Round(Faker.Random.Decimal(0.01m, 500m), 2)
    };
}
