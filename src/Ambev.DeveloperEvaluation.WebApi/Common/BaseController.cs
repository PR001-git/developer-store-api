using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    protected int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new NullReferenceException());

    protected string GetCurrentUserEmail() =>
        User.FindFirst(ClaimTypes.Email)?.Value ?? throw new NullReferenceException();

    /// <summary>
    /// Returns 200 with the body <c>{success, message, data}</c>.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    /// <param name="data">The response data. Pass the data itself, never an envelope.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A 200 result whose body is built exactly once.</returns>
    protected IActionResult Ok<T>(T data, string message) =>
        base.Ok(new ApiResponseWithData<T> { Success = true, Message = message, Data = data });

    /// <summary>
    /// Returns 200 with the body <c>{success, message}</c>, for responses without data.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A 200 result whose body is built exactly once.</returns>
    protected IActionResult Ok(string message) =>
        base.Ok(new ApiResponse { Success = true, Message = message });

    /// <summary>
    /// Returns 201 with the body <c>{success, message, data}</c> and a <c>Location</c> header that points at
    /// <paramref name="actionName"/> in the same controller.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    /// <param name="actionName">The action that reads the created resource, such as <c>nameof(GetUser)</c>.</param>
    /// <param name="routeValues">The route values of that action, such as <c>new { id }</c>.</param>
    /// <param name="data">The response data. Pass the data itself, never an envelope.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A 201 result whose body is built exactly once.</returns>
    protected IActionResult Created<T>(string actionName, object routeValues, T data, string message) =>
        base.CreatedAtAction(actionName, routeValues, new ApiResponseWithData<T> { Success = true, Message = message, Data = data });

    /// <summary>
    /// Returns 200 with the paged body: <c>{success, message, data}</c> plus <c>currentPage</c>, <c>totalPages</c> and <c>totalItems</c>.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="pagedList">The page of items and its paging information.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A 200 result whose body is built exactly once.</returns>
    protected IActionResult OkPaginated<T>(PaginatedList<T> pagedList, string message) =>
        base.Ok(new PaginatedResponse<T>
        {
            Success = true,
            Message = message,
            Data = pagedList,
            CurrentPage = pagedList.CurrentPage,
            TotalPages = pagedList.TotalPages,
            TotalItems = pagedList.TotalCount
        });
}
