using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Exceptions;
using EnterprisePlatform.Core.Options;
using Microsoft.Extensions.Options;

namespace EnterprisePlatform.Api.Infrastructure;

public sealed class LocalAvatarStorageService : IAvatarStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly FileStorageOptions _options;

    public LocalAvatarStorageService(IWebHostEnvironment environment, IOptions<FileStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<string> SaveAvatarAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
            throw new BusinessException("请选择头像文件", ApiStatusCode.BadRequest);

        if (file.Length > _options.MaxAvatarSizeBytes)
            throw new BusinessException($"头像大小不能超过 {_options.MaxAvatarSizeBytes / 1024 / 1024}MB", ApiStatusCode.BadRequest);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !_options.AllowedAvatarExtensions.Contains(extension))
            throw new BusinessException("仅支持 JPG、PNG、GIF、WEBP 格式头像", ApiStatusCode.BadRequest);

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("仅支持图片文件", ApiStatusCode.BadRequest);

        var avatarDir = Path.Combine(_environment.ContentRootPath, _options.UploadRootPath, _options.AvatarSubPath);
        Directory.CreateDirectory(avatarDir);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(avatarDir, fileName);

        await using (var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/{_options.AvatarSubPath}/{fileName}";
    }
}
