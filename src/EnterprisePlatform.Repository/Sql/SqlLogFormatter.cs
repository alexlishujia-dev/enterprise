using System.Data.Common;
using System.Text;

namespace EnterprisePlatform.Repository.Sql;

/// <summary>
/// 将 SQL 与参数格式化为可写入日志的文本（转换前/转换后）。
/// </summary>
public static class SqlLogFormatter
{
    public static (string Before, string After) FormatPair(DbCommand command)
        => FormatPair(command.CommandText, EnumerateParameters(command));

    public static (string Before, string After) FormatPair(string sql, IEnumerable<DbParameter>? parameters = null)
    {
        var parameterList = parameters?.ToList() ?? [];
        return (FormatBefore(sql, parameterList), ResolveSql(sql, parameterList));
    }

    public static string Format(DbCommand command)
        => FormatPair(command).Before;

    public static string Format(string sql, IEnumerable<DbParameter>? parameters = null)
        => FormatPair(sql, parameters).Before;

    private static string FormatBefore(string sql, IReadOnlyList<DbParameter> parameters)
    {
        var builder = new StringBuilder();
        builder.AppendLine(sql.Trim());

        if (parameters.Count > 0)
        {
            builder.AppendLine("-- Parameters:");
            foreach (var parameter in parameters)
                builder.AppendLine($"  {NormalizeParameterName(parameter.ParameterName)} = {FormatValue(parameter.Value)}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string ResolveSql(string sql, IReadOnlyList<DbParameter> parameters)
    {
        if (parameters.Count == 0)
            return sql.Trim();

        var resolved = sql;
        foreach (var parameter in parameters.OrderByDescending(p => NormalizeParameterName(p.ParameterName).Length))
        {
            var name = NormalizeParameterName(parameter.ParameterName);
            resolved = resolved.Replace(name, FormatValue(parameter.Value), StringComparison.Ordinal);
        }

        return resolved.Trim();
    }

    private static IEnumerable<DbParameter> EnumerateParameters(DbCommand command)
    {
        foreach (DbParameter parameter in command.Parameters)
            yield return parameter;
    }

    private static string NormalizeParameterName(string name)
        => name.StartsWith('@') ? name : $"@{name}";

    private static string FormatValue(object? value)
    {
        if (value is null or DBNull)
            return "NULL";

        return value switch
        {
            string text => $"'{text.Replace("'", "''")}'",
            char ch => $"'{ch}'",
            DateTime dateTime => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'",
            DateTimeOffset dateTimeOffset => $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss.fff zzz}'",
            bool boolean => boolean ? "TRUE" : "FALSE",
            byte[] bytes => $"0x{Convert.ToHexString(bytes)}",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL"
        };
    }
}
