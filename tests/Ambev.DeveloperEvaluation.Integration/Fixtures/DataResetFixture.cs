using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Resets the data after each test class in <see cref="DatabaseCollection"/>, so the next class starts with empty tables.
/// </summary>
public sealed class DataResetFixture : IAsyncLifetime
{
    /// <summary>
    /// The tables the tests write to. Add a table here when a migration creates one.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Tables = ["Users", "Sales", "SaleItems"];

    /// <summary>
    /// The sequences the tests advance. Add a sequence here when a migration creates one.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Sequences = ["sale_number_seq"];

    private readonly PostgreSqlFixture _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResetFixture"/> class.
    /// </summary>
    /// <param name="database">The collection's database, injected by xUnit.</param>
    public DataResetFixture(PostgreSqlFixture database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs after the last test of the class.
    /// </summary>
    public Task DisposeAsync() => _database.ResetDataAsync(Tables, Sequences);
}
