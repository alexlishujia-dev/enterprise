using EnterprisePlatform.Api.Swagger;
using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Api.Infrastructure;
using EnterprisePlatform.Utils.Security;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace EnterprisePlatform.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EnterprisePlatform API",
                Version = "v1",
                Description = "ASP.NET Core + ADO.NET 多数据库企业级通用框架。" +
                              "启用 RequestSign 时，请求需携带 X-Platform-Timestamp、X-Platform-Nonce、X-Platform-Signature 请求头。"
            });

            // 关键：避免泛型/同名类型 SchemaId 冲突
            options.CustomSchemaIds(SwaggerSchemaHelper.GetSchemaId);

            // 关键：修复 ApiResult<T> / PagedResult<T> 无法定义的问题
            options.SchemaFilter<ApiResultSchemaFilter>();
            options.SchemaFilter<PagedResultSchemaFilter>();
            options.DocumentFilter<SwaggerDocumentFilter>();

            options.SupportNonNullableReferenceTypes();
            options.UseAllOfForInheritance();
            options.DescribeAllParametersInCamelCase();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            var coreXml = Path.Combine(AppContext.BaseDirectory, "EnterprisePlatform.Core.xml");
            if (File.Exists(coreXml))
                options.IncludeXmlComments(coreXml);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT 授权，格式：Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddPlatformAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenValidator>(sp =>
        {
            var jwtOptions = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
            return new JwtTokenValidator(jwtOptions);
        });

        return services;
    }

    public static IServiceCollection AddPlatformRequestSign(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RequestSignOptions>(configuration.GetSection(RequestSignOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton<IRequestSignatureVerifier, RequestSignatureVerifier>();
        return services;
    }

    public static IServiceCollection AddPlatformCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["*"];
        services.AddCors(options =>
        {
            options.AddPolicy("PlatformCors", policy =>
            {
                if (origins.Contains("*"))
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                else
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });
        });

        return services;
    }
}
