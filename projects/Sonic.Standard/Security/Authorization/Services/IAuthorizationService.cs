using Std.Security.Authorization.Models;

namespace Std.Security.Authorization.Services;

/// <summary>
///     授权服务接口，提供权限和角色的查询与管理能力。
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    ///     检查用户是否拥有指定名称的权限。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="permissionName">权限名称</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>是否拥有权限</returns>
    Task<bool> has_permission(string userId, string permissionName, CancellationToken ct = default);

    /// <summary>
    ///     检查用户是否对指定资源拥有指定操作的权限。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>是否拥有权限</returns>
    Task<bool> has_permission(string userId, string resource, string action, CancellationToken ct = default);

    /// <summary>
    ///     检查用户是否拥有指定角色。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="roleName">角色名称</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>是否拥有角色</returns>
    Task<bool> has_role(string userId, string roleName, CancellationToken ct = default);

    /// <summary>
    ///     检查用户是否拥有任一指定角色。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="roleNames">角色名称列表</param>
    /// <returns>是否拥有任一角色</returns>
    Task<bool> has_any_role(string userId, params string[] roleNames);

    /// <summary>
    ///     检查用户是否拥有所有指定角色。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="roleNames">角色名称列表</param>
    /// <returns>是否拥有所有角色</returns>
    Task<bool> has_all_roles(string userId, params string[] roleNames);

    /// <summary>
    ///     创建权限。
    /// </summary>
    /// <param name="permission">权限实例</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>创建后的权限</returns>
    Task<Permission> create_permission(Permission permission, CancellationToken ct = default);

    /// <summary>
    ///     根据标识获取权限。
    /// </summary>
    /// <param name="permissionId">权限标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>权限实例，不存在返回 null</returns>
    Task<Permission?> get_permission(string permissionId, CancellationToken ct = default);

    /// <summary>
    ///     根据名称获取权限。
    /// </summary>
    /// <param name="name">权限名称</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>权限实例，不存在返回 null</returns>
    Task<Permission?> get_permission_by_name(string name, CancellationToken ct = default);

    /// <summary>
    ///     获取所有权限。
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<Permission>> get_all_permissions(CancellationToken ct = default);

    /// <summary>
    ///     删除权限。
    /// </summary>
    /// <param name="permissionId">权限标识</param>
    /// <param name="ct">取消令牌</param>
    Task delete_permission(string permissionId, CancellationToken ct = default);

    /// <summary>
    ///     创建角色。
    /// </summary>
    /// <param name="role">角色实例</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>创建后的角色</returns>
    Task<Role> create_role(Role role, CancellationToken ct = default);

    /// <summary>
    ///     根据标识获取角色。
    /// </summary>
    /// <param name="roleId">角色标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>角色实例，不存在返回 null</returns>
    Task<Role?> get_role(string roleId, CancellationToken ct = default);

    /// <summary>
    ///     根据名称获取角色。
    /// </summary>
    /// <param name="name">角色名称</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>角色实例，不存在返回 null</returns>
    Task<Role?> get_role_by_name(string name, CancellationToken ct = default);

    /// <summary>
    ///     获取所有角色。
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<List<Role>> get_all_roles(CancellationToken ct = default);

    /// <summary>
    ///     删除角色。
    /// </summary>
    /// <param name="roleId">角色标识</param>
    /// <param name="ct">取消令牌</param>
    Task delete_role(string roleId, CancellationToken ct = default);

    /// <summary>
    ///     为用户分配角色。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="roleId">角色标识</param>
    /// <param name="ct">取消令牌</param>
    Task assign_role_to_user(string userId, string roleId, CancellationToken ct = default);

    /// <summary>
    ///     移除用户的角色。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="roleId">角色标识</param>
    /// <param name="ct">取消令牌</param>
    Task remove_role_from_user(string userId, string roleId, CancellationToken ct = default);

    /// <summary>
    ///     获取用户的所有角色。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<List<Role>> get_user_roles(string userId, CancellationToken ct = default);

    /// <summary>
    ///     为角色分配权限。
    /// </summary>
    /// <param name="roleId">角色标识</param>
    /// <param name="permissionId">权限标识</param>
    /// <param name="ct">取消令牌</param>
    Task assign_permission_to_role(string roleId, string permissionId, CancellationToken ct = default);

    /// <summary>
    ///     移除角色的权限。
    /// </summary>
    /// <param name="roleId">角色标识</param>
    /// <param name="permissionId">权限标识</param>
    /// <param name="ct">取消令牌</param>
    Task remove_permission_from_role(string roleId, string permissionId, CancellationToken ct = default);

    /// <summary>
    ///     获取角色的所有权限。
    /// </summary>
    /// <param name="roleId">角色标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<Permission>> get_role_permissions(string roleId, CancellationToken ct = default);

    /// <summary>
    ///     为用户直接分配权限。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="permissionId">权限标识</param>
    /// <param name="ct">取消令牌</param>
    Task assign_permission_to_user(string userId, string permissionId, CancellationToken ct = default);

    /// <summary>
    ///     移除用户的直接权限。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="permissionId">权限标识</param>
    /// <param name="ct">取消令牌</param>
    Task remove_permission_from_user(string userId, string permissionId, CancellationToken ct = default);

    /// <summary>
    ///     获取用户的所有权限（包含直接权限和角色继承的权限）。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<Permission>> get_user_permissions(string userId, CancellationToken ct = default);
}