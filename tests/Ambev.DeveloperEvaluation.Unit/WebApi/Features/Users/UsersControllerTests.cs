using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Application.Users.DeleteUser;
using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestData;
using Ambev.DeveloperEvaluation.WebApi.Features.Users;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Users;

/// <summary>
/// Contains unit tests for the <see cref="UsersController"/> class.
/// </summary>
public sealed class UsersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UsersController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersControllerTests"/> class.
    /// </summary>
    public UsersControllerTests()
    {
        _controller = new UsersController(_mediator, _mapper);
    }

    /// <summary>
    /// Tests that an invalid sign-up request throws <see cref="ValidationException"/> instead of returning a raw error list.
    /// </summary>
    [Fact(DisplayName = "Given an invalid sign-up request When creating a user Then it throws ValidationException and sends no command")]
    public async Task Given_InvalidSignUpRequest_When_CreatingUser_Then_ThrowsValidationException()
    {
        // Given
        var request = UserRequestTestData.GenerateValidCreateUserRequest();
        request.Email = "not-an-email";

        // When
        var act = () => _controller.CreateUser(request, CancellationToken.None);

        // Then
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("The provided email address is not valid.");
        _mediator.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a sign-up returns 201 pointing at <c>GetUser</c>, with the envelope built once.
    /// </summary>
    [Fact(DisplayName = "Given a valid sign-up request When creating a user Then it returns 201 at GetUser with {success, message, data}")]
    public async Task Given_ValidSignUpRequest_When_CreatingUser_Then_Returns201WithEnvelope()
    {
        // Given
        var request = UserRequestTestData.GenerateValidCreateUserRequest();
        var command = new CreateUserCommand();
        var result = new CreateUserResult { Id = Guid.NewGuid() };
        _mapper.Map<CreateUserCommand>(request).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(result);
        _mapper.Map<CreateUserResponse>(result).Returns(new CreateUserResponse { Id = result.Id });

        // When
        var response = await _controller.CreateUser(request, CancellationToken.None);

        // Then
        var created = response.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UsersController.GetUser));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(result.Id);
        var body = MvcJson.SerializeToElement(created.Value);
        body.EnumerateObject().Select(property => property.Name).Should().Equal("success", "message", "data");
        body.GetProperty("message").GetString().Should().Be("User created successfully");
        body.GetProperty("data").GetProperty("id").GetGuid().Should().Be(result.Id);
    }

    /// <summary>
    /// Tests that an empty user id throws <see cref="ValidationException"/> instead of returning a raw error list.
    /// </summary>
    [Fact(DisplayName = "Given an empty user id When getting a user Then it throws ValidationException and sends no command")]
    public async Task Given_EmptyUserId_When_GettingUser_Then_ThrowsValidationException()
    {
        // When
        var act = () => _controller.GetUser(Guid.Empty, CancellationToken.None);

        // Then
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("User ID is required");
        _mediator.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that getting a user returns 200 with the envelope built once.
    /// </summary>
    [Fact(DisplayName = "Given an existing user id When getting a user Then it returns 200 with {success, message, data}")]
    public async Task Given_ExistingUserId_When_GettingUser_Then_Returns200WithEnvelope()
    {
        // Given
        var id = Guid.NewGuid();
        var command = new GetUserCommand(id);
        var result = new GetUserResult { Id = id };
        _mapper.Map<GetUserCommand>(id).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(result);
        _mapper.Map<GetUserResponse>(result).Returns(new GetUserResponse { Id = id });

        // When
        var response = await _controller.GetUser(id, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        var body = MvcJson.SerializeToElement(ok.Value);
        body.EnumerateObject().Select(property => property.Name).Should().Equal("success", "message", "data");
        body.GetProperty("message").GetString().Should().Be("User retrieved successfully");
        body.GetProperty("data").GetProperty("id").GetGuid().Should().Be(id);
    }

    /// <summary>
    /// Tests that an empty user id throws <see cref="ValidationException"/> instead of returning a raw error list.
    /// </summary>
    [Fact(DisplayName = "Given an empty user id When deleting a user Then it throws ValidationException and sends no command")]
    public async Task Given_EmptyUserId_When_DeletingUser_Then_ThrowsValidationException()
    {
        // When
        var act = () => _controller.DeleteUser(Guid.Empty, CancellationToken.None);

        // Then
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("User ID is required");
        _mediator.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that deleting a user returns 200 with <c>{success, message}</c> and nothing else.
    /// </summary>
    [Fact(DisplayName = "Given an existing user id When deleting a user Then it returns 200 with exactly {success, message}")]
    public async Task Given_ExistingUserId_When_DeletingUser_Then_Returns200WithSuccessMessage()
    {
        // Given
        var id = Guid.NewGuid();
        var command = new DeleteUserCommand(id);
        _mapper.Map<DeleteUserCommand>(id).Returns(command);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(new DeleteUserResponse { Success = true });

        // When
        var response = await _controller.DeleteUser(id, CancellationToken.None);

        // Then
        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be("""{"success":true,"message":"User deleted successfully"}""");
    }
}
