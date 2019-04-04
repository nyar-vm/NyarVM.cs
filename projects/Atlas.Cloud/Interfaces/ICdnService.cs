using Atlas.Cloud.Models;

namespace Atlas.Cloud.Interfaces;

/// <summary>
/// CDN 管理服务抽象接口
/// </summary>
public interface ICdnService
{
    /// <summary>
    /// 刷新 CDN 缓存
    /// </summary>
    /// <param name="urls">需要刷新的 URL 列表</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<CdnResult> purge(string[] urls, CancellationToken ct = default);

    /// <summary>
    /// 预热 CDN 缓存
    /// </summary>
    /// <param name="urls">需要预热的 URL 列表</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<CdnResult> prefetch(string[] urls, CancellationToken ct = default);
}
