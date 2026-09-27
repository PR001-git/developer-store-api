using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Groups every database-backed functional test, so one container and one in-memory API serve the whole run.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    /// <summary>
    /// The collection name to put in <c>[Collection(ApiCollection.Name)]</c>.
    /// </summary>
    public const string Name = "Api";
}
