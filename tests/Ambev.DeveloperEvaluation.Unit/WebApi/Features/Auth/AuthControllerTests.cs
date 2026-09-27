using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth;
using Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Auth;

/// <summary>
/// Contains unit tests for the <see cref="AuthController"/> class.
/// </summary>
public sealed class AuthControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AuthController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthControllerTests"/> class.
    /// </summary>
    public AuthControllerTests()
    {
        _controller = new AuthController(_mediator, _mapper);
    }

    /// <summary>
    /// Tests that a successful login returns the envelope once, with the token at <c>data.token</c>.
    /// </summary>
    [Fact(DisplayName = "Given valid credentials When authenticating Then it returns 200 with the token at data.token")]
    public async Task Given_ValidCredentials_When_Authenticating_Then_ReturnsTokenAtDataToken()
    {
        // Given
        var request = UserRequestTestData.GenerateValidAuthenticateUserRequest();
        var command = new AuthenticateUserCommand { Email = request.Email, Password = request.Password };
        var result = new AuthenticateUserResult { Token = "jwt-token", Email = "admin@example.com", Name = "admin", Role = "Admin" };
        _mapper.Map<AuthenticateUserCommand>(request).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(result);
        _mapper.Map<AuthenticateUserResponse>(result).Returns(new AuthenticateUserResponse
        {
            Token = result.Token,
            Email = result.Email,
            Name = result.Name,
            Role = result.Role
        });

        // When
        var response = await _controller.AuthenticateUser(request, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            """{"success":true,"message":"User authenticated successfully","data":{"token":"jwt-token","email":"admin@example.com","name":"admin","role":"Admin"}}""");
    }
}
