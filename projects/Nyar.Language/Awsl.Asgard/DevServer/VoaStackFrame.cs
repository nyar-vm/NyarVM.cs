namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     堆栈帧信息
/// </summary>
public sealed class VoaStackFrame
{
    public string function_name { get; set; } = string.Empty;
    public string? file_path { get; set; }
    public int line { get; set; }
    public int column { get; set; }
    public bool is_native { get; set; }
}
