using Core.Data.Storage;

namespace Std.Data;

/// <summary>
///     存储提供者工厂，根据配置创建对应的存储提供者实例。
///     当前仅支持本地文件系统存储。
/// </summary>
public sealed class StorageProviderFactory
{
    private readonly LocalStorageOptions _options;

    /// <summary>
    ///     初始化存储提供者工厂。
    /// </summary>
    /// <param name="options">本地存储配置选项</param>
    public StorageProviderFactory(LocalStorageOptions options)
    {
        _options = options;
    }

    /// <summary>
    ///     创建本地文件系统存储提供者。
    /// </summary>
    /// <returns>存储提供者实例</returns>
    public IStorageProvider create()
    {
        return new LocalStorageProvider(_options);
    }
}

/// <summary>
///     <see cref="IStorageProvider" /> 扩展方法。
/// </summary>
public static class StorageExtensions
{
    /// <summary>
    ///     上传字符串内容到存储。
    /// </summary>
    /// <param name="provider">存储提供者</param>
    /// <param name="key">文件键名</param>
    /// <param name="content">字符串内容</param>
    /// <param name="contentType">内容类型，默认 text/plain; charset=utf-8</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>文件信息</returns>
    public static async Task<StorageFileInfo> upload_string(
        this IStorageProvider provider,
        string key,
        string content,
        string? contentType = null,
        CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return await provider.UploadAsync(key, bytes, contentType ?? "text/plain; charset=utf-8", ct);
    }

    /// <summary>
    ///     下载文件内容为字符串。
    /// </summary>
    /// <param name="provider">存储提供者</param>
    /// <param name="key">文件键名</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>字符串内容</returns>
    public static async Task<string> download_string(
        this IStorageProvider provider,
        string key,
        CancellationToken ct = default)
    {
        var bytes = await provider.DownloadBytesAsync(key, ct);
        return Encoding.UTF8.GetString(bytes);
    }
}