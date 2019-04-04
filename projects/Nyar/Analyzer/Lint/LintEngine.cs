using System.Reflection;
using Std.Data.Text.Valkyrie.AST;

namespace Nyar.Lint;

/// <summary>
///     Lint 引擎：注册规则、遍历 AST、收集诊断
/// </summary>
public class LintEngine
{
    private readonly List<ILintRule> _rules = new();

    /// <summary>
    ///     注册一条 lint 规则
    /// </summary>
    public void Register(ILintRule rule)
    {
        _rules.Add(rule);
    }

    /// <summary>
    ///     对 AST 根节点运行所有已注册规则
    /// </summary>
    public List<LintDiagnostic> Run(ValkyrieNode root)
    {
        var diagnostics = new List<LintDiagnostic>();
        Walk(root, node =>
        {
            foreach (var rule in _rules)
            {
                diagnostics.AddRange(rule.Diagnose(node));
            }
        });
        return diagnostics;
    }

    /// <summary>
    ///     深度优先遍历 AST 树
    /// </summary>
    private static void Walk(ValkyrieNode node, Action<ValkyrieNode> visitor)
    {
        if (node is null)
        {
            return;
        }

        visitor(node);

        foreach (var child in GetChildren(node))
        {
            Walk(child, visitor);
        }
    }

    /// <summary>
    ///     通过反射获取子节点（<see cref="ValkyrieNode"/> 无 Accept 模式）
    /// </summary>
    private static IEnumerable<ValkyrieNode> GetChildren(ValkyrieNode node)
    {
        foreach (var prop in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType == typeof(ValkyrieNode) || typeof(ValkyrieNode).IsAssignableFrom(prop.PropertyType))
            {
                var child = prop.GetValue(node) as ValkyrieNode;
                if (child is not null)
                {
                    yield return child;
                }
            }
            else if (typeof(IEnumerable<ValkyrieNode>).IsAssignableFrom(prop.PropertyType))
            {
                var children = prop.GetValue(node) as IEnumerable<ValkyrieNode>;
                if (children is not null)
                {
                    foreach (var child in children)
                    {
                        if (child is not null)
                        {
                            yield return child;
                        }
                    }
                }
            }
        }
    }
}