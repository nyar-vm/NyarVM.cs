namespace Valhalla.Authorization;

/// <summary>
///     单个授权事件
/// </summary>
public class AuthorizationGrant
{
    /// <summary>被授权的命名空间</summary>
    public string @namespace { get; set; } = string.Empty;

    /// <summary>被授权者公钥指纹</summary>
    public string grantee { get; set; } = string.Empty;

    /// <summary>授予的权限</summary>
    public AuthorizationPermission permissions { get; set; } = AuthorizationPermission.publish;

    /// <summary>签发时的根组织 incarnation</summary>
    public int incarnation { get; set; } = 1;

    /// <summary>授权过期时间</summary>
    public DateTime? expires_at { get; set; }

    /// <summary>签发时间</summary>
    public DateTime issued_at { get; set; } = DateTime.UtcNow;

    /// <summary>签发者公钥指纹</summary>
    public string issuer { get; set; } = string.Empty;

    /// <summary>签发者 Ed25519 签名（Base64）</summary>
    public string signature { get; set; } = string.Empty;
}