namespace Nyar.Language.Awsl.Asgard.Compiler;

/// <summary>
///     JS 导入信息
/// </summary>
public sealed class JsImportInfo
{
    /// <summary>
    ///     模块名称
    /// </summary>
    public string module_name { get; set; } = "";

    /// <summary>
    ///     函数名称
    /// </summary>
    public string func_name { get; set; } = "";

    /// <summary>
    ///     JS 函数表达式
    /// </summary>
    public string js_function { get; set; } = "";

    /// <summary>
    ///     是否为内置桥接函数
    /// </summary>
    public bool is_builtin { get; set; }
}
