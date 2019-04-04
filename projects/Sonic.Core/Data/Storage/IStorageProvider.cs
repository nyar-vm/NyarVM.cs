using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Data.Storage;

/// <summary>
///     存储供应商类型。
/// </summary>
public enum StorageProviderKind
{
    /// <summary>本地文件系统</summary>
    Local,

    /// <summary>Amazon S3</summary>
    S3,

    /// <summary>阿里云 OSS</summary>
    AliyunOss,

    /// <summary>腾讯云 COS</summary>
    TencentCos
}

/// <summary>
///     存储文件信息。
/// </summary>
public sealed class StorageFileInfo
{
    /// <summary>
    ///     文件键名。
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    ///     访问 URL。
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    ///     文件大小（字节）。
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    ///     内容类型。
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    ///     最后修改时间。
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    ///     ETag 标识。
    /// </summary>
    public string ETag { get; set; } = string.Empty;
}

/// <summary>
///     持久化存储服务接口。跨实例共享，数据不会因实例回收而丢失。
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    ///     上传文件流。
    /// </summary>
    Task<StorageFileInfo> UploadAsync(string key, System.IO.Stream data, string? contentType = null,
        CancellationToken ct = default);

    /// <summary>
    ///     上传字节数据。
    /// </summary>
    Task<StorageFileInfo> UploadAsync(string key, byte[] data, string? contentType = null,
        CancellationToken ct = default);

    /// <summary>
    ///     下载文件流。
    /// </summary>
    Task<System.IO.Stream> DownloadAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     下载为字节数组。
    /// </summary>
    Task<byte[]> DownloadBytesAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     检查文件是否存在。
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     删除文件。
    /// </summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     获取文件信息。
    /// </summary>
    Task<StorageFileInfo?> GetFileInfoAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     获取文件的公开访问 URL。
    /// </summary>
    string GetUrl(string key, TimeSpan? expire = null);

    /// <summary>
    ///     列出指定前缀的文件键名。
    /// </summary>
    IAsyncEnumerable<string> ListAsync(string? prefix = null, CancellationToken ct = default);

    /// <summary>
    ///     复制文件。
    /// </summary>
    Task<StorageFileInfo> CopyAsync(string sourceKey, string destKey, CancellationToken ct = default);
}