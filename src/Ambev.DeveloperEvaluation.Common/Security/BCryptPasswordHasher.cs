using System;

namespace Ambev.DeveloperEvaluation.Common.Security;

/// <summary>
/// Implements password hashing functionality using BCrypt algorithm.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// A fixed hash, computed once with the same call <see cref="HashPassword"/> uses, so verifying against it
    /// costs the same as verifying against a real user's hash. The password behind it is never used to sign in.
    /// </summary>
    private static readonly string _dummyHash = BCrypt.Net.BCrypt.HashPassword("no-account-ever-uses-this-password");

    /// <inheritdoc />
    public string DummyHash => _dummyHash;

    /// <summary>
    /// Hashes a plain text password using BCrypt algorithm.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The BCrypt hashed password.</returns>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifies if a plain text password matches a BCrypt hashed password.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hash">The BCrypt hashed password to compare against.</param>
    /// <returns>True if the password matches the hash, false otherwise.</returns>
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
