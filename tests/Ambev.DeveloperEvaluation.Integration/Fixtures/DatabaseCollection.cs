using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Groups every database-backed integration test, so one container serves the whole run.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgreSqlFixture>
{
    /// <summary>
    /// The collection name to put in <c>[Collection(DatabaseCollection.Name)]</c>.
    /// </summary>
    public const string Name = "Database";
}
