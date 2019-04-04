using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sonic.Data.Generator.Meta;

/// <summary>
///     增量 Source Generator 管道的简化构建器，封装常用的 <see cref="IncrementalValueProvider{TValue}" /> 操作模式。
/// </summary>
public static class IncrementalHelper
{
    /// <summary>
    ///     从语法提供者中筛选所有应用了指定特性的类型声明。
    ///     这是 Source Generator 管道中最常用的入口方法。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    /// <param name="attributeFullyQualifiedName">特性的完全限定名。</param>
    /// <returns>应用了指定特性的类型符号增量提供者。</returns>
    public static IncrementalValuesProvider<INamedTypeSymbol> for_types_with_attribute(
        this IncrementalGeneratorInitializationContext context,
        string attributeFullyQualifiedName)
    {
        var typeDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is TypeDeclarationSyntax,
            static (syntaxContext, _) => syntaxContext
        );

        return typeDeclarations
            .Select((syntaxContext, _) =>
            {
                var symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node);
                return symbol as INamedTypeSymbol;
            })
            .Where(typeSymbol => typeSymbol is not null)
            .Select((typeSymbol, _) => typeSymbol!)
            .Where(typeSymbol => { return SymbolHelper.has_attribute(typeSymbol, attributeFullyQualifiedName); });
    }

    /// <summary>
    ///     对增量值提供者进行过滤，简化调用无需处理 <see cref="System.Threading.CancellationToken" />。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="source">源提供者。</param>
    /// <param name="predicate">过滤条件。</param>
    /// <returns>过滤后的增量值提供者。</returns>
    public static IncrementalValuesProvider<T> filter<T>(
        this IncrementalValuesProvider<T> source,
        Func<T, bool> predicate)
    {
        return source.Where(predicate);
    }

    /// <summary>
    ///     将增量值提供者中的集合展开为单个元素序列。
    /// </summary>
    /// <typeparam name="TCollection">集合类型。</typeparam>
    /// <typeparam name="TElement">元素类型。</typeparam>
    /// <param name="source">源提供者。</param>
    /// <param name="selector">集合到元素枚举的投影。</param>
    /// <returns>展开后的增量值提供者。</returns>
    public static IncrementalValuesProvider<TElement> select_many<TCollection, TElement>(
        this IncrementalValuesProvider<TCollection> source,
        Func<TCollection, IEnumerable<TElement>> selector)
    {
        return source.SelectMany((collection, _) => selector(collection).ToImmutableArray());
    }

    /// <summary>
    ///     将两个增量值提供者组合为元组。
    /// </summary>
    /// <typeparam name="TLeft">左侧值类型。</typeparam>
    /// <typeparam name="TRight">右侧值类型。</typeparam>
    /// <param name="left">左侧提供者。</param>
    /// <param name="right">右侧提供者。</param>
    /// <returns>组合后的元组增量值提供者。</returns>
    public static IncrementalValueProvider<(TLeft Left, TRight Right)> combine<TLeft, TRight>(
        this IncrementalValueProvider<TLeft> left,
        IncrementalValueProvider<TRight> right)
    {
        return left.combine(right)
            .Select((pair, _) => (pair.Left, pair.Right));
    }
}