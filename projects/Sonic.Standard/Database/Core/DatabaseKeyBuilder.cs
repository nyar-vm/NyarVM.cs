namespace Std.Database.Core;

/// <summary>
///     键构建器，支持流畅 API 创建结构化键
/// </summary>
internal sealed class DatabaseKeyBuilder
{
    private readonly StringBuilder _builder;

    private DatabaseKeyBuilder(string prefix)
    {
        _builder = new StringBuilder(prefix);
    }

    /// <summary>
    ///     从前缀开始构建键
    /// </summary>
    /// <param name="prefix">前缀</param>
    /// <returns>键构建器</returns>
    public static DatabaseKeyBuilder from(string prefix)
    {
        return new DatabaseKeyBuilder(prefix);
    }

    /// <summary>
    ///     添加字符串段
    /// </summary>
    /// <param name="segment">段值</param>
    /// <returns>键构建器</returns>
    public DatabaseKeyBuilder append(string segment)
    {
        _builder.Append(':').Append(segment);
        return this;
    }

    /// <summary>
    ///     添加 int 段
    /// </summary>
    /// <param name="segment">段值</param>
    /// <returns>键构建器</returns>
    public DatabaseKeyBuilder append(int segment)
    {
        _builder.Append(':').Append(segment);
        return this;
    }

    /// <summary>
    ///     添加 long 段
    /// </summary>
    /// <param name="segment">段值</param>
    /// <returns>键构建器</returns>
    public DatabaseKeyBuilder append(long segment)
    {
        _builder.Append(':').Append(segment);
        return this;
    }

    /// <summary>
    ///     添加 Guid 段
    /// </summary>
    /// <param name="segment">段值</param>
    /// <returns>键构建器</returns>
    public DatabaseKeyBuilder append(Guid segment)
    {
        _builder.Append(':').Append(segment);
        return this;
    }

    /// <summary>
    ///     添加格式化段
    /// </summary>
    /// <param name="format">格式字符串</param>
    /// <param name="args">参数</param>
    /// <returns>键构建器</returns>
    public DatabaseKeyBuilder append_format(string format, params object[] args)
    {
        _builder.Append(':').AppendFormat(format, args);
        return this;
    }

    /// <summary>
    ///     构建为 DatabaseKey
    /// </summary>
    /// <returns>键实例</returns>
    public DatabaseKey build()
    {
        return DatabaseKey.from_string(_builder.ToString());
    }

    /// <summary>
    ///     隐式转换为 DatabaseKey
    /// </summary>
    /// <param name="builder">键构建器</param>
    public static implicit operator DatabaseKey(DatabaseKeyBuilder builder)
    {
        return builder.build();
    }
}