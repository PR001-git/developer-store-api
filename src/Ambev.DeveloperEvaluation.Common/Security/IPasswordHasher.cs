using System;

namespace Ambev.DeveloperEvaluation.Common.Security;

/// <summary>
/// Provides functionality for hashing and verifying passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password using a secure hashing algorithm.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The hashed password.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies if a plain text password matches a hashed password.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hash">The hashed password to compare against.</param>
    /// <returns>True if the password matches the hash, false otherwise.</returns>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// A fixed hash, computed once at the same cost as a real one, for a caller to verify against when there is
    /// no real user to compare a password with. This keeps an unknown-email login as slow as a known one, so
    /// the response time doesn't tell an attacker whether the email exists.
    /// </summary>
    string DummyHash { get; }
}
