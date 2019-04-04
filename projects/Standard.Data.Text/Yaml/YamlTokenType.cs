namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 词法单元类型
/// </summary>
public enum YamlTokenType
{
    /// <summary>
    ///     文档开始 ---
    /// </summary>
    document_start,

    /// <summary>
    ///     文档结束 ...
    /// </summary>
    document_end,

    /// <summary>
    ///     缩进
    /// </summary>
    indent,

    /// <summary>
    ///     键
    /// </summary>
    key,

    /// <summary>
    ///     值
    /// </summary>
    value,

    /// <summary>
    ///     序列项标记 -
    /// </summary>
    dash,

    /// <summary>
    ///     字符串值
    /// </summary>
    @string,

    /// <summary>
    ///     数字值
    /// </summary>
    number,

    /// <summary>
    ///     布尔值
    /// </summary>
    boolean,

    /// <summary>
    ///     null 值
    /// </summary>
    @null,

    /// <summary>
    ///     流式映射开始 {
    /// </summary>
    flow_map_start,

    /// <summary>
    ///     流式映射结束 }
    /// </summary>
    flow_map_end,

    /// <summary>
    ///     流式序列开始 [
    /// </summary>
    flow_seq_start,

    /// <summary>
    ///     流式序列结束 ]
    /// </summary>
    flow_seq_end,

    /// <summary>
    ///     流式逗号
    /// </summary>
    flow_comma,

    /// <summary>
    ///     冒号
    /// </summary>
    colon,

    /// <summary>
    ///     注释
    /// </summary>
    comment,

    /// <summary>
    ///     换行
    /// </summary>
    newline,

    /// <summary>
    ///     多行字符串 | 或 >
    /// </summary>
    multiline_indicator,

    /// <summary>
    ///     标签 !xxx
    /// </summary>
    tag,

    /// <summary>
    ///     锚点 &xxx
    /// </summary>
    anchor,

    /// <summary>
    ///     别名 *xxx
    /// </summary>
    alias,

    /// <summary>
    ///     文件结束
    /// </summary>
    end_of_file,

    /// <summary>
    ///     无效词法单元
    /// </summary>
    invalid
}