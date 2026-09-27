using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
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
    public Task ResetDataAsync(IReadOnlyCollection<string> tables, IReadOnlyCollection<string> sequences) =>
        DataReset.RunAsync(_database.GetConnectionString(), tables, sequences);

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
}
