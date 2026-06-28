namespace EnterprisePlatform.Api.Filters;

/// <summary>标记 Action 或 Controller 所需的权限码（满足任一即可）。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public RequirePermissionAttribute(params string[] permissions)
    {
        if (permissions.Length == 0)
            throw new ArgumentException("至少指定一个权限码。", nameof(permissions));

        Permissions = permissions;
    }

    public IReadOnlyList<string> Permissions { get; }
}
