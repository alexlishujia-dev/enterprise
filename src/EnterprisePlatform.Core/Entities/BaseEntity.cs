namespace EnterprisePlatform.Core.Entities;

/// <summary>
/// 实体基类。
/// </summary>
public abstract class BaseEntity
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}
