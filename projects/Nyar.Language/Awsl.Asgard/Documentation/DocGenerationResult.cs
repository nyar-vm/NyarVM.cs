namespace Valkyrie.Asgard.Documentation;

/// <summary>
///     文档生成结果
/// </summary>
public sealed class DocGenerationResult
{
    /// <summary>是否成功</summary>
    public bool success { get; set; }

    /// <summary>输出文件路径</summary>
    public string? output_path { get; set; }

    /// <summary>处理的文件数</summary>
    public int files_processed { get; set; }

    /// <summary>提取的函数数</summary>
    public int functions_found { get; set; }

    /// <summary>提取的结构体数</summary>
    public int structs_found { get; set; }

    /// <summary>提取的枚举数</summary>
    public int enums_found { get; set; }

    /// <summary>提取的外函数数</summary>
    public int externs_found { get; set; }
}
