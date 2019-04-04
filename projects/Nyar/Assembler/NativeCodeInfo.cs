namespace Nyar.Assembler;

/// <summary>
///     Native AOT 代码生成的中间信息，承载扫描遍产生的结果的
///     供代码生成遍使用。模的WASM 后端的<c>WasiPrintInfo</c>的
/// </summary>
public sealed class NativeCodeInfo
{
    /// <summary>
    ///     是否包含任何 Print/Println 调用
    /// </summary>
    public bool has_prints { get; set; }

    /// <summary>
    ///     字符串常量索的的字符串内的
    /// </summary>
    public Dictionary<int, string> const_idx_to_string { get; } = new();

    /// <summary>
    ///     字符串内的的.data 段偏的
    /// </summary>
    public Dictionary<string, uint> string_to_offset { get; } = new();

    /// <summary>
    ///     .data 段下一个可用偏的
    /// </summary>
    public uint next_data_offset { get; set; }

    /// <summary>
    ///     需要的 Windows API 导入（仅 PE 格式时使用）
    /// </summary>
    public HashSet<string> required_win32_apis { get; } = [];

    /// <summary>
    ///     .data 段中每个字符串的字节长度信息的
    ///     使用 (offset, byteLength) 追踪，供 RIP-relative 寻址使用的
    /// </summary>
    public List<(int Offset, int ByteLength)> string_layout { get; } = [];
}