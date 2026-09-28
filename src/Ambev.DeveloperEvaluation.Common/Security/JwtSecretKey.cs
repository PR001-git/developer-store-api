using Microsoft.Extensions.Configuration;
using System.Text;

namespace Ambev.DeveloperEvaluation.Common.Security;

/// <summary>
/// Reads the key that signs and validates the JWT tokens, so startup and token generation fail the same way when it is missing
/// or too short to sign HMAC-SHA256 tokens.
/// </summary>
internal static class JwtSecretKey
{
    /// <summary>
    /// The configuration setting that holds the key.
    /// </summary>
    public const string SettingName = "Jwt:SecretKey";

    /// <summary>
    /// The minimum key length, in bytes, that HMAC-SHA256 requires (256 bits).
    /// </summary>
    private const int MinimumKeyLengthInBytes = 32;

    /// <summary>
    /// Returns the configured signing key.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The key; never null, empty, blank, or shorter than <see cref="MinimumKeyLengthInBytes"/> bytes once UTF-8 encoded.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the setting is missing, empty, blank, or too short.</exception>
    public static string Read(IConfiguration configuration)
    {
        var secretKey = configuration[SettingName];
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException(
                $"Configuration setting '{SettingName}' is missing or empty. Set it to the key that signs the JWT tokens.");

        if (Encoding.UTF8.GetByteCount(secretKey) < MinimumKeyLengthInBytes)
            throw new InvalidOperationException(
                $"Configuration setting '{SettingName}' is too short. It must be at least {MinimumKeyLengthInBytes} bytes (256 bits) long once UTF-8 encoded.");

        return secretKey;
    }

    /// <summary>
    /// Returns the configured signing key as UTF-8 bytes, ready to build a <c>SymmetricSecurityKey</c> from.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The UTF-8 bytes of the key returned by <see cref="Read"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the setting is missing, empty, blank, or too short.</exception>
    public static byte[] ReadBytes(IConfiguration configuration) => Encoding.UTF8.GetBytes(Read(configuration));
}
