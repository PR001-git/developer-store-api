using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ambev.DeveloperEvaluation.ORM;

/// <summary>
/// Creates the <see cref="DefaultContext"/> that the EF Core tools (<c>dotnet ef</c>) use at design time.
/// </summary>
/// <remarks>
/// Reads <c>ConnectionStrings:DefaultConnection</c> from the <c>appsettings.json</c> in the current directory,
/// then from environment variables, so a connection string set that way overrides the file. The tools set that
/// directory to the startup project's folder, so run them with
/// <c>--startup-project src/Ambev.DeveloperEvaluation.WebApi</c>.
/// </remarks>
public sealed class DefaultContextFactory : IDesignTimeDbContextFactory<DefaultContext>
{
    /// <summary>
    /// Creates a context that uses PostgreSQL and keeps its migrations in this assembly.
    /// </summary>
    /// <param name="args">Arguments passed by the design-time tools; not used.</param>
    /// <returns>A new <see cref="DefaultContext"/>.</returns>
    public DefaultContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var builder = new DbContextOptionsBuilder<DefaultContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(
               connectionString,
               b => b.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM")
        );

        return new DefaultContext(builder.Options);
    }
}
