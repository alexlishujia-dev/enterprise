using EnterprisePlatform.Core.Common;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

namespace EnterprisePlatform.Api.Swagger;

/// <summary>
/// 修复 ApiResult&lt;T&gt; 泛型类型在 Swagger 中无法正确生成 Schema 的问题。
/// </summary>
public sealed class ApiResultSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(ApiResult))
        {
            ApplyApiResultSchema(schema, context, typeof(object));
            return;
        }

        if (context.Type.IsGenericType && context.Type.GetGenericTypeDefinition() == typeof(ApiResult<>))
        {
            var dataType = context.Type.GetGenericArguments()[0];
            ApplyApiResultSchema(schema, context, dataType);
        }
    }

    private static void ApplyApiResultSchema(OpenApiSchema schema, SchemaFilterContext context, Type dataType)
    {
        schema.Type = "object";
        schema.AdditionalPropertiesAllowed = false;
        schema.Properties = new Dictionary<string, OpenApiSchema>
        {
            ["code"] = new OpenApiSchema { Type = "integer", Format = "int32", Description = "状态码，200 表示成功" },
            ["message"] = new OpenApiSchema { Type = "string", Description = "提示信息" },
            ["traceId"] = new OpenApiSchema { Type = "string", Nullable = true, Description = "请求追踪标识" },
            ["data"] = context.SchemaGenerator.GenerateSchema(dataType, context.SchemaRepository)
        };
        schema.Required = new HashSet<string> { "code", "message" };
    }
}
