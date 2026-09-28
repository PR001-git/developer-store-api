using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Groups every database-backed functional test: one container and one in-memory API for the run,
/// and a data reset after each test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>, IClassFixture<DataResetFixture>
{
    /// <summary>
    /// The collection name to put in <c>[Collection(ApiCollection.Name)]</c>.
    /// </summary>
    public const string Name = "Api";
}
