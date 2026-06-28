using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Entities;

namespace EnterprisePlatform.Repository.Abstractions;

/// <summary>
/// 通用仓储接口。
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<TEntity>> GetPagedAsync(PageQuery query, CancellationToken cancellationToken = default);

    Task<long> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<int> SoftDeleteAsync(long id, CancellationToken cancellationToken = default);
}
