using System.Data.Common;

namespace EnterprisePlatform.Repository.Abstractions;

/// <summary>
/// 事务作用域。
/// </summary>
public interface ITransactionScope : IAsyncDisposable
{
    DbTransaction Transaction { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
