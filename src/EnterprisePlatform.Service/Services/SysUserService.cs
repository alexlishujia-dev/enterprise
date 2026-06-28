using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Exceptions;
using EnterprisePlatform.Repository.Repositories;
using EnterprisePlatform.Service.Abstractions;
using EnterprisePlatform.Utils.Excel;
using EnterprisePlatform.Utils.Logging;
using EnterprisePlatform.Utils.Security;

namespace EnterprisePlatform.Service.Services;

public sealed class SysUserService : ServiceBase, ISysUserService
{
    private readonly ISysUserRepository _userRepository;
    private readonly ISysRoleRepository _roleRepository;

    public SysUserService(
        ISysUserRepository userRepository,
        ISysRoleRepository roleRepository,
        IFileLogWriter fileLogWriter)
        : base(fileLogWriter)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public Task<SysUserDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetByIdAsync), async () =>
        {
            var entity = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (entity is null)
                return null;

            var roles = await _roleRepository.GetRolesByUserIdAsync(id, cancellationToken);
            return MapToDto(entity, roles);
        });

    public Task<PagedResult<SysUserDto>> GetPagedAsync(PageQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetPagedAsync), async () =>
        {
            var page = await _userRepository.GetPagedAsync(query, cancellationToken);
            var items = new List<SysUserDto>();

            foreach (var entity in page.Items)
            {
                var roles = await _roleRepository.GetRolesByUserIdAsync(entity.Id, cancellationToken);
                items.Add(MapToDto(entity, roles));
            }

            return new PagedResult<SysUserDto>
            {
                Items = items,
                Total = page.Total,
                PageIndex = page.PageIndex,
                PageSize = page.PageSize
            };
        });

    public Task<long> CreateAsync(SysUserCreateDto dto, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(CreateAsync), async () =>
        {
            var existing = await _userRepository.GetByUserNameAsync(dto.UserName, cancellationToken);
            if (existing is not null)
                throw new BusinessException("用户名已存在", ApiStatusCode.Conflict);

            var entity = new SysUser
            {
                UserName = dto.UserName,
                PasswordHash = HashHelper.Sha256($"{dto.Password}:{dto.UserName}"),
                DisplayName = dto.DisplayName,
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                AvatarUrl = NormalizeAvatarUrl(dto.AvatarUrl),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            return await _userRepository.InsertAsync(entity, cancellationToken);
        });

    public Task UpdateAsync(long id, SysUserUpdateDto dto, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(UpdateAsync), async () =>
        {
            var entity = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (entity is null)
                throw new BusinessException("用户不存在", ApiStatusCode.NotFound);

            entity.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim();
            entity.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            entity.IsActive = dto.IsActive;
            entity.AvatarUrl = NormalizeAvatarUrl(dto.AvatarUrl);

            if (!string.IsNullOrWhiteSpace(dto.Password))
                entity.PasswordHash = HashHelper.Sha256($"{dto.Password}:{entity.UserName}");

            var affected = await _userRepository.UpdateAsync(entity, cancellationToken);
            if (affected == 0)
                throw new BusinessException("用户不存在", ApiStatusCode.NotFound);
        });

    public Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(DeleteAsync), async () =>
        {
            var affected = await _userRepository.SoftDeleteAsync(id, cancellationToken);
            if (affected == 0)
                throw new BusinessException("用户不存在", ApiStatusCode.NotFound);
        });

    public Task<FileExportResult> ExportAsync(PageQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(ExportAsync), async () =>
        {
            var page = await GetPagedAsync(PageQuery.CreateExport(query.Keyword), cancellationToken);
            var rows = page.Items.Select(user => new Dictionary<string, object?>
            {
                ["ID"] = user.Id,
                ["用户名"] = user.UserName,
                ["显示名"] = user.DisplayName ?? string.Empty,
                ["邮箱"] = user.Email ?? string.Empty,
                ["状态"] = user.IsActive ? "启用" : "禁用",
                ["角色"] = string.Join(", ", user.Roles),
                ["创建时间"] = ExcelExportHelper.FormatDateTime(user.CreatedAt)
            });

            return new FileExportResult
            {
                Content = ExcelExportHelper.CreateWorkbook(rows, "用户列表"),
                FileName = ExcelExportHelper.BuildFileName("用户列表")
            };
        });

    private static SysUserDto MapToDto(SysUser entity, IReadOnlyList<SysRole> roles)
        => new()
        {
            Id = entity.Id,
            UserName = entity.UserName,
            DisplayName = entity.DisplayName,
            Email = entity.Email,
            AvatarUrl = entity.AvatarUrl,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            Roles = roles.Select(r => r.RoleCode).ToList()
        };

    private static string? NormalizeAvatarUrl(string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
            return null;

        var value = avatarUrl.Trim();
        return value.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ? value : null;
    }
}
