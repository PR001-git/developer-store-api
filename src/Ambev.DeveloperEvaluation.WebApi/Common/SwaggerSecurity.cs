using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// Describes the API's JWT authentication in the Swagger document.
/// </summary>
public static class SwaggerSecurity
{
    /// <summary>
    /// The name the Bearer security scheme is declared and referenced under.
    /// </summary>
    internal const string BearerSchemeName = "Bearer";

    /// <summary>
    /// Declares the JWT Bearer security scheme, and applies it only to the operations that require it, so
    /// Swagger UI's <b>Authorize</b> button accepts the token from <c>POST /api/auth</c> and sends it as
    /// <c>Authorization: Bearer &lt;token&gt;</c>.
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

        options.OperationFilter<AuthorizeOperationFilter>();
    }
}

/// <summary>
/// Marks an operation as Bearer-protected in the Swagger document only when it actually requires authorization:
/// its endpoint carries <see cref="AuthorizeAttribute"/> metadata and isn't marked <see cref="AllowAnonymousAttribute"/>.
/// Without this filter, every operation would show the lock icon even though no controller has <c>[Authorize]</c> yet.
/// </summary>
public sealed class AuthorizeOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var requiresAuthorization = metadata.OfType<AuthorizeAttribute>().Any();
        var allowsAnonymous = metadata.OfType<AllowAnonymousAttribute>().Any();

        if (!requiresAuthorization || allowsAnonymous)
            return;

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SwaggerSecurity.BearerSchemeName }
            }] = Array.Empty<string>()
        });
    }
}
