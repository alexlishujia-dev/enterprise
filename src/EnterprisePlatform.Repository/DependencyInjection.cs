using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Repository.Abstractions;
using EnterprisePlatform.Repository.Data;
using EnterprisePlatform.Repository.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterprisePlatform.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<ISysUserRepository, SysUserRepository>();
        services.AddScoped<ISysRoleRepository, SysRoleRepository>();
        services.AddScoped<ISysLogRepository, SysLogRepository>();
        services.AddScoped<ISysMenuRepository, SysMenuRepository>();
        return services;
    }
}
