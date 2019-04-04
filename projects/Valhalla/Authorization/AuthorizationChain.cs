namespace Valhalla.Authorization;

/// <summary>
///     授权链——从根组织到最终发布者的授权路径
/// </summary>
public class AuthorizationChain
{
    /// <summary>授权事件的顺序列表（从根到叶子）</summary>
    public List<AuthorizationGrant> grants { get; set; } = [];

    /// <summary>整个链的 SHA-256，用于 lock 文件记录</summary>
    public string chain_hash { get; set; } = string.Empty;

    /// <summary>获取链的根授权者</summary>
    public string? root_issuer => grants.Count > 0 ? grants[0].issuer : null;

    /// <summary>获取链的最终被授权者</summary>
    public string? leaf_grantee => grants.Count > 0 ? grants[^1].grantee : null;

    /// <summary>验证链中所有授权的连续性</summary>
    public bool is_chain_continuous()
    {
        for (var i = 1; i < grants.Count; i++)
            if (grants[i].issuer != grants[i - 1].grantee)
                return false;

        return true;
    }
}