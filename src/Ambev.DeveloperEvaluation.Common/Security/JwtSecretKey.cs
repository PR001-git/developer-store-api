using Microsoft.Extensions.Configuration;

namespace Ambev.DeveloperEvaluation.Common.Security;

/// <summary>
/// Reads the key that signs and validates the JWT tokens, so startup and token generation fail the same way when it is missing.
/// </summary>
internal static class JwtSecretKey
{
    /// <summary>
    /// The configuration setting that holds the key.
    /// </summary>
    public const string SettingName = "Jwt:SecretKey";

    /// <summary>
    /// Returns the configured signing key.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The key; never null, empty or blank.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the setting is missing, empty or blank.</exception>
    public static string Read(IConfiguration configuration)
    {
        var secretKey = configuration[SettingName];
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException(
                $"Configuration setting '{SettingName}' is missing or empty. Set it to the key that signs the JWT tokens.");

        return secretKey;
    }
}
