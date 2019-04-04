using System.Collections.Immutable;
using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.Dialect;

public sealed class DeclarationSemantics
{
    public ImmutableList<DeclarationDiagnostic> validate_var_decl(EGraph<AlgebraNode> egraph, VarDecl varDecl)
    {
        var diagnostics = ImmutableList.CreateBuilder<DeclarationDiagnostic>();

        if (string.IsNullOrEmpty(varDecl.name))
            diagnostics.Add(new DeclarationDiagnostic(DeclarationDiagnosticLevel.error, "变量声明缺少名称", varDecl.name));

        var typeValid = validate_type_ref(egraph, varDecl.type);
        if (!typeValid)
            diagnostics.Add(new DeclarationDiagnostic(
                DeclarationDiagnosticLevel.error,
                $"变量 '{varDecl.name}' 的类型引用无效",
                varDecl.name));

        if (varDecl.value != default)
        {
            var typeCompatible = check_type_compatibility(egraph, varDecl.type, varDecl.value);
            if (!typeCompatible)
                diagnostics.Add(new DeclarationDiagnostic(
                    DeclarationDiagnosticLevel.warning,
                    $"变量 '{varDecl.name}' 的初始值类型与声明类型不兼容",
                    varDecl.name));
        }

        foreach (var attrId in varDecl.attributes)
        {
            var attrDiags = validate_attribute(egraph, attrId);
            diagnostics.AddRange(attrDiags);
        }

        validate_modifiers(varDecl, diagnostics);

        return diagnostics.ToImmutable();
    }

    public ImmutableList<DeclarationDiagnostic> validate_attribute(EGraph<AlgebraNode> egraph, Id attrId)
    {
        var diagnostics = ImmutableList.CreateBuilder<DeclarationDiagnostic>();
        var attrClass = egraph.get_class(attrId);

        if (attrClass is null) return diagnostics.ToImmutable();

        foreach (var node in attrClass.nodes)
        {
            if (node is not Attrib attr) continue;

            if (string.IsNullOrEmpty(attr.name))
                diagnostics.Add(new DeclarationDiagnostic(
                    DeclarationDiagnosticLevel.error,
                    "特性标注缺少名称",
                    ""));

            var knownAttributes = new HashSet<string>
            {
                "Serializable", "range", "color",
                "header", "space", "inline", "noinline", "deprecated",
                "must_use", "unsafe",
                "js", "js_builtin", "wasi",
                "c", "com", "syscall",
                "clr", "dlr",
                "jvm",
                "import", "export", "pure"
            };

            if (!knownAttributes.Contains(attr.name))
                diagnostics.Add(new DeclarationDiagnostic(
                    DeclarationDiagnosticLevel.warning,
                    $"未知特性标注 '{attr.name}'",
                    attr.name));
        }

        return diagnostics.ToImmutable();
    }

    public bool validate_type_ref(EGraph<AlgebraNode> egraph, Id typeRefId)
    {
        var typeClass = egraph.get_class(typeRefId);
        if (typeClass is null) return false;

        foreach (var node in typeClass.nodes)
            switch (node)
            {
                case TypeRef typeRef:
                    return is_valid_type_name(typeRef.name);
                case FuncType:
                case ListType:
                case ArrType:
                case UnionType:
                case IntersectType:
                    return true;
            }

        return false;
    }

    public AlgebraNode? resolve_type_alias(EGraph<AlgebraNode> egraph, Id aliasId)
    {
        var aliasClass = egraph.get_class(aliasId);
        if (aliasClass is null) return null;

        foreach (var node in aliasClass.nodes)
        {
            if (node is not TypeAlias alias) continue;

            var targetClass = egraph.get_class(alias.target);
            if (targetClass is null) continue;

            foreach (var targetNode in targetClass.nodes)
            {
                if (targetNode is TypeAlias nestedAlias) return resolve_type_alias(egraph, alias.target);

                return targetNode;
            }
        }

        return null;
    }

    public AlgebraNode? expand_type_ref(EGraph<AlgebraNode> egraph, Id typeRefId, ImmutableDictionary<string, Id> aliasMap)
    {
        var typeClass = egraph.get_class(typeRefId);
        if (typeClass is null) return null;

        foreach (var node in typeClass.nodes)
            if (node is TypeRef typeRef)
            {
                if (aliasMap.TryGetValue(typeRef.name, out var targetId))
                    return expand_type_ref(egraph, targetId, aliasMap);

                return typeRef;
            }

        return null;
    }

