using Std.Data.Text.Syntax;

namespace Std.Data.Text.Msil;

/// <summary>
///     方法声明
/// </summary>
/// <param name="access_flags">访问标志。</param>
/// <param name="return_type_name">返回类型名。</param>
/// <param name="method_name">方法名。</param>
/// <param name="parameters">参数列表。</param>
/// <param name="max_stack">最大栈深度。</param>
/// <param name="local_variables">局部变量列表。</param>
/// <param name="instructions">指令列表。</param>
/// <param name="is_entry_point">是否为入口点。</param>
/// <param name="span">源码位置。</param>
public sealed record MsilMethodDeclaration(
    List<string> access_flags,
    string return_type_name,
    string method_name,
    List<MsilParameter> parameters,
    int max_stack,
    List<MsilLocalVariable> local_variables,
    List<MsilInstruction> instructions,
    bool is_entry_point = false,
    TextSpan span = default) : MsilAstNode(span);