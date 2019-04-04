namespace Olympus.Athena.Storage;

/// <summary>
///     微分区异步写入器，将微分区持久化到文件系统
/// </summary>
public sealed class MicroPartitionWriter
{
    #region 字段

    private readonly string _basePath;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建微分区写入器
    /// </summary>
    /// <param name="basePath">数据存储目录路径</param>
    public MicroPartitionWriter(string basePath)
    {
        _basePath = basePath;
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     将微分区写入文件
    /// </summary>
    /// <param name="partition">要写入的微分区</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>写入任务</returns>
    public async Task WriteAsync(MicroPartition partition, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_basePath);

        var fileName = $"{partition.PartitionId}.nyar";
        var filePath = Path.Combine(_basePath, fileName);
        var data = partition.Serialize();

        await using var stream =
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await stream.WriteAsync(data, ct);
        await stream.FlushAsync(ct);
    }

    #endregion
}