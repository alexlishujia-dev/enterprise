namespace EnterprisePlatform.Core.Options;

/// <summary>本地文件存储配置。</summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>上传根目录（相对 ContentRoot）。</summary>
    public string UploadRootPath { get; set; } = "uploads";

    /// <summary>头像子目录。</summary>
    public string AvatarSubPath { get; set; } = "avatars";

    /// <summary>头像最大字节数，默认 2MB。</summary>
    public long MaxAvatarSizeBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>允许的头像扩展名（小写，含点）。</summary>
    public string[] AllowedAvatarExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
}
