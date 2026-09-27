namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// The body of every error response, <c>{type, error, detail}</c>, as described in <c>.doc/general-api.md</c>.
/// </summary>
/// <param name="Type">A machine-readable error type identifier, such as <c>ValidationError</c>.</param>
/// <param name="Error">A short, human-readable summary of the problem.</param>
/// <param name="Detail">A human-readable explanation specific to this occurrence of the problem.</param>
public sealed record ApiErrorResponse(string Type, string Error, string Detail)
{
    /// <summary>
    /// Creates the 400 <c>ValidationError</c> body. Each failure appears as <c>Property: message</c>,
    /// or as the message alone when it has no property, and the failures are joined with <c>; </c>.
    /// </summary>
    /// <param name="failures">The property name and message of each validation failure.</param>
    /// <returns>The validation error body.</returns>
    public static ApiErrorResponse ValidationError(IEnumerable<(string Property, string Message)> failures) =>
        new("ValidationError", "Invalid input data", string.Join("; ", failures.Select(FormatFailure)));

    private static string FormatFailure((string Property, string Message) failure) =>
        string.IsNullOrEmpty(failure.Property) ? failure.Message : $"{failure.Property}: {failure.Message}";
}
