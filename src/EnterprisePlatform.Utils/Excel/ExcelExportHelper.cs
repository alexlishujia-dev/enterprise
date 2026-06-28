using MiniExcelLibs;

namespace EnterprisePlatform.Utils.Excel;

/// <summary>Excel 导出辅助类（基于 MiniExcel）。</summary>
public static class ExcelExportHelper
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] CreateWorkbook(IEnumerable<IDictionary<string, object?>> rows, string sheetName = "Sheet1")
    {
        using var stream = new MemoryStream();
        stream.SaveAs(rows, sheetName: sheetName);
        return stream.ToArray();
    }

    public static string BuildFileName(string prefix)
        => $"{prefix}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

    public static string FormatDateTime(DateTime value)
        => value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
}
