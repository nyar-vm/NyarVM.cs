namespace Std.Security.Authorization.Requirements;

/// <summary>
///     授权需求接口，所有授权需求必须实现此接口。
/// </summary>
public interface IAuthorizationRequirement
{
}

/// <summary>
///     权限需求，要求用户拥有指定名称的权限。
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     初始化权限需求。
    /// </summary>
    /// <param name="permissionName">权限名称</param>
    public PermissionRequirement(string permissionName)
    {
        permission_name = permissionName;
    }

    /// <summary>
    ///     要求的权限名称。
    /// </summary>
    public string permission_name { get; }
}

/// <summary>
///     资源操作需求，要求用户对指定资源拥有指定操作的权限。
/// </summary>
public sealed class ResourceActionRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     初始化资源操作需求。
    /// </summary>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    public ResourceActionRequirement(string resource, string action)
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

/// <summary>
///     角色需求，要求用户拥有指定名称的角色。
/// </summary>
public sealed class RoleRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     初始化角色需求。
    /// </summary>
    /// <param name="roleName">角色名称</param>
    public RoleRequirement(string roleName)
    {
        role_name = roleName;
    }

    /// <summary>
    ///     要求的角色名称。
    /// </summary>
    public string role_name { get; }
}