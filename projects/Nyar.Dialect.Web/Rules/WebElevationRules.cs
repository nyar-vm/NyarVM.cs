using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Web.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Web.Rules;

/// <summary>
///     DOM 查询提升：Apply(Import("document", "querySelector"), [selector]) → DomQuery(selector)
/// </summary>
public sealed class DomQueryElevationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "dom-query-elevation";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Apply apply) continue;

            var import = TryGetImport(egraph, apply.function);
            if (import is not { module_name: "document", name: "querySelector" or "getElementById" }) continue;

            if (apply.arguments.Length != 1) continue;

            var selector = TryGetStringConstant(egraph, apply.arguments[0]);
            if (selector is null) continue;

            if (import.name == "getElementById") selector = "#" + selector;

            yield return (node, new DomQuery(selector));
        }
    }

    internal static Import? TryGetImport(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) return null;

        foreach (var node in eclass.nodes)
            if (node is Import import)
                return import;

        return null;
    }

    internal static string? TryGetStringConstant(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) return null;

        foreach (var node in eclass.nodes)
            if (node is Literal<string> sc)
                return sc.value;

        return null;
    }
}

/// <summary>
///     DOM 变更提升：Apply(Import("Element", "setAttribute/setStyle"), [handle, name, value]) → DomMutate
/// </summary>
public sealed class DomMutateElevationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "dom-mutate-elevation";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Apply apply) continue;

            var import = DomQueryElevationRule.TryGetImport(egraph, apply.function);
            if (import is null || import.module_name != "Element") continue;

            if (import.name is not ("setAttribute" or "setStyle" or "setClassName")) continue;

            if (apply.arguments.Length < 2) continue;

            var handleId = apply.arguments[0];
            var propName = import.name switch
            {
                "setClassName" => "className",
                _ => DomQueryElevationRule.TryGetStringConstant(egraph, apply.arguments[1])
            };

            if (propName is null) continue;

            if (import.name == "setStyle") propName = "style." + propName;

            var valueId = apply.arguments.Length > 2 ? apply.arguments[2] : apply.arguments[1];

            yield return (node, new DomMutate(handleId, propName, valueId));
        }
    }
}

/// <summary>
///     元素创建提升：Apply(Import("document", "createElement"), [tag]) → Element(tag, [], [])
/// </summary>
public sealed class ElementElevationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "element-elevation";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Apply apply) continue;

            var import = DomQueryElevationRule.TryGetImport(egraph, apply.function);
            if (import is not { module_name: "document", name: "createElement" }) continue;

            if (apply.arguments.Length != 1) continue;

            var tag = DomQueryElevationRule.TryGetStringConstant(egraph, apply.arguments[0]);
            if (tag is null) continue;

            yield return (node, new Element(tag, [], []));
        }
    }
}

/// <summary>
///     文本节点提升：Apply(Import("document", "createTextNode"), [text]) → TextNode(text)
/// </summary>
public sealed class TextNodeElevationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "text-node-elevation";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Apply apply) continue;

            var import = DomQueryElevationRule.TryGetImport(egraph, apply.function);
            if (import is not { module_name: "document", name: "createTextNode" }) continue;

            if (apply.arguments.Length != 1) continue;

            var text = DomQueryElevationRule.TryGetStringConstant(egraph, apply.arguments[0]);
            if (text is null) continue;

            yield return (node, new TextNode(text));
        }
    }
}

/// <summary>
///     Storage 读取提升：Apply(Import("localStorage"/"sessionStorage", "getItem"), [key]) → StorageGet(type, key)
/// </summary>
public sealed class StorageGetElevationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "storage-get-elevation";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Apply apply) continue;

            var import = DomQueryElevationRule.TryGetImport(egraph, apply.function);
            if (import is null || import.name != "getItem") continue;

            var storageType = import.module_name switch
            {
                "localStorage" => "local",
                "sessionStorage" => "session",
                _ => null
            };

            if (storageType is null) continue;

            if (apply.arguments.Length != 1) continue;

            var key = DomQueryElevationRule.TryGetStringConstant(egraph, apply.arguments[0]);
            if (key is null) continue;

            yield return (node, new StorageGet(storageType, key));
        }
    }
}

/// <summary>
///     Storage 写入提升：Apply(Import("localStorage"/"sessionStorage", "setItem"), [key, value]) → StorageSet(type, key, value)
/// </summary>
public sealed class StorageSetElevationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "storage-set-elevation";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Apply apply) continue;

            var import = DomQueryElevationRule.TryGetImport(egraph, apply.function);
            if (import is null || import.name != "setItem") continue;

            var storageType = import.module_name switch
            {
                "localStorage" => "local",
                "sessionStorage" => "session",
                _ => null
            };

            if (storageType is null) continue;

            if (apply.arguments.Length != 2) continue;

            var key = DomQueryElevationRule.TryGetStringConstant(egraph, apply.arguments[0]);
            if (key is null) continue;

            yield return (node, new StorageSet(storageType, key, apply.arguments[1]));
        }
    }
}