using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.IoC.ModuleInitializers;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.IoC;

/// <summary>
/// Contains unit tests showing that the Application validators, registered by <see cref="ApplicationModuleInitializer"/>,
/// run in the MediatR pipeline through <see cref="ValidationBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class ValidationPipelineTests
{
    /// <summary>
    /// Tests that an invalid command is rejected by <see cref="ValidationBehavior{TRequest, TResponse}"/> before its handler runs.
    /// <see cref="AuthenticateUserHandler"/> doesn't validate by itself, so only the behavior can throw the exception.
    /// </summary>
    [Fact(DisplayName = "Given an invalid login command When it is sent through MediatR Then ValidationBehavior throws ValidationException before the handler runs")]
    public async Task Given_InvalidLoginCommand_When_SentThroughMediatR_Then_ValidationBehaviorThrows()
    {
        // Given
        var userRepository = Substitute.For<IUserRepository>();
        await using var provider = BuildPipeline(userRepository);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new AuthenticateUserCommand { Email = "not-an-email", Password = "123" };

        // When
        var act = () => mediator.Send(command);

        // Then
        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Select(failure => failure.PropertyName).Should().Equal("Email", "Password");
        await userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Builds the MediatR pipeline the way <c>Program.cs</c> does, with the validators that the IoC module registers.
    /// </summary>
    private static ServiceProvider BuildPipeline(IUserRepository userRepository)
    {
        var builder = WebApplication.CreateBuilder();
        new ApplicationModuleInitializer().Initialize(builder);
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationLayer).Assembly));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddSingleton(userRepository);
        builder.Services.AddSingleton(Substitute.For<IJwtTokenGenerator>());
        return builder.Services.BuildServiceProvider();
    }
}
