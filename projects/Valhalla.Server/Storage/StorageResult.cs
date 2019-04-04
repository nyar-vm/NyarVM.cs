namespace Valhalla.Server.Storage;

/// <summary>
///     二进制存储操作结果
/// </summary>
public class StorageResult
{
    /// <summary>是否成功</summary>
    public bool success { get; set; }

    /// <summary>错误消息</summary>
    public string? error { get; set; }

    /// <summary>创建成功结果</summary>
    public static StorageResult succeed()
    {
        return new StorageResult { success = true };
    }

    /// <summary>创建失败结果</summary>
    public static StorageResult fail(string error)
    {
        return new StorageResult { success = false, error = error };
    }
}