    public ImmutableDictionary<string, Id> build_alias_map(EGraph<AlgebraNode> egraph, IEnumerable<Id> rootIds)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, Id>();
        var visited = new HashSet<Id>();

        foreach (var rootId in rootIds) collect_aliases(egraph, rootId, builder, visited);

        return builder.ToImmutable();
    }

    private void collect_aliases(EGraph<AlgebraNode> egraph, Id id, ImmutableDictionary<string, Id>.Builder builder,
        HashSet<Id> visited)
    {
        if (!visited.Add(id)) return;

        var eclass = egraph.get_class(id);
        if (eclass is null) return;

        foreach (var node in eclass.nodes)
            if (node is TypeAlias alias)
            {
                builder[alias.alias_name] = alias.target;
                collect_aliases(egraph, alias.target, builder, visited);
            }
            else if (node is VarDecl varDecl)
            {
                collect_aliases(egraph, varDecl.type, builder, visited);
            }
            else if (node is TypeRef typeRef)
            {
                foreach (var argId in typeRef.generic_args) collect_aliases(egraph, argId, builder, visited);
            }
    }

    private bool check_type_compatibility(EGraph<AlgebraNode> egraph, Id typeId, Id valueId)
    {
        var typeClass = egraph.get_class(typeId);
        var valueClass = egraph.get_class(valueId);
        if (typeClass is null || valueClass is null) return true;

        string? typeName = null;
        foreach (var node in typeClass.nodes)
            if (node is TypeRef tr)
            {
                typeName = tr.name;
                break;
            }

        if (typeName is null) return true;

        foreach (var valueNode in valueClass.nodes)
        {
            var compatible = (valueNode, typeName) switch
            {
                (Literal<long> _, "i32" or "i64") => true,
                (Literal<double> _, "f32" or "f64") => true,
                (Literal<bool> _, "bool") => true,
                (Literal<string> _, "string" or "str") => true,
                (Literal<long> _, "f32" or "f64") => true,
                _ => (bool?)null
            };

            if (compatible is false) return false;

            if (compatible is true) return true;
        }

        return true;
    }

    private bool is_valid_type_name(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        var primitiveTypes = new HashSet<string>
        {
            "i8", "i16", "i32", "i64", "isize",
            "u8", "u16", "u32", "u64", "usize",
            "f16", "f32", "f64",
            "bool", "char", "string", "str",
            "void", "never", "any"
        };

        if (primitiveTypes.Contains(name)) return true;

        return char.IsUpper(name[0]);
    }

    private void validate_modifiers(VarDecl varDecl, ImmutableList<DeclarationDiagnostic>.Builder diagnostics)
    {
        var validModifiers = new HashSet<string>
        {
            "mut", "const", "static",
            "readonly", "volatile", "abstract", "virtual",
            "override", "sealed", "extern", "inline"
        };

        var seenModifiers = new HashSet<string>();

        foreach (var mod in varDecl.modifiers)
        {
            if (!validModifiers.Contains(mod))
                diagnostics.Add(new DeclarationDiagnostic(
                    DeclarationDiagnosticLevel.warning,
                    $"未知修饰符 '{mod}'",
                    mod));

            if (!seenModifiers.Add(mod))
                diagnostics.Add(new DeclarationDiagnostic(
                    DeclarationDiagnosticLevel.error,
                    $"重复修饰符 '{mod}'",
                    mod));
        }

        if (varDecl.modifiers.Contains("const") && varDecl.value == default)
            diagnostics.Add(new DeclarationDiagnostic(
                DeclarationDiagnosticLevel.error,
                $"常量声明 '{varDecl.name}' 必须有初始值",
                varDecl.name));

        if (varDecl.modifiers.Contains("mut") && varDecl.modifiers.Contains("const"))
            diagnostics.Add(new DeclarationDiagnostic(
                DeclarationDiagnosticLevel.error,
                $"声明 '{varDecl.name}' 不能同时为 mut 和 const",
                varDecl.name));
    }
}

public sealed class DeclarationDiagnostic
{
    public DeclarationDiagnostic(DeclarationDiagnosticLevel level, string message, string location)
    {
        this.level = level;
        this.message = message;
        this.location = location;
    }

    public DeclarationDiagnosticLevel level { get; }
    public string message { get; }
    public string location { get; }

    public override string ToString()
    {
        return $"[{level}] {message} (at '{location}')";
    }
}

public enum DeclarationDiagnosticLevel
{
    info,
    warning,
    error
}
