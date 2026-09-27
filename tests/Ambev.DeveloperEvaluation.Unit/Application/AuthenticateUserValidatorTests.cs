using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

/// <summary>
/// Contains unit tests for the <see cref="AuthenticateUserValidator"/> class. Login validation must only check
/// presence and format, never password strength, so a wrong password always fails as 401 Invalid credentials
/// instead of leaking the sign-up password policy through a 400.
/// </summary>
public class AuthenticateUserValidatorTests
{
    private readonly AuthenticateUserValidator _validator = new();

    /// <summary>
    /// Tests that a valid email and any non-empty password pass validation.
    /// </summary>
    [Fact(DisplayName = "Given a valid email and a short password When validated Then has no errors")]
    public void Given_ValidEmailAndShortPassword_When_Validated_Then_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new AuthenticateUserCommand { Email = "maria.silva@example.com", Password = "ab" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that an empty email fails validation.
    /// </summary>
    [Fact(DisplayName = "Given an empty email When validated Then has an error for Email")]
    public void Given_EmptyEmail_When_Validated_Then_ShouldHaveErrorForEmail()
    {
        // Arrange
        var command = new AuthenticateUserCommand { Email = string.Empty, Password = "Str0ng@Pass" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    /// <summary>
    /// Tests that a malformed email fails validation.
    /// </summary>
    [Fact(DisplayName = "Given a malformed email When validated Then has an error for Email")]
    public void Given_MalformedEmail_When_Validated_Then_ShouldHaveErrorForEmail()
    {
        // Arrange
        var command = new AuthenticateUserCommand { Email = "not-an-email", Password = "Str0ng@Pass" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    /// <summary>
    /// Tests that an empty password fails validation.
    /// </summary>
    [Fact(DisplayName = "Given an empty password When validated Then has an error for Password")]
    public void Given_EmptyPassword_When_Validated_Then_ShouldHaveErrorForPassword()
    {
        // Arrange
        var command = new AuthenticateUserCommand { Email = "maria.silva@example.com", Password = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
