using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Runs one PostgreSQL 13 container for the whole integration test run, with every migration applied.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:13").Build();

    /// <summary>
    /// Gets the connection string of the running container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Creates a context connected to the container. The caller disposes it.
    /// </summary>
    /// <returns>A new <see cref="DefaultContext"/>.</returns>
    public DefaultContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new DefaultContext(options);
    }

    /// <summary>
    /// Starts the container and applies all migrations.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Stops and removes the container.
    /// </summary>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
