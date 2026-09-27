using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

/// <summary>
/// Contains unit tests for the sign-up maps, from the saved <see cref="User"/> to <see cref="CreateUserResponse"/>,
/// with the AutoMapper configuration the API registers.
/// </summary>
public sealed class CreateUserMappingTests
{
    private readonly IMapper _mapper = ApiMapper.Create();

    /// <summary>
    /// Tests that the sign-up result carries the saved user's fields, with the username as the name.
    /// </summary>
    [Fact(DisplayName = "Given a saved user When mapping it to CreateUserResult Then the result carries its fields, with Username as Name")]
    public void Given_SavedUser_When_MappingToCreateUserResult_Then_CarriesItsFieldsWithUsernameAsName()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        user.Id = Guid.NewGuid();

        // When
        var result = _mapper.Map<CreateUserResult>(user);

        // Then
        result.Should().BeEquivalentTo(new
        {
            user.Id,
            Name = user.Username,
            user.Email,
            user.Phone,
            user.Role,
            user.Status
        });
    }

    /// <summary>
    /// Tests that the API response copies every field of the sign-up result.
    /// </summary>
    [Fact(DisplayName = "Given a CreateUserResult When mapping it to CreateUserResponse Then every field is copied")]
    public void Given_CreateUserResult_When_MappingToCreateUserResponse_Then_EveryFieldIsCopied()
    {
        // Given
        var result = new CreateUserResult
        {
            Id = Guid.NewGuid(),
            Name = "maria.silva",
            Email = "maria.silva@example.com",
            Phone = "+5511987654321",
            Role = UserRole.Admin,
            Status = UserStatus.Active
        };

        // When
        var response = _mapper.Map<CreateUserResponse>(result);

        // Then
        response.Should().BeEquivalentTo(result);
    }
}
