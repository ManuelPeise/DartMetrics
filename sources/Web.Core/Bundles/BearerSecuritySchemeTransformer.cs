using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Web.Core.Bundles
{
    /// <summary>
    /// Adds the JWT bearer scheme to the OpenAPI document, so Swagger UI shows an "Authorize" button.
    /// </summary>
    internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
        : IOpenApiDocumentTransformer
    {
        private const string SchemeName = JwtBearerDefaults.AuthenticationScheme;

        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

            if (!authenticationSchemes.Any(scheme => scheme.Name == SchemeName))
            {
                return;
            }

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the token from /api/UserAuthentication/AuthenticateUser (without the \"Bearer \" prefix)."
            };

            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
            });
        }
    }
}
