using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Hosts the API in memory for the whole functional test run, connected to one PostgreSQL 13 container.
/// The API runs in Development, so it applies the migrations itself at startup.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:13").Build();

    /// <summary>
    /// Starts the container, then the API.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // Reading Server builds and starts the API now, so its startup migrations finish before the first test.
        _ = Server;
    }

    /// <summary>
    /// Empties the given tables and restarts the given sequences.
    /// </summary>
    /// <param name="tables">Tables to truncate.</param>
    /// <param name="sequences">Sequences to restart from their start value.</param>
    public async Task ResetDataAsync(IReadOnlyCollection<string> tables, IReadOnlyCollection<string> sequences)
    {
        var statements = new List<string>();
        if (tables.Count > 0)
            statements.Add($"TRUNCATE TABLE {string.Join(", ", tables.Select(Quote))};");
        statements.AddRange(sequences.Select(sequence => $"ALTER SEQUENCE {Quote(sequence)} RESTART;"));

        if (statements.Count == 0)
            return;

        await using var dataSource = NpgsqlDataSource.Create(_database.GetConnectionString());
        await using var command = dataSource.CreateCommand(string.Join(Environment.NewLine, statements));
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Stops the API, then removes the container.
    /// </summary>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _database.GetConnectionString());
    }

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
