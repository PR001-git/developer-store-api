using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// Answers a request that reaches an <c>[Authorize]</c> endpoint without a valid JWT (missing, badly signed or expired)
/// with the 401 body of spec §7.4, instead of the JWT bearer handler's empty response.
/// </summary>
public static class JwtBearerChallenge
{
    private static readonly ApiErrorResponse InvalidTokenBody = new(
        "AuthenticationError",
        "Invalid authentication token",
        "The provided authentication token has expired or is invalid");

    /// <summary>
    /// Makes the JWT bearer handler answer its challenges with <see cref="WriteResponseAsync"/>.
    /// </summary>
    /// <param name="options">The options of the JWT bearer scheme.</param>
    public static void Configure(JwtBearerOptions options) =>
        options.Events = new JwtBearerEvents { OnChallenge = WriteResponseAsync };

    /// <summary>
    /// Writes 401 with the <c>AuthenticationError</c> body and a <c>WWW-Authenticate: Bearer</c> header,
    /// and marks the challenge handled so the handler doesn't write its own response.
    /// </summary>
    /// <param name="context">The challenge of the JWT bearer handler.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    public static async Task WriteResponseAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;
        await context.Response.WriteAsJsonAsync(InvalidTokenBody);
    }
}
