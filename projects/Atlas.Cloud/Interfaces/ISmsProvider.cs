namespace Atlas.Cloud.Interfaces;

/// <summary>
/// 短信发送结果
/// </summary>
public sealed class SmsResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool success { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? error { get; init; }

    /// <summary>
    /// 平台返回的请求 ID
    /// </summary>
    public string? request_id { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    public static SmsResult ok(string? requestId = null) => new() { success = true, request_id = requestId };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误消息</param>
    public static SmsResult fail(string error) => new() { success = false, error = error };
}

/// <summary>
/// 短信服务抽象接口，统一阿里云短信、腾讯云短信等操作
/// </summary>
public interface ISmsProvider
{
    /// <summary>
    /// 发送短信
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="templateCode">短信模板代码</param>
    /// <param name="templateParams">模板参数键值对</param>
    /// <param name="signName">短信签名</param>
    /// <param name="cancel">取消令牌</param>
    /// <returns>发送结果</returns>
    Task<SmsResult> send(
        string phoneNumber, string templateCode,
        IReadOnlyDictionary<string, string>? templateParams = null,
        string? signName = null,
        CancellationToken cancel = default
        );
}