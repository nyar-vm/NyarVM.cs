namespace Std.Security.Authorization.Attributes;

/// <summary>
///     标记路由要求特定角色。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasRoleAttribute : Attribute
{
    /// <summary>
    ///     初始化角色要求属性。
    /// </summary>
    /// <param name="roles">要求的角色列表</param>
    public HasRoleAttribute(params string[] roles)
    {
        this.roles = roles;
    }

    /// <summary>
    ///     要求的角色。
    /// </summary>
    public string[] roles { get; }
}

/// <summary>
///     标记路由要求特定权限。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAttribute : Attribute
{
    /// <summary>
    ///     初始化权限要求属性。
    /// </summary>
    /// <param name="permissionName">权限名称</param>
    public PermissionAttribute(string permissionName)
    {
        permission_name = permissionName;
    }

    /// <summary>
    ///     要求的权限名称。
    /// </summary>
    public string permission_name { get; }
}

/// <summary>
///     标记路由要求对指定资源的操作权限。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ResourceActionAttribute : Attribute
{
    /// <summary>
    ///     初始化资源操作要求属性。
    /// </summary>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    public ResourceActionAttribute(string resource, string action)
    {
        this.resource = resource;
        this.action = action;
    }

    /// <summary>
    ///     要求的资源。
    /// </summary>
    public string resource { get; }

    /// <summary>
    ///     要求的操作。
    /// </summary>
    public string action { get; }
}