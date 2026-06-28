using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnterprisePlatform.Api.Swagger;

/// <summary>
/// 使用完整类型名作为 SchemaId，避免不同命名空间下同名类型冲突导致 Swagger 生成失败。
/// </summary>
public static class SwaggerSchemaHelper
{
    public static string GetSchemaId(Type type)
    {
        if (type.IsGenericType)
        {
            var genericName = type.Name.Split('`')[0];
            var args = string.Join("_", type.GetGenericArguments().Select(GetSchemaId));
            var ns = type.Namespace?.Replace('.', '_') ?? "Global";
            return $"{ns}_{genericName}_{args}";
        }

        return type.FullName?.Replace("+", ".").Replace(".", "_") ?? type.Name;
    }
}

public sealed class SwaggerDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Info.Description += " | 统一响应格式 ApiResult&lt;T&gt;：code / message / data / traceId";
    }
}
