namespace EnterprisePlatform.Core.Common;

/// <summary>文件导出结果。</summary>
public sealed class FileExportResult
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    public string ContentType { get; init; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
