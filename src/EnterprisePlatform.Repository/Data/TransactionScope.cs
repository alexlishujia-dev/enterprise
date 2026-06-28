using EnterprisePlatform.Repository.Abstractions;
using System.Data.Common;

namespace EnterprisePlatform.Repository.Data;

public sealed class TransactionScope : ITransactionScope
{
    private readonly DbConnection _connection;
    private bool _completed;

    public TransactionScope(DbConnection connection)
    {
        _connection = connection;
        Transaction = connection.BeginTransaction();
    }

    public DbTransaction Transaction { get; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
            return Task.CompletedTask;

        Transaction.Commit();
        _completed = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
            return Task.CompletedTask;

        Transaction.Rollback();
        _completed = true;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
            await RollbackAsync();

        await Transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
