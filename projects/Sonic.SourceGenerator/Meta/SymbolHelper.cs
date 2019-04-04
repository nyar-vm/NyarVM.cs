using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Sonic.Data.Generator.Meta;

/// <summary>
///     Roslyn 符号访问的静态辅助方法，提供类型安全的符号查询能力。
/// </summary>
public static class SymbolHelper
{
    /// <summary>
    ///     获取指定符号上所有特性数据，按名称过滤。
    /// </summary>
    /// <param name="symbol">要查询的符号。</param>
    /// <param name="attributeName">特性类的完全限定名（含命名空间）。</param>
    /// <returns>匹配的特性数据集合。</returns>
    public static ImmutableArray<AttributeData> get_attributes(ISymbol symbol, string attributeName)
    {
        if (symbol is null) return ImmutableArray<AttributeData>.Empty;

        return
        [
            .. symbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == attributeName)
        ];
    }

    /// <summary>
    ///     判断指定符号是否应用了某个特性。
    /// </summary>
    /// <param name="symbol">要查询的符号。</param>
    /// <param name="attributeName">特性类的完全限定名（含命名空间）。</param>
    /// <returns>如果符号包含该特性则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool has_attribute(ISymbol symbol, string attributeName)
    {
        if (symbol is null) return false;

        return symbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == attributeName);
    }

    /// <summary>
    ///     获取指定类型的基类型符号。
    /// </summary>
    /// <param name="typeSymbol">要查询的命名类型符号。</param>
    /// <returns>基类型的符号，若为 <c>object</c> 或无基类型则返回 <c>null</c>。</returns>
    public static INamedTypeSymbol? get_base_type(INamedTypeSymbol? typeSymbol)
    {
        var baseType = typeSymbol?.BaseType;
        if (baseType is null) return null;

        if (baseType.SpecialType == SpecialType.System_Object) return null;

        return baseType;
    }

    /// <summary>
    ///     获取指定类型中满足条件的成员符号集合。
    /// </summary>
    /// <param name="typeSymbol">要查询的命名类型符号。</param>
    /// <param name="predicate">成员筛选条件。</param>
    /// <returns>满足条件的成员符号集合。</returns>
    public static ImmutableArray<ISymbol> get_members(INamedTypeSymbol? typeSymbol,
        Func<ISymbol, bool>? predicate = null)
    {
        if (typeSymbol is null) return ImmutableArray<ISymbol>.Empty;

        var members = typeSymbol.GetMembers();
        if (predicate is null) return members;

        return [.. members.Where(predicate)];
    }

    /// <summary>
    ///     判断类型声明是否包含 <c>partial</c> 关键字。
    /// </summary>
    /// <param name="typeSymbol">要检查的类型符号。</param>
    /// <returns>如果类型的任一语法声明包含 <c>partial</c> 关键字则返回 <c>true</c>。</returns>
    public static bool is_partial(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        foreach (var declaration in typeSymbol.DeclaringSyntaxReferences)
        {
            var syntax = declaration.GetSyntax();
            if (syntax.HasLeadingTrivia || syntax.HasTrailingTrivia)
            {
            }

            var text = syntax.ToFullString();
            if (text.Contains("partial")) return true;
        }

        return false;
    }

    /// <summary>
    ///     获取符号所在命名空间的完全限定名。
    /// </summary>
    /// <param name="symbol">要查询的符号。</param>
    /// <returns>命名空间的完全限定名字符串；若符号没有命名空间则返回空字符串。</returns>
    public static string get_namespace(ISymbol? symbol)
    {
        var namespaceSymbol = symbol?.ContainingNamespace;
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace) return string.Empty;

        return namespaceSymbol.ToDisplayString();
    }
}