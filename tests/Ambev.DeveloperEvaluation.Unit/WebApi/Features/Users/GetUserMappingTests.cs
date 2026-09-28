using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;
using AutoMapper;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

/// <summary>
/// Contains unit tests for the get-user maps, from the stored <see cref="User"/> to <see cref="GetUserResponse"/>,
/// with the AutoMapper configuration the API registers.
/// </summary>
public sealed class GetUserMappingTests
{
    private readonly IMapper _mapper = ApiMapper.Create();

    /// <summary>
    /// Tests that the get-user result carries the user's fields, with the username as the name.
    /// </summary>
    [Fact(DisplayName = "Given a stored user When mapping it to GetUserResult Then the result carries its fields, with Username as Name")]
    public void Given_StoredUser_When_MappingToGetUserResult_Then_CarriesItsFieldsWithUsernameAsName()
    {
        // Given
        var user = UserTestData.GenerateValidUser();
        user.Id = Guid.NewGuid();

        // When
        var result = _mapper.Map<GetUserResult>(user);

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
    /// Tests that the API response copies every field of the get-user result.
    /// </summary>
    [Fact(DisplayName = "Given a GetUserResult When mapping it to GetUserResponse Then every field is copied")]
    public void Given_GetUserResult_When_MappingToGetUserResponse_Then_EveryFieldIsCopied()
    {
        // Given
        var result = new GetUserResult
        {
            Id = Guid.NewGuid(),
            Name = "maria.silva",
            Email = "maria.silva@example.com",
            Phone = "+5511987654321",
            Role = UserRole.Customer,
            Status = UserStatus.Active
        };

        // When
        var response = _mapper.Map<GetUserResponse>(result);

        // Then
        response.Should().BeEquivalentTo(result);
    }
}
