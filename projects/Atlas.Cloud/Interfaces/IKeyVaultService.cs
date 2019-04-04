namespace Atlas.Cloud.Interfaces;

/// <summary>
/// 密钥管理服务抽象接口，统一阿里云 KMS、AWS KMS 等操作
/// </summary>
public interface IKeyVaultService
{
    /// <summary>
    /// 获取密钥值
    /// </summary>
    /// <param name="keyName">密钥名称</param>
    /// <param name="cancel">取消令牌</param>
    /// <returns>密钥值，不存在时返回 null</returns>
    Task<string?> get_secret(string keyName, CancellationToken cancel = default);

    /// <summary>
    /// 设置密钥值
    /// </summary>
    /// <param name="keyName">密钥名称</param>
    /// <param name="value">密钥值</param>
    /// <param name="cancel">取消令牌</param>
    Task set_secret(string keyName, string value, CancellationToken cancel = default);

    /// <summary>
    /// 删除密钥
    /// </summary>
    /// <param name="keyName">密钥名称</param>
    /// <param name="cancel">取消令牌</param>
    Task delete_secret(string keyName, CancellationToken cancel = default);
}