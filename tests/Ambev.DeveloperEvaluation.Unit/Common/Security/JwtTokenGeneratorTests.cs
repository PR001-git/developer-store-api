using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Security;

/// <summary>
/// Contains unit tests for the <see cref="JwtTokenGenerator"/> class.
/// </summary>
public sealed class JwtTokenGeneratorTests
{
    private const string SecretKey = "unit-test-signing-key-that-is-at-least-32-bytes-long";

    /// <summary>
    /// Tests that the token is signed with the configured key and identifies the user.
    /// </summary>
    [Fact(DisplayName = "Given a configured Jwt:SecretKey When generating a token Then it is signed with that key and carries the user's id, name and role")]
    public void Given_ConfiguredSecretKey_When_GeneratingToken_Then_TokenIsSignedAndCarriesUserClaims()
    {
        // Given
        var generator = new JwtTokenGenerator(CreateConfiguration(SecretKey));
        var user = UserTestData.GenerateValidUser();
        user.Id = Guid.NewGuid();

        // When
        var token = generator.GenerateToken(user);

        // Then
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        }, out _);
        principal.Claims.Select(claim => (claim.Type, claim.Value)).Should().Contain(new[]
        {
            (ClaimTypes.NameIdentifier, user.Id.ToString()),
            (ClaimTypes.Name, user.Username),
            (ClaimTypes.Role, user.Role.ToString())
        });
    }

    /// <summary>
    /// Tests that a missing signing key fails with the same configuration error as startup, instead of a null argument.
    /// </summary>
    [Fact(DisplayName = "Given no Jwt:SecretKey When generating a token Then it throws a configuration error naming the setting")]
    public void Given_NoSecretKey_When_GeneratingToken_Then_ThrowsConfigurationErrorNamingTheSetting()
    {
        // Given
        var generator = new JwtTokenGenerator(CreateConfiguration(null));

        // When
        var act = () => generator.GenerateToken(UserTestData.GenerateValidUser());

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Configuration setting 'Jwt:SecretKey' is missing or empty. Set it to the key that signs the JWT tokens.");
    }

    /// <summary>
    /// Tests that a key shorter than 32 bytes fails the same way startup does, instead of throwing IDX10720 from the JWT library.
    /// </summary>
    [Fact(DisplayName = "Given a Jwt:SecretKey shorter than 32 bytes When generating a token Then it throws a configuration error naming the setting")]
    public void Given_TooShortSecretKey_When_GeneratingToken_Then_ThrowsConfigurationErrorNamingTheSetting()
    {
        // Given
        var generator = new JwtTokenGenerator(CreateConfiguration("too-short-key"));

        // When
        var act = () => generator.GenerateToken(UserTestData.GenerateValidUser());

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Configuration setting 'Jwt:SecretKey' is too short. It must be at least 32 bytes (256 bits) long once UTF-8 encoded.");
    }

    /// <summary>
    /// Tests that a key with non-ASCII characters signs and validates with its actual UTF-8 bytes, instead of the '?' bytes ASCII encoding would produce.
    /// </summary>
    [Fact(DisplayName = "Given a Jwt:SecretKey with non-ASCII characters When generating a token Then it is signed with the key's UTF-8 bytes")]
    public void Given_NonAsciiSecretKey_When_GeneratingToken_Then_TokenIsSignedWithUtf8Bytes()
    {
        // Given
        const string nonAsciiSecretKey = "chave-secreta-com-acentuação-e-emoji-🔒-que-passa-de-32-bytes";
        var generator = new JwtTokenGenerator(CreateConfiguration(nonAsciiSecretKey));
        var user = UserTestData.GenerateValidUser();

        // When
        var token = generator.GenerateToken(user);

        // Then
        var act = () => new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(nonAsciiSecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        }, out _);
        act.Should().NotThrow();
    }

    private static IConfiguration CreateConfiguration(string? secretKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = secretKey })
            .Build();
}
