namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     VOA 结构化错误信息
/// </summary>
public sealed class VoaErrorInfo
{
    public string severity { get; set; } = "error";

    public string severity_label => severity switch
    {
        "error" => "错误",
        "warning" => "警告",
        "syntax" => "语法错误",
        "compile" => "编译错误",
        "runtime" => "运行时错误",
        _ => "错误"
    };

    public string title { get; set; } = "编译错误";
    public string message { get; set; } = string.Empty;
    public string? file_path { get; set; }
    public int line { get; set; }
    public int column { get; set; }
    public VoaSourceContext? source_context { get; set; }
    public List<VoaErrorInfo>? related_errors { get; set; }
    public List<string>? suggestions { get; set; }
    public string? documentation_url { get; set; }
    public List<VoaStackFrame>? stack_frames { get; set; }
}
