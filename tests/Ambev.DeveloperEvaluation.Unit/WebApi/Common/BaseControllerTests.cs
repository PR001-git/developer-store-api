using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

/// <summary>
/// Contains unit tests for the response helpers of <see cref="BaseController"/>. Each test serializes the
/// helper's result the way MVC does and checks the exact JSON, so an envelope can't be wrapped twice.
/// </summary>
public sealed class BaseControllerTests
{
    private readonly EnvelopeController _controller = new();

    /// <summary>
    /// Tests that <c>Ok(data, message)</c> builds <c>{success, message, data}</c> once.
    /// </summary>
    [Fact(DisplayName = "Given data and a message When Ok is called Then the body is exactly {success, message, data}")]
    public void Given_DataAndMessage_When_OkIsCalled_Then_BodyIsSuccessMessageData()
    {
        // Given
        var data = new { Token = "jwt-token" };

        // When
        var result = _controller.CallOk(data, "User authenticated successfully");

        // Then
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            """{"success":true,"message":"User authenticated successfully","data":{"token":"jwt-token"}}""");
    }

    /// <summary>
    /// Tests that <c>Ok(message)</c> builds <c>{success, message}</c>, with no <c>data</c> or <c>errors</c>.
    /// </summary>
    [Fact(DisplayName = "Given only a message When Ok is called Then the body is exactly {success, message}")]
    public void Given_OnlyMessage_When_OkIsCalled_Then_BodyIsSuccessMessage()
    {
        // When
        var result = _controller.CallOk("User deleted successfully");

        // Then
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be("""{"success":true,"message":"User deleted successfully"}""");
    }

    /// <summary>
    /// Tests that <c>Created</c> returns 201 pointing at the given action, with <c>{success, message, data}</c>.
    /// </summary>
    [Fact(DisplayName = "Given data and a message When Created is called Then it returns 201 at the action with {success, message, data}")]
    public void Given_DataAndMessage_When_CreatedIsCalled_Then_Returns201WithEnvelope()
    {
        // Given
        var id = Guid.Parse("7f9c2a44-5555-4d1e-8a3b-000000000010");
        var data = new { Id = id };

        // When
        var result = _controller.CallCreated("GetSale", new { id }, data, "Sale created successfully");

        // Then
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be("GetSale");
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(id);
        MvcJson.Serialize(created.Value).Should().Be(
            """{"success":true,"message":"Sale created successfully","data":{"id":"7f9c2a44-5555-4d1e-8a3b-000000000010"}}""");
    }

    /// <summary>
    /// Tests that <c>OkPaginated</c> builds the paged envelope once: the items in <c>data</c> plus the paging fields.
    /// </summary>
    [Fact(DisplayName = "Given a page of items When OkPaginated is called Then the body is the paged envelope, not wrapped again")]
    public void Given_PageOfItems_When_OkPaginatedIsCalled_Then_BodyIsPagedEnvelope()
    {
        // Given
        var page = new PaginatedList<string>(["S-000003", "S-000004"], count: 5, pageNumber: 2, pageSize: 2);

        // When
        var result = _controller.CallOkPaginated(page, "Sales retrieved successfully");

        // Then
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        MvcJson.Serialize(ok.Value).Should().Be(
            """{"success":true,"message":"Sales retrieved successfully","data":["S-000003","S-000004"],"currentPage":2,"totalPages":3,"totalCount":5}""");
    }

    /// <summary>
    /// Exposes the protected helpers of <see cref="BaseController"/> to the tests.
    /// </summary>
    private sealed class EnvelopeController : BaseController
    {
        public IActionResult CallOk<T>(T data, string message) => Ok(data, message);

        public IActionResult CallOk(string message) => Ok(message);

        public IActionResult CallCreated<T>(string actionName, object routeValues, T data, string message) =>
            Created(actionName, routeValues, data, message);

        public IActionResult CallOkPaginated<T>(PaginatedList<T> pagedList, string message) =>
            OkPaginated(pagedList, message);
    }
}
