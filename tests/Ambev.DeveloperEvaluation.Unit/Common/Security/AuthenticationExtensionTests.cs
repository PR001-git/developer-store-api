using Ambev.DeveloperEvaluation.Common.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Security;

/// <summary>
/// Contains unit tests for <see cref="AuthenticationExtension.AddJwtAuthentication"/>.
/// </summary>
public sealed class AuthenticationExtensionTests
{
    /// <summary>
    /// Tests that a missing, empty or blank signing key stops the registration with an error that names the setting.
    /// </summary>
    /// <param name="secretKey">The configured value of <c>Jwt:SecretKey</c>.</param>
    [Theory(DisplayName = "Given a missing or blank Jwt:SecretKey When adding JWT authentication Then it throws a configuration error naming the setting")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_MissingOrBlankSecretKey_When_AddingJwtAuthentication_Then_ThrowsConfigurationErrorNamingTheSetting(string? secretKey)
    {
        // Given
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = secretKey })
            .Build();
        var services = new ServiceCollection();

        // When
        var act = () => services.AddJwtAuthentication(configuration);

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Configuration setting 'Jwt:SecretKey' is missing or empty. Set it to the key that signs the JWT tokens.");
    }
}
