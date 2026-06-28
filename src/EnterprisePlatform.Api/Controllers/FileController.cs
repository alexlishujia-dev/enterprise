using EnterprisePlatform.Api.Filters;
using EnterprisePlatform.Api.Infrastructure;
using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EnterprisePlatform.Api.Controllers;

/// <summary>文件上传。</summary>
public sealed class FileController : AuthorizedApiControllerBase
{
    private readonly IAvatarStorageService _avatarStorage;

    public FileController(IAvatarStorageService avatarStorage)
    {
        _avatarStorage = avatarStorage;
    }

    /// <summary>上传用户头像，返回可访问 URL。</summary>
    [HttpPost("avatar")]
    [RequirePermission("system.users:create", "system.users:edit")]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<ApiResult<string>>> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
            throw new BusinessException("请选择头像文件", ApiStatusCode.BadRequest);

        var url = await _avatarStorage.SaveAvatarAsync(file, cancellationToken);
        return Ok(ApiResult<string>.Ok(url, "上传成功"));
    }
}
