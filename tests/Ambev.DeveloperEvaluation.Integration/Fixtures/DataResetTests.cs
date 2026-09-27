using Ambev.DeveloperEvaluation.Integration.TestData;
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
    /// Tests that the reset restarts the listed sequences. The test creates and drops its own sequence,
    /// because no migration creates one yet.
    /// </summary>
    [Fact(DisplayName = "Given an advanced sequence When the data is reset Then the sequence starts over at 1")]
    public async Task Given_AdvancedSequence_When_DataIsReset_Then_SequenceStartsOverAtOne()
    {
        // Given
        const string sequence = "reset_probe_seq";
        await ExecuteAsync($"CREATE SEQUENCE {sequence};");
        await ExecuteAsync($"SELECT nextval('{sequence}'); SELECT nextval('{sequence}');");

        try
        {
            // When
            await _database.ResetDataAsync([], [sequence]);

            // Then
            (await NextValueAsync(sequence)).Should().Be(1);
        }
        finally
        {
            await ExecuteAsync($"DROP SEQUENCE {sequence};");
        }
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
