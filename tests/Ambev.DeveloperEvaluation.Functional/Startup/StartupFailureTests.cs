using Ambev.DeveloperEvaluation.WebApi;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Startup;

/// <summary>
/// Contains tests for what the API does when it cannot start.
/// They need no Docker: startup fails before anything touches the database.
/// </summary>
public sealed class StartupFailureTests
{
    /// <summary>
    /// Tests that a startup exception reaches the host instead of being swallowed by <c>Program.Main</c>.
    /// </summary>
    [Fact(DisplayName = "Given an empty JWT secret key When the API starts Then the startup exception reaches the host")]
    public void Given_EmptyJwtSecretKey_When_ApiStarts_Then_StartupExceptionReachesHost()
    {
        // Given
        using var factory = new WebApplicationFactory<Program>();
        var misconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:SecretKey", string.Empty));

        // When
        var start = () => misconfigured.CreateClient();

        // Then
        start.Should().Throw<ArgumentException>().WithParameterName("secretKey");
    }
}
