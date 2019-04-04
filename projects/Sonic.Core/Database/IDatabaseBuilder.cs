namespace Core.Database;

/// <summary>
///     数据库构建器接口
/// </summary>
public interface IDatabaseBuilder
{
    /// <summary>
    ///     构建数据库实例
    /// </summary>
    IDatabase Build();
}