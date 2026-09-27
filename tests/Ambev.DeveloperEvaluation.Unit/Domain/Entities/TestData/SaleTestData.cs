using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

/// <summary>
/// Generates valid inputs for <see cref="Sale.Create"/> with Bogus, and sales built from them.
/// Every generated value passes the domain rules, so a test changes only the value it is about.
/// </summary>
public static class SaleTestData
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a sale number of the form <c>S-123456</c>.
    /// </summary>
    /// <returns>A valid sale number.</returns>
    public static string GenerateSaleNumber() => $"S-{Faker.Random.Number(100000, 999999)}";

    /// <summary>
    /// Generates a customer with a random id and a person's name.
    /// </summary>
    /// <returns>A valid customer identity.</returns>
    public static ExternalIdentity GenerateCustomer() => new(Guid.NewGuid(), Faker.Name.FullName());

    /// <summary>
    /// Generates a branch with a random id and a city name.
    /// </summary>
    /// <returns>A valid branch identity.</returns>
    public static ExternalIdentity GenerateBranch() => new(Guid.NewGuid(), $"Filial {Faker.Address.City()}");

    /// <summary>
    /// Generates a line for a new product with the given quantity and unit price.
    /// </summary>
    /// <param name="quantity">The quantity of identical items.</param>
    /// <param name="unitPrice">The price of one item.</param>
    /// <returns>The line's input data.</returns>
    public static SaleItemData GenerateItem(int quantity, decimal unitPrice) =>
        new(new ExternalIdentity(Guid.NewGuid(), Faker.Commerce.ProductName()), quantity, unitPrice);

    /// <summary>
    /// Generates a line for a new product with a random quantity from 1 to 20 and a random price with 2 decimal places.
    /// </summary>
    /// <returns>The line's input data.</returns>
    public static SaleItemData GenerateItem() =>
        GenerateItem(Faker.Random.Int(1, 20), Math.Round(Faker.Random.Decimal(0.01m, 500m), 2));

    /// <summary>
    /// Creates a sale with a random number, date, customer and branch.
    /// </summary>
    /// <param name="items">The lines of the sale. With none, the sale gets one random line.</param>
    /// <returns>A new sale.</returns>
    public static Sale CreateSale(params SaleItemData[] items) =>
        Sale.Create(
            GenerateSaleNumber(),
            Faker.Date.Recent().ToUniversalTime(),
            GenerateCustomer(),
            GenerateBranch(),
            items.Length > 0 ? items : [GenerateItem()]);
}
