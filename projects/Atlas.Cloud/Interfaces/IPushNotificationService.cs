using Atlas.Cloud.Models;

namespace Atlas.Cloud.Interfaces;

/// <summary>
/// 移动推送通知服务抽象接口
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// 发送推送通知
    /// </summary>
    /// <param name="request">推送请求参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>推送结果</returns>
    Task<PushResult> push(PushRequest request, CancellationToken ct = default);
}
