using EnterprisePlatform.Core.Common;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnterprisePlatform.Api.Swagger;

/// <summary>
/// 为分页结果 PagedResult&lt;T&gt; 生成完整 Schema。
/// </summary>
public sealed class PagedResultSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsGenericType || context.Type.GetGenericTypeDefinition() != typeof(PagedResult<>))
            return;

        var itemType = context.Type.GetGenericArguments()[0];
        schema.Type = "object";
        schema.Properties = new Dictionary<string, OpenApiSchema>
        {
            ["items"] = new OpenApiSchema
            {
                Type = "array",
                Items = context.SchemaGenerator.GenerateSchema(itemType, context.SchemaRepository)
            },
            ["total"] = new OpenApiSchema { Type = "integer", Format = "int64" },
            ["pageIndex"] = new OpenApiSchema { Type = "integer", Format = "int32" },
            ["pageSize"] = new OpenApiSchema { Type = "integer", Format = "int32" },
            ["totalPages"] = new OpenApiSchema { Type = "integer", Format = "int32" }
        };
        schema.Required = new HashSet<string> { "items", "total", "pageIndex", "pageSize", "totalPages" };
    }
}
