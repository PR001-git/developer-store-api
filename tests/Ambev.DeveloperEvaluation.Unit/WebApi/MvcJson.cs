using System.Text.Encodings.Web;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.Unit.WebApi;

/// <summary>
/// Serializes values the way the API's MVC JSON output does: web defaults (camelCase) and relaxed escaping.
/// Tests use it to compare the exact body an action result would produce.
/// </summary>
internal static class MvcJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Serializes a value as its runtime type, as MVC does for an <c>ObjectResult</c>.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(object? value) => JsonSerializer.Serialize(value, Options);

    /// <summary>
    /// Serializes a value as its runtime type into a <see cref="JsonElement"/>, for tests that check parts of a body.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON element.</returns>
    public static JsonElement SerializeToElement(object? value) => JsonSerializer.SerializeToElement(value, Options);
}
