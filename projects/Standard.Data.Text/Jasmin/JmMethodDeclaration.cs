using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     .method / .end method 声明
/// </summary>
/// <param name="access_flags">访问标志。</param>
/// <param name="method_name">方法名。</param>
/// <param name="descriptor">方法描述符。</param>
/// <param name="limits">.limit 指令列表</param>
/// <param name="instructions">指令列表。</param>
/// <param name="span">源码位置。</param>
public sealed record JmMethodDeclaration(
    List<string> access_flags,
    string method_name,
    string descriptor,
    List<JmLimitDirective> limits,
    List<JmInstruction> instructions,
    TextSpan span = default) : JmAstNode(span);