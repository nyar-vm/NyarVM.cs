namespace Atlas.Cloud.Interfaces;

/// <summary>
/// 云存储桶操作结果
/// </summary>
public sealed class BlobResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool success { get; init; }

    /// <summary>
    /// 错误消息，成功时为空
    /// </summary>
    public string? error { get; init; }

    /// <summary>
    /// 对象的 ETag 或版本标识
    /// </summary>
    public string? etag { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="etag">ETag</param>
    public static BlobResult ok(string? etag = null) => new() { success = true, etag = etag };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误消息</param>
    public static BlobResult fail(string error) => new() { success = false, error = error };
}

/// <summary>
/// 云存储单个对象的信息
/// </summary>
public sealed class BlobInfo
{
    /// <summary>
    /// 对象键名
    /// </summary>
    public string key { get; init; } = string.Empty;

    /// <summary>
    /// 对象大小（字节）
    /// </summary>
    public long size { get; init; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime last_modified { get; init; }

    /// <summary>
    /// ETag
    /// </summary>
    public string? etag { get; init; }
}

/// <summary>
/// 对象存储服务抽象接口，统一阿里云 OSS、腾讯云 COS、AWS S3 等云存储操作
/// </summary>
public interface IBlobStorage
{
    /// <summary>
    /// 上传对象到存储桶
    /// </summary>
    /// <param name="bucket">存储桶名称</param>
    /// <param name="key">对象键名</param>
    /// <param name="data">对象数据</param>
    /// <param name="contentType">内容类型</param>
    /// <param name="cancel">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<BlobResult> put_object(
        string bucket, string key, byte[] data, string? contentType = null,
        CancellationToken cancel = default);

    /// <summary>
    /// 从存储桶下载对象
    /// </summary>
    /// <param name="bucket">存储桶名称</param>
    /// <param name="key">对象键名</param>
    /// <param name="cancel">取消令牌</param>
    /// <returns>对象数据，不存在时返回 null</returns>
    Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default);

    /// <summary>
    /// 删除对象
    /// </summary>
    /// <param name="bucket">存储桶名称</param>
    /// <param name="key">对象键名</param>
    /// <param name="cancel">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<BlobResult> delete_object(string bucket, string key, CancellationToken cancel = default);

    /// <summary>
    /// 列出存储桶中的对象
    /// </summary>
    /// <param name="bucket">存储桶名称</param>
    /// <param name="prefix">对象键前缀过滤</param>
    /// <param name="maxKeys">最大返回数量</param>
    /// <param name="cancel">取消令牌</param>
    /// <returns>对象信息列表</returns>
    Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default);

    /// <summary>
    /// 生成预签名 URL，用于临时访问
    /// </summary>
    /// <param name="bucket">存储桶名称</param>
    /// <param name="key">对象键名</param>
    /// <param name="expiry">过期时间</param>
    /// <param name="method">HTTP 方法</param>
    /// <returns>预签名 URL</returns>
    string generate_presigned_url(string bucket, string key, TimeSpan expiry, string method = "GET");
}