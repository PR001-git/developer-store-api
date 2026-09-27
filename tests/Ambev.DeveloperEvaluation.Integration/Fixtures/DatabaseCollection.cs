using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Groups every database-backed integration test: one container for the run, and a data reset after each test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgreSqlFixture>, IClassFixture<DataResetFixture>
{
    /// <summary>
    /// The collection name to put in <c>[Collection(DatabaseCollection.Name)]</c>.
    /// </summary>
    public const string Name = "Database";
}
