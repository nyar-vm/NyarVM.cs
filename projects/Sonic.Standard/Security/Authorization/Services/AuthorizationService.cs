using Std.Security.Authorization.Models;

namespace Std.Security.Authorization.Services;

/// <summary>
///     授权服务实现，基于内存字典存储权限和角色数据。
/// </summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly Dictionary<string, Permission> _permissions = new();
    private readonly List<RolePermission> _role_permissions = [];
    private readonly Dictionary<string, Role> _roles = new();
    private readonly List<UserPermission> _user_permissions = [];
    private readonly List<UserRole> _user_roles = [];

    /// <inheritdoc />
    public async Task<bool> has_permission(string userId, string permissionName, CancellationToken ct = default)
    {
        var userPermissions = await get_user_permissions(userId, ct);
        return userPermissions.Any(p => p.name == permissionName && p.is_enabled);
    }

    /// <inheritdoc />
    public async Task<bool> has_permission(string userId, string resource, string action,
        CancellationToken ct = default)
    {
        var userPermissions = await get_user_permissions(userId, ct);
        return userPermissions.Any(p => p.resource == resource && p.action == action && p.is_enabled);
    }

    /// <inheritdoc />
    public async Task<bool> has_role(string userId, string roleName, CancellationToken ct = default)
    {
        var roles = await get_user_roles(userId, ct);
        return roles.Any(r => r.name == roleName && r.is_enabled);
    }

    /// <inheritdoc />
    public async Task<bool> has_any_role(string userId, params string[] roleNames)
    {
        var roles = await get_user_roles(userId);
        var userRoleNames = roles.Where(r => r.is_enabled).Select(r => r.name).ToHashSet();
        return roleNames.Any(r => userRoleNames.Contains(r));
    }

    /// <inheritdoc />
    public async Task<bool> has_all_roles(string userId, params string[] roleNames)
    {
        var roles = await get_user_roles(userId);
        var userRoleNames = roles.Where(r => r.is_enabled).Select(r => r.name).ToHashSet();
        return roleNames.All(r => userRoleNames.Contains(r));
    }

    /// <inheritdoc />
    public Task<Permission> create_permission(Permission permission, CancellationToken ct = default)
    {
        _permissions[permission.id] = permission;
        return Task.FromResult(permission);
    }

    /// <inheritdoc />
    public Task<Permission?> get_permission(string permissionId, CancellationToken ct = default)
    {
        _permissions.TryGetValue(permissionId, out var permission);
        return Task.FromResult(permission);
    }

    /// <inheritdoc />
    public Task<Permission?> get_permission_by_name(string name, CancellationToken ct = default)
    {
        var permission = _permissions.Values.FirstOrDefault(p => p.name == name);
        return Task.FromResult(permission);
    }

    /// <inheritdoc />
    public Task<List<Permission>> get_all_permissions(CancellationToken ct = default)
    {
        return Task.FromResult(_permissions.Values.ToList());
    }

    /// <inheritdoc />
    public Task delete_permission(string permissionId, CancellationToken ct = default)
    {
        _permissions.Remove(permissionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Role> create_role(Role role, CancellationToken ct = default)
    {
        _roles[role.id] = role;
        return Task.FromResult(role);
    }

    /// <inheritdoc />
    public Task<Role?> get_role(string roleId, CancellationToken ct = default)
    {
        _roles.TryGetValue(roleId, out var role);
        return Task.FromResult(role);
    }

    /// <inheritdoc />
    public Task<Role?> get_role_by_name(string name, CancellationToken ct = default)
    {
        var role = _roles.Values.FirstOrDefault(r => r.name == name);
        return Task.FromResult(role);
    }

    /// <inheritdoc />
    public Task<List<Role>> get_all_roles(CancellationToken ct = default)
    {
        return Task.FromResult(_roles.Values.ToList());
    }

    /// <inheritdoc />
    public Task delete_role(string roleId, CancellationToken ct = default)
    {
        _roles.Remove(roleId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task assign_role_to_user(string userId, string roleId, CancellationToken ct = default)
    {
        _user_roles.Add(new UserRole { user_id = userId, role_id = roleId });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task remove_role_from_user(string userId, string roleId, CancellationToken ct = default)
    {
        _user_roles.RemoveAll(ur => ur.user_id == userId && ur.role_id == roleId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<List<Role>> get_user_roles(string userId, CancellationToken ct = default)
    {
        var roleIds = _user_roles
            .Where(ur => ur.user_id == userId)
            .Select(ur => ur.role_id)
            .ToHashSet();

        var roles = _roles.Values.Where(r => roleIds.Contains(r.id)).ToList();
        return Task.FromResult(roles);
    }

    /// <inheritdoc />
    public Task assign_permission_to_role(string roleId, string permissionId, CancellationToken ct = default)
    {
        _role_permissions.Add(new RolePermission { role_id = roleId, permission_id = permissionId });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task remove_permission_from_role(string roleId, string permissionId, CancellationToken ct = default)
    {
        _role_permissions.RemoveAll(rp => rp.role_id == roleId && rp.permission_id == permissionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<List<Permission>> get_role_permissions(string roleId, CancellationToken ct = default)
    {
        var permissionIds = _role_permissions
            .Where(rp => rp.role_id == roleId)
            .Select(rp => rp.permission_id)
            .ToHashSet();

        var permissions = _permissions.Values.Where(p => permissionIds.Contains(p.id)).ToList();
        return Task.FromResult(permissions);
    }

    /// <inheritdoc />
    public Task assign_permission_to_user(string userId, string permissionId, CancellationToken ct = default)
    {
        _user_permissions.Add(new UserPermission { user_id = userId, permission_id = permissionId });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task remove_permission_from_user(string userId, string permissionId, CancellationToken ct = default)
    {
        _user_permissions.RemoveAll(up => up.user_id == userId && up.permission_id == permissionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<List<Permission>> get_user_permissions(string userId, CancellationToken ct = default)
    {
        var directPermissions = get_direct_user_permissions(userId);
        var rolePermissions = await get_user_role_permissions(userId, ct);

        var allPermissions = directPermissions.Concat(rolePermissions)
            .GroupBy(p => p.id)
            .Select(g => g.First())
            .ToList();

        return allPermissions;
    }

    private List<Permission> get_direct_user_permissions(string userId)
    {
        var permissionIds = _user_permissions
            .Where(up => up.user_id == userId)
            .Select(up => up.permission_id)
            .ToHashSet();

        return [.. _permissions.Values.Where(p => permissionIds.Contains(p.id))];
    }

    private async Task<List<Permission>> get_user_role_permissions(string userId, CancellationToken ct)
    {
        var roles = await get_user_roles(userId, ct);
        var permissions = new List<Permission>();

        foreach (var role in roles)
        {
            var rolePermissions = await get_role_permissions(role.id, ct);
            permissions.AddRange(rolePermissions);
        }

        return permissions;
    }
}