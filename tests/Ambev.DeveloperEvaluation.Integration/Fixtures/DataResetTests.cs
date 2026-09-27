using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Integration.TestData;
using Ambev.DeveloperEvaluation.ORM.Mapping;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Contains tests for <see cref="PostgreSqlFixture.ResetDataAsync"/>, which runs after every test class.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class DataResetTests
{
    private const string SaleNumberSequence = SaleConfiguration.SaleNumberSequence;

    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResetTests"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public DataResetTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <summary>
    /// Tests that the reset empties the listed tables.
    /// </summary>
    [Fact(DisplayName = "Given a saved user When the data is reset Then the Users table is empty")]
    public async Task Given_SavedUser_When_DataIsReset_Then_UsersTableIsEmpty()
    {
        // Given
        await using (var context = _database.CreateContext())
        {
            await new UserRepository(context).CreateAsync(UserTestData.GenerateValidUser());
        }

        // When
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        await using var readContext = _database.CreateContext();
        (await readContext.Users.CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// Tests that the reset empties the Sales tables too, items included.
    /// </summary>
    [Fact(DisplayName = "Given a saved sale When the data is reset Then the Sales and SaleItems tables are empty")]
    public async Task Given_SavedSale_When_DataIsReset_Then_SalesTablesAreEmpty()
    {
        // Given
        await using (var context = _database.CreateContext())
        {
            await new SaleRepository(context).CreateAsync(SaleTestData.GenerateValidSale());
        }

        // When
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        await using var readContext = _database.CreateContext();
        (await readContext.Sales.CountAsync()).Should().Be(0);
        (await readContext.Set<SaleItem>().CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// Tests that the reset restarts the listed sequences, so sale numbers start over in every test class.
    /// </summary>
    [Fact(DisplayName = "Given an advanced sale_number_seq When the data is reset Then the sequence starts over at 1")]
    public async Task Given_AdvancedSaleNumberSequence_When_DataIsReset_Then_SequenceStartsOverAtOne()
    {
        // Given
        await ExecuteAsync($"SELECT nextval('{SaleNumberSequence}'); SELECT nextval('{SaleNumberSequence}');");

        // When
        await _database.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        (await NextValueAsync(SaleNumberSequence)).Should().Be(1);
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var dataSource = NpgsqlDataSource.Create(_database.ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<long> NextValueAsync(string sequence)
    {
        await using var dataSource = NpgsqlDataSource.Create(_database.ConnectionString);
        await using var command = dataSource.CreateCommand($"SELECT nextval('{sequence}');");
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
