using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// Describes the API's JWT authentication in the Swagger document.
/// </summary>
public static class SwaggerSecurity
{
    private const string BearerSchemeName = "Bearer";

    /// <summary>
    /// Declares the JWT Bearer security scheme and applies it to every operation, so Swagger UI's <b>Authorize</b> button
    /// accepts the token from <c>POST /api/auth</c> and sends it as <c>Authorization: Bearer &lt;token&gt;</c>.
    /// </summary>
    /// <param name="options">The Swagger generator options to configure.</param>
    public static void AddJwtBearer(SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(BearerSchemeName, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the token from POST /api/auth (data.token), without the Bearer prefix."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BearerSchemeName }
            }] = Array.Empty<string>()
        });
    }
}
