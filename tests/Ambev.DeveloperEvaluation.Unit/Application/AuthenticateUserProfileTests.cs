using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.Unit.WebApi;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

/// <summary>
/// Contains unit tests for the Application-layer <see cref="AuthenticateUserProfile"/>, the map
/// <see cref="AuthenticateUserHandler"/> now actually uses to build its result.
/// </summary>
public sealed class AuthenticateUserProfileTests
{
    private readonly IMapper _mapper = ApiMapper.Create();

    /// <summary>
    /// Tests that mapping a User to AuthenticateUserResult carries every field the handler needs, with the
    /// username as the name, the role as a string, and the token left for the handler to set afterward.
    /// </summary>
    [Fact(DisplayName = "Given a user When mapping it to AuthenticateUserResult Then carries its fields with Username as Name and an empty Token")]
    public void Given_User_When_MappingToAuthenticateUserResult_Then_CarriesFieldsWithUsernameAsNameAndEmptyToken()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        user.Id = Guid.NewGuid();

        // When
        var result = _mapper.Map<AuthenticateUserResult>(user);

        // Then
        result.Should().BeEquivalentTo(new
        {
            user.Id,
            Name = user.Username,
            user.Email,
            user.Phone,
            Role = user.Role.ToString()
        });
        result.Token.Should().BeEmpty();
    }
}
