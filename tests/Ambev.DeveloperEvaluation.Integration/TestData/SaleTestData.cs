using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Bogus;

namespace Ambev.DeveloperEvaluation.Integration.TestData;

/// <summary>
/// Generates valid <see cref="Sale"/> aggregates with Bogus, within the column limits of the Sales tables.
/// </summary>
public static class SaleTestData
{
    private static readonly Faker Faker = new();

    /// <summary>
    /// Generates a sale that hasn't been saved yet, with lines for different products at random quantities
    /// (so across the discount tiers) and prices with cents.
    /// </summary>
    /// <param name="itemCount">The number of lines.</param>
    /// <returns>A new sale.</returns>
    public static Sale GenerateValidSale(int itemCount = 3) =>
        Sale.Create(
            $"S-{Faker.Random.Number(100000, 999999)}",
            Faker.Date.Recent().ToUniversalTime(),
            new ExternalIdentity(Guid.NewGuid(), Faker.Name.FullName()),
            new ExternalIdentity(Guid.NewGuid(), $"Filial {Faker.Address.City()}"),
            Enumerable.Range(0, itemCount).Select(_ => GenerateItem()).ToList());

    private static SaleItemData GenerateItem() =>
        new(new ExternalIdentity(Guid.NewGuid(), Faker.Commerce.ProductName()),
            Faker.Random.Int(1, 20),
            Math.Round(Faker.Random.Decimal(0.01m, 500m), 2));
}
