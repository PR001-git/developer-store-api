using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

/// <summary>
/// Contains unit tests for <see cref="InvalidModelStateResponseFactory"/>, which shapes model-binding failures
/// (malformed JSON, bad route or query values) as <c>{type, error, detail}</c>.
/// </summary>
public sealed class InvalidModelStateResponseFactoryTests
{
    /// <summary>
    /// Tests that every binding error is listed as <c>Key: message</c>, sorted by key and joined with <c>; </c>, in a JSON 400.
    /// </summary>
    [Fact(DisplayName = "Given binding errors When the factory builds the response Then it returns 400 ValidationError listing each as Key: message, sorted by key")]
    public void Given_BindingErrors_When_ResponseIsBuilt_Then_Returns400ValidationError()
    {
        // Given
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("_page", "The value 'abc' is not valid for _page.");
        modelState.AddModelError("$.quantity", "The JSON value could not be converted to System.Int32. Path: $.quantity | LineNumber: 0 | BytePositionInLine: 16.");

        // When
        var result = InvalidModelStateResponseFactory.Create(CreateActionContext(modelState));

        // Then
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        badRequest.ContentTypes.Should().Equal("application/json");
        MvcJson.Serialize(badRequest.Value).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"$.quantity: The JSON value could not be converted to System.Int32. Path: $.quantity | LineNumber: 0 | BytePositionInLine: 16.; _page: The value 'abc' is not valid for _page."}""");
    }

    /// <summary>
    /// Tests that an error that carries only an exception uses ASP.NET Core's generic message, never the exception's.
    /// </summary>
    [Fact(DisplayName = "Given a binding error without a message When the factory builds the response Then it uses the generic message")]
    public void Given_BindingErrorWithoutMessage_When_ResponseIsBuilt_Then_UsesGenericMessage()
    {
        // Given
        var modelState = new ModelStateDictionary();
        modelState.TryAddModelException("id", new FormatException("Internal parser detail"));

        // When
        var result = InvalidModelStateResponseFactory.Create(CreateActionContext(modelState));

        // Then
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        MvcJson.Serialize(badRequest.Value).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"id: The input was not valid."}""");
    }

    /// <summary>
    /// Tests that an error recorded against the whole request (empty key) is listed as its message alone.
    /// </summary>
    [Fact(DisplayName = "Given a binding error without a key When the factory builds the response Then the detail is the message alone")]
    public void Given_BindingErrorWithoutKey_When_ResponseIsBuilt_Then_DetailIsMessageAlone()
    {
        // Given
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, "A non-empty request body is required.");

        // When
        var result = InvalidModelStateResponseFactory.Create(CreateActionContext(modelState));

        // Then
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        MvcJson.Serialize(badRequest.Value).Should().Be(
            """{"type":"ValidationError","error":"Invalid input data","detail":"A non-empty request body is required."}""");
    }

    private static ActionContext CreateActionContext(ModelStateDictionary modelState) =>
        new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), modelState);
}
