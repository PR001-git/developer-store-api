using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Ambev.DeveloperEvaluation.ORM.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.ORM;

/// <summary>
/// Contains integration tests for <see cref="SaleNumberGenerator"/> (spec §8.3). Each test resets the data first,
/// because it depends on the sequence's value and the order of tests in a class isn't fixed.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SaleNumberGeneratorTests
{
    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleNumberGeneratorTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public SaleNumberGeneratorTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests steps 1, 2 and 4 of §8.3: the numbers follow the sequence, zero-padded to 6 digits.
    /// </summary>
    [Fact(DisplayName = "Given the data reset restarted sale_number_seq When generating two numbers Then they are S-000001 and S-000002")]
    public async Task Given_RestartedSequence_When_GeneratingTwoNumbers_Then_TheyAreFirstAndSecond()
    {
        // Given
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await using var context = _database.CreateContext();
        var generator = CreateGenerator(context);

        // When
        var first = await generator.NextAsync();
        var second = await generator.NextAsync();

        // Then
        first.Should().Be("S-000001");
        second.Should().Be("S-000002");
    }

    /// <summary>
    /// Tests step 3 of §8.3: numbers clients already took are skipped, however many in a row.
    /// </summary>
    [Fact(DisplayName = "Given clients took S-000001 and S-000002 When generating a number Then it skips both and returns S-000003")]
    public async Task Given_ClientsTookFirstTwoNumbers_When_Generating_Then_SkipsThemAndReturnsThird()
    {
        // Given
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await using (var writeContext = _database.CreateContext())
        {
            var sales = new SaleRepository(writeContext);
            await sales.CreateAsync(SaleTestData.GenerateValidSale(saleNumber: "S-000001"));
            await sales.CreateAsync(SaleTestData.GenerateValidSale(saleNumber: "S-000002"));
        }

        // When
        await using var context = _database.CreateContext();
        var number = await CreateGenerator(context).NextAsync();

        // Then
        number.Should().Be("S-000003");
    }

    /// <summary>
    /// Tests step 2 of §8.3: past 999999 the number grows instead of being cut to 6 digits.
    /// The class's data reset restarts the sequence afterwards.
    /// </summary>
    [Fact(DisplayName = "Given sale_number_seq at 1000000 When generating a number Then it has 7 digits")]
    public async Task Given_SequenceAtOneMillion_When_Generating_Then_NumberHasSevenDigits()
    {
        // Given
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);
        await using var context = _database.CreateContext();
        await context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE sale_number_seq RESTART WITH 1000000");

        // When
        var number = await CreateGenerator(context).NextAsync();

        // Then
        number.Should().Be("S-1000000");
    }

    private static SaleNumberGenerator CreateGenerator(DefaultContext context) =>
        new(context, new SaleRepository(context));
}
