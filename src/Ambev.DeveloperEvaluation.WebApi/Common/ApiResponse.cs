using System.Text.Json.Serialization;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

public class ApiResponse
{
    // System.Text.Json writes a derived class's properties first; the orders keep success and message on top.
    [JsonPropertyOrder(-3)]
    public bool Success { get; set; }

    [JsonPropertyOrder(-2)]
    public string Message { get; set; } = string.Empty;
}
