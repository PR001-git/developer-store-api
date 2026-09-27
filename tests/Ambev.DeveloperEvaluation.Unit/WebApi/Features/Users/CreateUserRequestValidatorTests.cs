using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

/// <summary>
/// Contains unit tests for the <see cref="CreateUserRequestValidator"/> class, focused on the enum fields that the
/// JSON deserializer can otherwise fill with values a client never intended (a bare integer, or a comma-combined name).
/// </summary>
public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    /// <summary>
    /// Tests that a valid sign-up request passes every rule.
    /// </summary>
    [Fact(DisplayName = "Given a valid sign-up request When validated Then has no errors")]
    public void Given_ValidRequest_When_Validated_Then_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var request = UserRequestTestData.GenerateValidCreateUserRequest();

        // Act
        var result = _validator.TestValidate(request);

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
        var request = UserRequestTestData.GenerateValidCreateUserRequest();
        request.Role = (UserRole)7;

        // Act
        var result = _validator.TestValidate(request);

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
        var request = UserRequestTestData.GenerateValidCreateUserRequest();
        request.Status = (UserStatus)99;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
