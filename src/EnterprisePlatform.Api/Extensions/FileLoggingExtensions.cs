using EnterprisePlatform.Api.Logging;
using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Utils.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EnterprisePlatform.Api.Extensions;

public static class FileLoggingExtensions
{
    public static IServiceCollection AddPlatformFileLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileLogOptions>(configuration.GetSection(FileLogOptions.SectionName));

        services.AddSingleton<IFileLogWriter>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FileLogOptions>>().Value;
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var directory = Path.IsPathRooted(options.Directory)
                ? options.Directory
                : Path.Combine(environment.ContentRootPath, options.Directory);

            return new FileLogWriter(directory, options);
        });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
        return services;
    }
}
