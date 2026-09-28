using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.TestData;

/// <summary>
/// Generates valid request contracts for the Users and Auth controllers, reusing the Bogus rules of <see cref="UserTestData"/>.
/// </summary>
public static class UserRequestTestData
{
    /// <summary>
    /// Generates a <see cref="CreateUserRequest"/> that passes <see cref="CreateUserRequestValidator"/>.
    /// </summary>
    /// <returns>A valid sign-up request for an active customer.</returns>
    public static CreateUserRequest GenerateValidCreateUserRequest() => new()
    {
        Username = UserTestData.GenerateValidUsername(),
        Password = UserTestData.GenerateValidPassword(),
        Phone = UserTestData.GenerateValidPhone(),
        Email = UserTestData.GenerateValidEmail(),
        Status = UserStatus.Active,
        Role = UserRole.Customer
    };

    /// <summary>
    /// Generates an <see cref="AuthenticateUserRequest"/> that passes <see cref="AuthenticateUserRequestValidator"/>.
    /// </summary>
    /// <returns>A valid login request.</returns>
    public static AuthenticateUserRequest GenerateValidAuthenticateUserRequest() => new()
    {
        Email = UserTestData.GenerateValidEmail(),
        Password = UserTestData.GenerateValidPassword()
    };
}
