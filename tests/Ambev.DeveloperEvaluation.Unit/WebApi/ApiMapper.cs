using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.WebApi;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Unit.WebApi;

/// <summary>
/// Builds the mapper the API uses: <c>Program.cs</c> registers every AutoMapper profile in the WebApi and Application assemblies.
/// Mapping tests use it, so they fail when a map the API needs is missing.
/// </summary>
internal static class ApiMapper
{
    /// <summary>
    /// Creates a mapper with the API's profiles.
    /// </summary>
    /// <returns>A new <see cref="IMapper"/>.</returns>
    public static IMapper Create() =>
        new MapperConfiguration(config => config.AddMaps(typeof(Program).Assembly, typeof(ApplicationLayer).Assembly))
            .CreateMapper();
}
