using Bogus;

namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// Generates create-sale and update-sale bodies that pass every Sales rule, with Bogus.
/// </summary>
public static class SaleRequestBodyTestData
{
    /// <summary>
    /// Generates a valid body. The sale number is unique on every call, so tests that share the database never
    /// collide on its unique index, and the sale date has whole seconds, so it reads back exactly.
    /// </summary>
    /// <param name="items">The lines of the sale. With none, the body gets one random line.</param>
    /// <returns>A valid <see cref="SaleRequestBody"/>.</returns>
    public static SaleRequestBody GenerateValid(params SaleItemRequestBody[] items)
    {
        var faker = new Faker();
        var saleDate = faker.Date.Recent().ToUniversalTime();
        return new SaleRequestBody(
            SaleNumber: $"S-{Guid.NewGuid():N}",
            SaleDate: new DateTime(saleDate.Year, saleDate.Month, saleDate.Day, saleDate.Hour, saleDate.Minute, saleDate.Second, DateTimeKind.Utc),
            CustomerId: Guid.NewGuid(),
            CustomerName: faker.Name.FullName(),
            BranchId: Guid.NewGuid(),
            BranchName: $"Filial {faker.Address.City()}",
            Items: items.Length > 0 ? items : [GenerateItem(faker.Random.Int(1, 20), Math.Round(faker.Random.Decimal(0.01m, 500m), 2))]);
    }

    /// <summary>
    /// Generates a valid update body: a new header, as <see cref="GenerateValid"/> makes it but without a sale number,
    /// and the given lines. A line with an existing line's product changes that line, and an active line whose product
    /// is left out is cancelled (rule R10).
    /// </summary>
    /// <param name="items">The lines the sale must have. With none, the body gets one random line.</param>
    /// <returns>A valid <see cref="UpdateSaleRequestBody"/>.</returns>
    public static UpdateSaleRequestBody GenerateValidUpdate(params SaleItemRequestBody[] items)
    {
        var sale = GenerateValid(items);
        return new UpdateSaleRequestBody(sale.SaleDate, sale.CustomerId, sale.CustomerName, sale.BranchId, sale.BranchName, sale.Items);
    }

    /// <summary>
    /// Generates a line for a new product.
    /// </summary>
    /// <param name="quantity">The quantity of identical items.</param>
    /// <param name="unitPrice">The price of one item.</param>
    /// <returns>The line.</returns>
    public static SaleItemRequestBody GenerateItem(int quantity, decimal unitPrice) =>
        new(Guid.NewGuid(), new Faker().Commerce.ProductName(), quantity, unitPrice);
}
