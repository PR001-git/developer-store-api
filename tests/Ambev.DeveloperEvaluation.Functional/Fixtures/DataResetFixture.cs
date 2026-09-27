using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Resets the data after each test class in <see cref="ApiCollection"/>, so the next class starts with empty tables.
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

    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResetFixture"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public DataResetFixture(ApiFixture api)
    {
        _api = api;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs after the last test of the class.
    /// </summary>
    public Task DisposeAsync() => _api.ResetDataAsync(Tables, Sequences);
}
