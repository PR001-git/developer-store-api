using System.Text.Encodings.Web;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

/// <summary>
/// Turns every exception that escapes the rest of the pipeline into a <c>{type, error, detail}</c> response,
/// following the error table in the Sales API design spec (§7.4).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private const string ConcurrencyConflictDetail = "The sale was changed by another request. Reload it and try again.";
    private const string InternalServerErrorDetail = "An unexpected error occurred.";

    /// <summary>
    /// The web defaults (camelCase) with the relaxed escaping that MVC's JSON output uses, so error and success
    /// bodies escape text the same way: an apostrophe is written as it is, not as a Unicode escape sequence.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger that records unexpected exceptions.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Runs the rest of the pipeline and, if it throws, writes the matching error response.
    /// Unexpected exceptions are logged with their stack trace; the response never includes it.
    /// </summary>
    /// <param name="context">The HTTP context of the current request.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var (statusCode, body) = ToErrorResponse(exception);

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(body, JsonOptions);
        }
    }

    private static (int StatusCode, ApiErrorResponse Body) ToErrorResponse(Exception exception) => exception switch
    {
        ValidationException validation => (StatusCodes.Status400BadRequest,
            ApiErrorResponse.ValidationError(validation.Errors.Select(failure => (failure.PropertyName, failure.ErrorMessage)))),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
            new ApiErrorResponse("AuthenticationError", "Authentication failed", exception.Message)),
        KeyNotFoundException => (StatusCodes.Status404NotFound,
            new ApiErrorResponse("ResourceNotFound", "Resource not found", exception.Message)),
        DomainException => (StatusCodes.Status409Conflict,
            new ApiErrorResponse("BusinessRuleViolation", "Business rule violation", exception.Message)),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            new ApiErrorResponse("ConcurrencyConflict", "Concurrent modification", ConcurrencyConflictDetail)),
        _ => (StatusCodes.Status500InternalServerError,
            new ApiErrorResponse("InternalServerError", "Internal server error", InternalServerErrorDetail))
    };
}
