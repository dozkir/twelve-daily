using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TwelveDaily.Api.Swagger;

/// <summary>
/// Marks every non-nullable property as <c>required</c> in the OpenAPI schema.
/// Paired with <c>SupportNonNullableReferenceTypes</c>, this makes the generated
/// orval client treat C# non-nullable members as guaranteed (no <c>?</c>/<c>undefined</c>),
/// while nullable members (e.g. <c>string?</c>) stay optional.
/// </summary>
public sealed class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete || concrete.Properties is null)
        {
            return;
        }

        var required = concrete.Required ??= new HashSet<string>();

        foreach (var (name, property) in concrete.Properties)
        {
            // Nullable members carry the Null flag in their JSON Schema type
            // (e.g. string? -> String | Null); everything else is guaranteed.
            var isNullable = property.Type is { } type && type.HasFlag(JsonSchemaType.Null);
            if (!isNullable)
            {
                required.Add(name);
            }
        }
    }
}
