using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Middleware;

/// <summary>
/// Contains unit tests for the <see cref="ExceptionHandlingMiddleware"/> class:
/// one test per row of the error table in the Sales API design spec (§7.4).
/// </summary>
public sealed class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();

    /// <summary>
    /// Tests that a request that doesn't throw passes through untouched.
    /// </summary>
    [Fact(DisplayName = "Given a request that succeeds When the middleware runs Then the response is left untouched")]
    public async Task Given_RequestThatSucceeds_When_MiddlewareRuns_Then_ResponseIsUntouched()
    {
        // Given
        var middleware = new ExceptionHandlingMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            _logger);
        var context = CreateContext();

        // When
        await middleware.InvokeAsync(context);

        // Then
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        (await ReadBodyAsync(context)).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a <see cref="ValidationException"/> becomes a 400 listing every failure.
    /// </summary>
    [Fact(DisplayName = "Given a ValidationException When the middleware handles it Then it returns 400 ValidationError with each failure as Property: message")]
    public async Task Given_ValidationException_When_Handled_Then_Returns400ValidationError()
    {
        // Given
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Email", "Invalid email format"),
            new ValidationFailure("Password", "Password is required")
        });

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status400BadRequest,
            """{"type":"ValidationError","error":"Invalid input data","detail":"Email: Invalid email format; Password: Password is required"}""");
    }

    /// <summary>
    /// Tests that an <see cref="UnauthorizedAccessException"/> becomes a 401 with the exception message.
    /// </summary>
    [Fact(DisplayName = "Given an UnauthorizedAccessException When the middleware handles it Then it returns 401 AuthenticationError with the message")]
    public async Task Given_UnauthorizedAccessException_When_Handled_Then_Returns401AuthenticationError()
    {
        // Given
        var exception = new UnauthorizedAccessException("Invalid credentials");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status401Unauthorized,
            """{"type":"AuthenticationError","error":"Authentication failed","detail":"Invalid credentials"}""");
    }

    /// <summary>
    /// Tests that a <see cref="KeyNotFoundException"/> becomes a 404 with the exception message.
    /// </summary>
    [Fact(DisplayName = "Given a KeyNotFoundException When the middleware handles it Then it returns 404 ResourceNotFound with the message")]
    public async Task Given_KeyNotFoundException_When_Handled_Then_Returns404ResourceNotFound()
    {
        // Given
        var exception = new KeyNotFoundException("The sale with ID 7f9c2a44-5555-4d1e-8a3b-000000000010 does not exist");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status404NotFound,
            """{"type":"ResourceNotFound","error":"Resource not found","detail":"The sale with ID 7f9c2a44-5555-4d1e-8a3b-000000000010 does not exist"}""");
    }

    /// <summary>
    /// Tests that a <see cref="DomainException"/> becomes a 409 with the exception message,
    /// escaped the way MVC escapes success bodies (the apostrophe stays as it is).
    /// </summary>
    [Fact(DisplayName = "Given a DomainException When the middleware handles it Then it returns 409 BusinessRuleViolation with the message")]
    public async Task Given_DomainException_When_Handled_Then_Returns409BusinessRuleViolation()
    {
        // Given
        var exception = new DomainException("It's not possible to sell above 20 identical items");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status409Conflict,
            """{"type":"BusinessRuleViolation","error":"Business rule violation","detail":"It's not possible to sell above 20 identical items"}""");
    }

    /// <summary>
    /// Tests that a <see cref="DbUpdateConcurrencyException"/> becomes a 409 with a fixed detail.
    /// </summary>
    [Fact(DisplayName = "Given a DbUpdateConcurrencyException When the middleware handles it Then it returns 409 ConcurrencyConflict with the fixed detail")]
    public async Task Given_DbUpdateConcurrencyException_When_Handled_Then_Returns409ConcurrencyConflict()
    {
        // Given
        var exception = new DbUpdateConcurrencyException("The database operation was expected to affect 1 row(s), but actually affected 0 row(s).");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status409Conflict,
            """{"type":"ConcurrencyConflict","error":"Concurrent modification","detail":"The sale was changed by another request. Reload it and try again."}""");
    }

    /// <summary>
    /// Tests that any other exception becomes a 500 whose body reveals nothing about it.
    /// </summary>
    [Fact(DisplayName = "Given an unexpected exception When the middleware handles it Then it returns 500 InternalServerError without the exception details")]
    public async Task Given_UnexpectedException_When_Handled_Then_Returns500WithoutDetails()
    {
        // Given
        var exception = new InvalidOperationException("Host=db;Password=secret");

        // When
        var context = await HandleAsync(exception);

        // Then
        await ShouldBeErrorAsync(context, StatusCodes.Status500InternalServerError,
            """{"type":"InternalServerError","error":"Internal server error","detail":"An unexpected error occurred."}""");
    }

    /// <summary>
    /// Tests that an unexpected exception is logged as an error, together with the exception and its stack trace.
    /// </summary>
    [Fact(DisplayName = "Given an unexpected exception When the middleware handles it Then it logs the exception as an error")]
    public async Task Given_UnexpectedException_When_Handled_Then_LogsItAsError()
    {
        // Given
        var exception = new InvalidOperationException("Host=db;Password=secret");

        // When
        await HandleAsync(exception);

        // Then
        var logCall = _logger.ReceivedCalls().Should().ContainSingle(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Subject;
        logCall.GetArguments()[0].Should().Be(LogLevel.Error);
        logCall.GetArguments()[3].Should().BeSameAs(exception);
    }

    private async Task<HttpContext> HandleAsync(Exception exception)
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, _logger);
        var context = CreateContext();
        await middleware.InvokeAsync(context);
        return context;
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private static async Task ShouldBeErrorAsync(HttpContext context, int statusCode, string body)
    {
        context.Response.StatusCode.Should().Be(statusCode);
        context.Response.ContentType.Should().Be("application/json; charset=utf-8");
        (await ReadBodyAsync(context)).Should().Be(body);
    }
}
