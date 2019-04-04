namespace Std.Data.Text.Syntax;

/// <summary>
///     强类型 AST 节点工厂基类，提供程序化构造语法节点的通用能力
/// </summary>
/// <typeparam name="TRoot">语言特定的语法根类型</typeparam>
public abstract class SyntaxFactory<TRoot> where TRoot : SyntaxRoot
{
    /// <summary>
    ///     从绿树节点和语法树信息构造强类型语法根
    /// </summary>
    /// <param name="green">绿树根节点。</param>
    /// <param name="tree">所属语法树。</param>
    /// <returns>强类型语法根。</returns>
    public abstract TRoot create_root(GreenNode green, SyntaxTree tree);

    /// <summary>
    ///     使用 CstBuilder 程序化构造语法根
    /// </summary>
    /// <param name="buildAction">构建动作，在构建器上执行操作。</param>
    /// <returns>强类型语法根（无关联语法树）。</returns>
    public TRoot build_root(Action<CstBuilder> buildAction)
    {
        var b = new CstBuilder();
        buildAction(b);
        var green = b.build();
        return create_root(green, null!);
    }
}