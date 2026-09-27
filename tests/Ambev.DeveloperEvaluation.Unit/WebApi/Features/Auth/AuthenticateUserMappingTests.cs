using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Auth;

/// <summary>
/// Contains unit tests for the login maps, from <see cref="AuthenticateUserRequest"/> to <see cref="AuthenticateUserCommand"/>
/// and from <see cref="AuthenticateUserResult"/> to <see cref="AuthenticateUserResponse"/>, with the AutoMapper configuration
/// the API registers.
/// </summary>
public sealed class AuthenticateUserMappingTests
{
    private readonly IMapper _mapper = ApiMapper.Create();

    /// <summary>
    /// Tests that the login request becomes a command with the same credentials.
    /// </summary>
    [Fact(DisplayName = "Given a login request When mapping it to AuthenticateUserCommand Then the email and password are copied")]
    public void Given_LoginRequest_When_MappingToCommand_Then_EmailAndPasswordAreCopied()
    {
        // Given
        var request = new AuthenticateUserRequest { Email = "maria.silva@example.com", Password = "Str0ng@Pass" };

        // When
        var command = _mapper.Map<AuthenticateUserCommand>(request);

        // Then
        command.Should().BeEquivalentTo(new { request.Email, request.Password });
    }

    /// <summary>
    /// Tests that the login response carries the token and the user's email, name and role.
    /// </summary>
    [Fact(DisplayName = "Given a login result When mapping it to AuthenticateUserResponse Then the token, email, name and role are copied")]
    public void Given_LoginResult_When_MappingToResponse_Then_TokenEmailNameAndRoleAreCopied()
    {
        // Given
        var result = new AuthenticateUserResult
        {
            Token = "header.payload.signature",
            Id = Guid.NewGuid(),
            Name = "maria.silva",
            Email = "maria.silva@example.com",
            Phone = "+5511987654321",
            Role = "Admin"
        };

        // When
        var response = _mapper.Map<AuthenticateUserResponse>(result);

        // Then
        response.Should().BeEquivalentTo(new { result.Token, result.Email, result.Name, result.Role });
    }
}
