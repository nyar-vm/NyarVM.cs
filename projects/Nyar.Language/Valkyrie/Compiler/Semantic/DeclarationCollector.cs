using Nyar.Analyzer.Semantic;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Semantic;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Nyar.Language.Valkyrie.Compiler.Semantic;

/// <summary>
///     Phase 1 声明收集器，遍历所有文件的 AST 并填充全局声明�?/// </summary>
public sealed class DeclarationCollector
{
    /// <summary>
    ///     从多个编译单元收集声明到全局声明�?    /// </summary>
    /// <param name="asts">所有文件的 AST 编译单元列表</param>
    /// <returns>填充了类型、trait、函数签名的全局声明�?/returns>
    public GlobalDeclarationTable collect(IReadOnlyList<CompilationUnit> asts)
    {
        var table = new GlobalDeclarationTable();

        foreach (var ast in asts)
        {
            var moduleName = extract_module_name(ast);
            if (!table.modules.ContainsKey(moduleName)) table.modules[moduleName] = [];

            foreach (var declaration in ast.declarations)
            {
                table.modules[moduleName].Add(declaration);
                collect_declaration(declaration, moduleName, table);
            }
        }

        return table;
    }

    private static string extract_module_name(CompilationUnit ast)
    {
        foreach (var decl in ast.declarations)
            if (decl is NamespaceDecl ns)
                return ns.name.name;

        return "_root";
    }

    private static void collect_declaration(AstNode declaration, string moduleName, GlobalDeclarationTable table)
    {
        var currentNamespace = ValkyrieNameSpace.parse(moduleName == "_root" ? null : moduleName);

        switch (declaration)
        {
            case ClassDecl classDecl:
            {
                var clsName = currentNamespace.qualify(ValkyrieNamePath.parse(classDecl.name?.name));
                if (!table.types.ContainsKey(clsName))
                    table.types[clsName] = new HirClassDef(
                        clsName,
                        null,
                        [],
                        [],
                        [],
                        null,
                        [],
                        []);

                break;
            }

            case StructureDecl structDecl:
            {
                var structName = currentNamespace.qualify(ValkyrieNamePath.parse(structDecl.name?.name));
                if (!table.types.ContainsKey(structName))
                    table.types[structName] = new HirStructDef(
                        structName,
                        null,
                        [],
                        [],
                        []);

                break;
            }

            case EnumDecl enumDecl:
            {
                var enumName = currentNamespace.qualify(ValkyrieNamePath.parse(enumDecl.name.name));
                if (!table.types.ContainsKey(enumName))
                    table.types[enumName] = new HirEnumsDef(
                        enumName,
                        []);

                break;
            }

            case FlagsDecl flagsDecl:
            {
                var flagsName = currentNamespace.qualify(ValkyrieNamePath.parse(flagsDecl.name.name));
                if (!table.types.ContainsKey(flagsName))
                    table.types[flagsName] = new HirFlagsDef(
                        flagsName,
                        []);

                break;
            }

            case UniteDecl uniteDecl:
            {
                var uniteName = currentNamespace.qualify(ValkyrieNamePath.parse(uniteDecl.name?.name));
                if (!table.types.ContainsKey(uniteName))
                    table.types[uniteName] = new HirUniteDef(
                        uniteName,
                        [],
                        []);

                break;
            }

            case TraitDecl traitDecl:
            {
                var traitName = currentNamespace.qualify(ValkyrieNamePath.parse(traitDecl.name?.name));
                if (!table.traits.ContainsKey(traitName))
                    table.traits[traitName] = new HirTraitDef(
                        traitName,
                        [],
                        [],
                        []);

                break;
            }

            case FunctionDecl microDecl:
            {
                var funcName = currentNamespace.qualify(ValkyrieNamePath.parse(microDecl.name?.name));
                if (!table.functions.ContainsKey(funcName))
                    table.functions[funcName] = new HirFunctionDef(
                        funcName.ToString(),
                        microDecl,
                        [],
                        get_type_ref(microDecl.return_type, ValkyrieBuiltinTypeFacts.auto_name),
                        new HirCallableSemantics(false, null, []),
                        [],
                        moduleName == "_root" ? null : moduleName,
                        EffectSet.pure);

                break;
            }

            case DeclareNamespace namespaceDecl:
            {
                foreach (var nested in namespaceDecl.declarations) collect_declaration(nested, moduleName, table);

                break;
            }
        }
    }

