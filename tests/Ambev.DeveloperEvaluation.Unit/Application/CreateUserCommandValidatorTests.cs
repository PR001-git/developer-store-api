using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

/// <summary>
/// Contains unit tests for the <see cref="CreateUserCommandValidator"/> class, focused on the enum fields that a
/// caller could otherwise set to a value the deserializer accepted but the domain never defined.
/// </summary>
public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    /// <summary>
    /// Tests that a valid command passes every rule.
    /// </summary>
    [Fact(DisplayName = "Given a valid command When validated Then has no errors")]
    public void Given_ValidCommand_When_Validated_Then_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = CreateUserHandlerTestData.GenerateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that a role outside the enum's defined values fails validation, closing the gap a raw integer like
    /// <c>"role": 7</c> could otherwise slip through as.
    /// </summary>
    [Fact(DisplayName = "Given a role outside the defined enum values When validated Then has an error for Role")]
    public void Given_UndefinedRole_When_Validated_Then_ShouldHaveErrorForRole()
    {
        // Arrange
        var command = CreateUserHandlerTestData.GenerateValidCommand();
        command.Role = (UserRole)7;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    /// <summary>
    /// Tests that a status outside the enum's defined values fails validation.
    /// </summary>
    [Fact(DisplayName = "Given a status outside the defined enum values When validated Then has an error for Status")]
    public void Given_UndefinedStatus_When_Validated_Then_ShouldHaveErrorForStatus()
    {
        // Arrange
        var command = CreateUserHandlerTestData.GenerateValidCommand();
        command.Status = (UserStatus)99;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
