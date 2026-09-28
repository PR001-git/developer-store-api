using System.Text.Json.Serialization;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

public class ApiResponseWithData<T> : ApiResponse
{
    // After success and message, before the paging fields of PaginatedResponse<T>.
    [JsonPropertyOrder(-1)]
    public T? Data { get; set; }
}