    private static string get_type_name(TypeNode? typeNode, string fallback = "unknown")
    {
        return typeNode switch
        {
            TypeLiteralNamePathNode literal => get_named_type_name(literal),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.product } product => get_product_type_name(product,
                fallback),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.or } union => join_type_names(union,
                TypeBinaryOperator.or, " | ", fallback),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.and } intersection => join_type_names(intersection,
                TypeBinaryOperator.and, " & ", fallback),
            TypeExpressionUnaryNode { @operator: TypeUnaryOperator.nullable } nullable =>
                $"{get_type_name(nullable.operand, fallback)}?",
            null => fallback,
            _ => typeNode.ToString() ?? fallback
        };
    }

    private static string get_named_type_name(TypeLiteralNamePathNode literal)
    {
        var specialType = resolve_special_type(literal.path.full_name);
        return project_type_name(specialType) ?? literal.path.full_name;
    }

    private static IType? resolve_special_type(string typeName)
    {
        return ValkyrieBuiltinTypeFacts.try_create_annotation_type(typeName);
    }

    private static string? project_type_name(IType? type)
    {
        return type switch
        {
            PrimitiveType { name: "unit" } => "unit",
            PrimitiveType { name: "void" } => "void",
            AutoType => ValkyrieBuiltinTypeFacts.auto_name,
            NamedType { name: "ExitCode" } => "ExitCode",
            _ => null
        };
    }

    private static HirTypeRef get_type_ref(TypeNode? typeNode, string fallback = "unknown")
    {
        return typeNode switch
        {
            TypeLiteralNamePathNode literal => build_named_type_ref(literal),
            null => HirTypeRef.from_name_path(ValkyrieNamePath.parse(fallback)),
            _ => HirTypeRef.from_name_path(ValkyrieNamePath.parse(get_type_name(typeNode, fallback)))
        };
    }

    private static HirTypeRef build_named_type_ref(TypeLiteralNamePathNode literal)
    {
        var specialType = resolve_special_type(literal.path.full_name);
        return HirTypeRef.from_type(specialType, get_named_type_name(literal));
    }

    private static string get_product_type_name(TypeExpressionBinaryNode productNode, string fallback)
    {
        var items = flatten_type_binary(productNode, TypeBinaryOperator.product);
        if (items.Count == 0) return fallback;

        if (items[0] is TypeLiteralNamePathNode constructor) return constructor.path.full_name;

        return get_type_name(items[0], fallback);
    }

    private static string join_type_names(TypeExpressionBinaryNode node, TypeBinaryOperator op, string separator,
        string fallback)
    {
        var items = flatten_type_binary(node, op)
            .Select(item => get_type_name(item, fallback))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        return items.Length > 0 ? string.Join(separator, items) : fallback;
    }

    private static IReadOnlyList<TypeNode> flatten_type_binary(TypeNode node, TypeBinaryOperator op)
    {
        var items = new List<TypeNode>();
        collect_type_binary(node, op, items);
        return items;
    }

    private static void collect_type_binary(TypeNode node, TypeBinaryOperator op, ICollection<TypeNode> items)
    {
        if (node is TypeExpressionBinaryNode binary && binary.@operator == op)
        {
            collect_type_binary(binary.lhs, op, items);
            collect_type_binary(binary.rhs, op, items);
            return;
        }

        items.Add(node);
    }
}
