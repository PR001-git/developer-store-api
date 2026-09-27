using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

/// <summary>
/// Contains unit tests for the <see cref="AuthenticateUserHandler"/> class.
/// </summary>
public class AuthenticateUserHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IMapper _mapper;
    private readonly AuthenticateUserHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticateUserHandlerTests"/> class.
    /// </summary>
    public AuthenticateUserHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _mapper = Substitute.For<IMapper>();
        _passwordHasher.DummyHash.Returns("dummy-hash");
        _handler = new AuthenticateUserHandler(_userRepository, _passwordHasher, _jwtTokenGenerator, _mapper);
    }

    /// <summary>
    /// Tests that an unknown email still runs a password verification, against the fixed dummy hash, instead of
    /// short-circuiting straight to failure. Skipping BCrypt for an unknown email would answer faster than a
    /// known email with a wrong password, letting a caller tell the two cases apart and enumerate sign-ups.
    /// </summary>
    [Fact(DisplayName = "Given an unknown email When authenticating Then still verifies against the dummy hash and throws")]
    public async Task Given_UnknownEmail_When_Authenticating_Then_VerifiesAgainstDummyHashAndThrows()
    {
        // Given
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var command = new AuthenticateUserCommand { Email = "unknown@example.com", Password = "whatever" };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid credentials");
        _passwordHasher.Received(1).VerifyPassword(command.Password, "dummy-hash");
    }

    /// <summary>
    /// Tests that a wrong password for a known, active user is rejected with the same message an unknown email gets.
    /// </summary>
    [Fact(DisplayName = "Given a known user When authenticating with the wrong password Then throws Invalid credentials")]
    public async Task Given_KnownUser_When_AuthenticatingWithWrongPassword_Then_ThrowsInvalidCredentials()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        user.Status = UserStatus.Active;
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword(Arg.Any<string>(), user.Password).Returns(false);
        var command = new AuthenticateUserCommand { Email = user.Email, Password = "wrong-password" };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid credentials");
        _passwordHasher.Received(1).VerifyPassword(command.Password, user.Password);
    }

    /// <summary>
    /// Tests that an inactive user is rejected even with the right password, with a message distinct from a wrong password.
    /// </summary>
    [Fact(DisplayName = "Given an inactive user When authenticating with the right password Then throws User is not active")]
    public async Task Given_InactiveUser_When_AuthenticatingWithRightPassword_Then_ThrowsUserIsNotActive()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        user.Status = UserStatus.Inactive;
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword(Arg.Any<string>(), user.Password).Returns(true);
        var command = new AuthenticateUserCommand { Email = user.Email, Password = "right-password" };

        // When
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("User is not active");
    }

    /// <summary>
    /// Tests that a valid login builds the result from the mapped user (carrying every field AutoMapper knows how
    /// to copy, such as Id and Phone, which the handler used to leave empty by building AuthenticateUserResult by
    /// hand) and then sets the token the hand-built result never went through the mapper to get.
    /// </summary>
    [Fact(DisplayName = "Given an active user When authenticating with the right password Then returns the mapped result with the token set")]
    public async Task Given_ActiveUser_When_AuthenticatingWithRightPassword_Then_ReturnsMappedResultWithTokenSet()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        user.Id = Guid.NewGuid();
        user.Status = UserStatus.Active;
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword(Arg.Any<string>(), user.Password).Returns(true);
        _jwtTokenGenerator.GenerateToken(user).Returns("a-jwt-token");
        var mappedResult = new AuthenticateUserResult
        {
            Id = user.Id,
            Name = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString()
        };
        _mapper.Map<AuthenticateUserResult>(user).Returns(mappedResult);
        var command = new AuthenticateUserCommand { Email = user.Email, Password = "right-password" };

        // When
        var result = await _handler.Handle(command, CancellationToken.None);

        // Then
        result.Should().BeSameAs(mappedResult);
        result.Token.Should().Be("a-jwt-token");
        result.Id.Should().Be(user.Id);
        result.Phone.Should().Be(user.Phone);
    }
}
