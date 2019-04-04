using Core.Security.Authentication;

namespace Core.Security.Authorization;

/// <summary>
///     授权策略接口
/// </summary>
public interface IAuthorizationPolicy
{
    /// <summary>
    ///     评估主体是否有权访问指定资源
    /// </summary>
    /// <param name="principal">声明主体</param>
    /// <param name="resource">受保护的资源标识</param>
    /// <returns>是否授权通过</returns>
    bool evaluate(IClaimsPrincipal principal, string resource);
}