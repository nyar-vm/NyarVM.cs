using Std.Security.Authorization.Requirements;
using Std.Security.Authorization.Services;

namespace Std.Security.Authorization.Handlers;

/// <summary>
///     授权需求处理器接口，负责评估特定类型的授权需求。
/// </summary>
public interface IAuthorizationHandler
{
    /// <summary>
    ///     处理授权需求。
    /// </summary>
    /// <param name="context">授权上下文</param>
    /// <param name="requirement">授权需求</param>
    /// <returns>异步操作</returns>
    Task handle(AuthorizationContext context, IAuthorizationRequirement requirement);
}

/// <summary>
///     授权上下文，封装授权评估的状态。
/// </summary>
public sealed class AuthorizationContext
{
    /// <summary>
    ///     初始化授权上下文。
    /// </summary>
    /// <param name="userId">用户标识</param>
    public AuthorizationContext(string userId)
    {
        user_id = userId;
    }

    /// <summary>
    ///     用户标识。
    /// </summary>
    public string user_id { get; }

    /// <summary>
    ///     已满足的需求集合。
    /// </summary>
    public HashSet<IAuthorizationRequirement> succeeded_requirements { get; } = [];

    /// <summary>
    ///     标记需求已满足。
    /// </summary>
    /// <param name="requirement">已满足的需求</param>
    public void succeed(IAuthorizationRequirement requirement)
    {
        succeeded_requirements.Add(requirement);
    }
}

/// <summary>
///     权限需求处理器，检查用户是否拥有指定权限。
/// </summary>
public sealed class PermissionHandler : IAuthorizationHandler
{
    private readonly IAuthorizationService _authorization_service;

    /// <summary>
    ///     初始化权限需求处理器。
    /// </summary>
    /// <param name="authorizationService">授权服务</param>
    public PermissionHandler(IAuthorizationService authorizationService)
    {
        _authorization_service = authorizationService;
    }

    /// <inheritdoc />
    public async Task handle(AuthorizationContext context, IAuthorizationRequirement requirement)
    {
        if (requirement is PermissionRequirement permReq)
            if (await _authorization_service.has_permission(context.user_id, permReq.permission_name))
                context.succeed(requirement);
    }
}

/// <summary>
///     资源操作需求处理器，检查用户是否对指定资源拥有指定操作权限。
/// </summary>
public sealed class ResourceActionHandler : IAuthorizationHandler
{
    private readonly IAuthorizationService _authorization_service;

    /// <summary>
    ///     初始化资源操作需求处理器。
    /// </summary>
    /// <param name="authorizationService">授权服务</param>
    public ResourceActionHandler(IAuthorizationService authorizationService)
    {
        _authorization_service = authorizationService;
    }

    /// <inheritdoc />
    public async Task handle(AuthorizationContext context, IAuthorizationRequirement requirement)
    {
        if (requirement is ResourceActionRequirement resReq)
            if (await _authorization_service.has_permission(context.user_id, resReq.resource, resReq.action))
                context.succeed(requirement);
    }
}

/// <summary>
///     角色需求处理器，检查用户是否拥有指定角色。
/// </summary>
public sealed class RoleHandler : IAuthorizationHandler
{
    private readonly IAuthorizationService _authorization_service;

    /// <summary>
    ///     初始化角色需求处理器。
    /// </summary>
    /// <param name="authorizationService">授权服务</param>
    public RoleHandler(IAuthorizationService authorizationService)
    {
        _authorization_service = authorizationService;
    }

    /// <inheritdoc />
    public async Task handle(AuthorizationContext context, IAuthorizationRequirement requirement)
    {
        if (requirement is RoleRequirement roleReq)
            if (await _authorization_service.has_role(context.user_id, roleReq.role_name))
                context.succeed(requirement);
    }
}