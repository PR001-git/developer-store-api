using Bogus;

namespace Ambev.DeveloperEvaluation.Functional.TestData;

/// <summary>
/// Generates sign-up requests that pass every Users validator, with Bogus.
/// </summary>
public static class SignUpRequestTestData
{
    /// <summary>
    /// Generates a valid sign-up request. The email is unique on every call, so tests that share the database never collide.
    /// </summary>
    /// <param name="status">The account status to send.</param>
    /// <param name="role">The role to send.</param>
    /// <returns>A valid <see cref="SignUpRequest"/>.</returns>
    public static SignUpRequest GenerateValid(string status = "Active", string role = "Admin")
    {
        var faker = new Faker();
        return new SignUpRequest(
            Username: faker.Internet.UserName(),
            Password: $"Test@{faker.Random.Number(100, 999)}",
            Phone: $"+55{faker.Random.Number(11, 99)}{faker.Random.Number(100000000, 999999999)}",
            Email: $"user.{Guid.NewGuid():N}@example.com",
            Status: status,
            Role: role);
    }
}
