namespace Nyar.Assembler;

/// <summary>
///     元编译输出规范基类。
/// </summary>
public abstract record OutputSpec
{
    /// <summary>
    ///     目标平台数据结构（非强类型）
    /// </summary>
    public abstract object? raw_data { get; }

    /// <summary>
    ///     目标文件扩展名
    /// </summary>
    public string file_extension { get; init; } = "";

    /// <summary>
    ///     输出媒介类型
    /// </summary>
    public string media_type { get; init; } = "";

    /// <summary>
    ///     是否偏向文本输出。
    /// </summary>
    public bool generate_text_output { get; init; }

    /// <summary>
    ///     后端偏好的输出文件名（不含扩展名）。
    ///     当后端希望使用入口函数名而非模块名作为输出文件时设置。
    ///     如果为 null 或空，下游应使用模块名。
    /// </summary>
    public string? output_name { get; init; }

    /// <summary>
    ///     附加产物清单
    /// </summary>
    public IReadOnlyList<AssemblerAsset> assets { get; init; } = [];
}

/// <summary>
///     元编译输出规范。
///     根包只负责定义强类型输出载体，不负责具体二进制编码；
///     具体编码应由下游平台包或 Acorn 生态完成。
/// </summary>
/// <typeparam name="TOutput">目标平台数据结构类型</typeparam>
public sealed record OutputSpec<TOutput> : OutputSpec
{
    /// <summary>
    ///     目标平台数据结构
    /// </summary>
    public TOutput data { get; init; } = default!;


    /// <inheritdoc />
    public override object? raw_data => data;
}