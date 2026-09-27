using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// Builds the response that <c>[ApiController]</c> returns when model binding fails (malformed JSON,
/// a bad route or query value), so those failures share the <c>{type, error, detail}</c> body of every other error.
/// </summary>
public static class InvalidModelStateResponseFactory
{
    /// <summary>ASP.NET Core's own text for a model error that carries only an exception.</summary>
    private const string GenericErrorMessage = "The input was not valid.";

    /// <summary>
    /// Creates a 400 <c>ValidationError</c> response that lists each model-state error as <c>Key: message</c>.
    /// Keys are sorted (ordinal), because <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/>
    /// doesn't enumerate them in the order the errors were added.
    /// </summary>
    /// <param name="context">The action context whose model state is invalid.</param>
    /// <returns>A 400 result with an <see cref="ApiErrorResponse"/> body and the content type <c>application/json</c>.</returns>
    public static IActionResult Create(ActionContext context)
    {
        var failures = new List<(string Property, string Message)>();
        foreach (var (key, entry) in context.ModelState.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (entry is null)
                continue;

            foreach (var error in entry.Errors)
                failures.Add((key, string.IsNullOrEmpty(error.ErrorMessage) ? GenericErrorMessage : error.ErrorMessage));
        }

        return new BadRequestObjectResult(ApiErrorResponse.ValidationError(failures))
        {
            ContentTypes = { "application/json" }
        };
    }
}
