using Core.Security.Sandbox;

namespace Std.Security.Sandbox;

/// <summary>
///     沙箱上下文，提供资源访问检查能力。
/// </summary>
public static class SandboxContext
{
    private static SandboxPolicy _policy = SandboxPolicy.deny_all;

    /// <summary>
    ///     检查指定资源的访问权限是否被允许。
    /// </summary>
    /// <param name="resource">资源类型。</param>
    /// <param name="permission">访问权限。</param>
    /// <returns>如果允许访问则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool check_access(ResourceKind resource, AccessPermission permission)
    {
        if (_policy == SandboxPolicy.allow_all) return true;

        if (_policy == SandboxPolicy.deny_all) return false;

        return false;
    }

    /// <summary>
    ///     设置当前沙箱策略。
    /// </summary>
    /// <param name="policy">沙箱策略。</param>
    public static void set_policy(SandboxPolicy policy)
    {
        _policy = policy;
    }
}