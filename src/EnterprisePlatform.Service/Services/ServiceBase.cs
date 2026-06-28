using EnterprisePlatform.Utils.Logging;

namespace EnterprisePlatform.Service.Services;

/// <summary>
/// 业务服务基类，统一捕获并记录非业务异常。
/// </summary>
public abstract class ServiceBase
{
    protected ServiceBase(IFileLogWriter fileLogWriter)
    {
        FileLogWriter = fileLogWriter;
    }

    protected IFileLogWriter FileLogWriter { get; }

    protected string ServiceName => GetType().Name;

    protected async Task<T> ExecuteAsync<T>(string operation, Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            if (!ExceptionLogger.IsDatabaseException(ex))
                ExceptionLogger.Log(FileLogWriter, "Service", ServiceName, operation, ex);
            throw;
        }
    }

    protected async Task ExecuteAsync(string operation, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            if (!ExceptionLogger.IsDatabaseException(ex))
                ExceptionLogger.Log(FileLogWriter, "Service", ServiceName, operation, ex);
            throw;
        }
    }
}
