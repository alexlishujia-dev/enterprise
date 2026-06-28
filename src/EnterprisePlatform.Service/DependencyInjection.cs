using EnterprisePlatform.Service.Abstractions;
using EnterprisePlatform.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnterprisePlatform.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISysUserService, SysUserService>();
        services.AddScoped<ISysRoleService, SysRoleService>();
        services.AddScoped<ISysLogService, SysLogService>();
        services.AddScoped<ISysMenuService, SysMenuService>();
        return services;
    }
}
