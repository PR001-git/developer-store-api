using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

/// <summary>
/// Contains unit tests for <see cref="JwtBearerChallenge"/>, which answers a request without a valid JWT.
/// </summary>
public sealed class JwtBearerChallengeTests
{
    /// <summary>
    /// Tests that the challenge writes the 401 body of spec §7.4 instead of an empty response,
    /// keeps the <c>WWW-Authenticate: Bearer</c> header, and stops the handler from writing its own response.
    /// </summary>
    [Fact(DisplayName = "Given a JWT bearer challenge When it is answered Then it writes 401 AuthenticationError with the invalid-token body")]
    public async Task Given_JwtBearerChallenge_When_Answered_Then_Writes401WithInvalidTokenBody()
    {
        // Given
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
        var challenge = new JwtBearerChallengeContext(httpContext, scheme, new JwtBearerOptions(), new AuthenticationProperties());

        // When
        await JwtBearerChallenge.WriteResponseAsync(challenge);

        // Then
        challenge.Handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        httpContext.Response.Headers.WWWAuthenticate.ToString().Should().Be("Bearer");
        httpContext.Response.ContentType.Should().Be("application/json; charset=utf-8");
        httpContext.Response.Body.Position = 0;
        (await new StreamReader(httpContext.Response.Body).ReadToEndAsync()).Should().Be(
            """{"type":"AuthenticationError","error":"Invalid authentication token","detail":"The provided authentication token has expired or is invalid"}""");
    }

    /// <summary>
    /// Tests that configuring the JWT bearer options points the handler's challenge event at the response above.
    /// </summary>
    [Fact(DisplayName = "Given JWT bearer options When the challenge is configured Then the challenge event writes the invalid-token response")]
    public void Given_JwtBearerOptions_When_ChallengeConfigured_Then_ChallengeEventWritesInvalidTokenResponse()
    {
        // Given
        var options = new JwtBearerOptions();

        // When
        JwtBearerChallenge.Configure(options);

        // Then
        options.Events.OnChallenge.Should().Be(new Func<JwtBearerChallengeContext, Task>(JwtBearerChallenge.WriteResponseAsync));
    }
}
