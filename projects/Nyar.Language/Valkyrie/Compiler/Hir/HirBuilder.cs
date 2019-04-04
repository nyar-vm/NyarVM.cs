using System.Text;
using Nyar.Analyzer.Semantic;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Compiler.Hir.Attributes;
using Nyar.Language.Valkyrie.Semantic;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     �?`Valkyrie AST` �?`SemanticModel` 组织为结构化 `HIR`�?
/// </summary>
public sealed class HirBuilder
{
    #region 声明收集

    private static void collect_declarations(
        IReadOnlyList<AstNode> declarations,
        string? currentNamespace,
        SemanticModel semantics,
        ICollection<HirFunction> functions,
        ICollection<HirLetDefs> lets,
        ICollection<HirTypeDef> types,
        ICollection<HirTraitDef> traits,
        ICollection<HirTypeAliasRef> typeAliases,
        ICollection<HirTraitAliasRef> traitAliases,
        ICollection<HirImplyDef> implys,
        IDictionary<string, ClassDecl> classDeclarations)
    {
        foreach (var declaration in declarations)
        {
            if (declaration is NamespaceDecl namespaceDecl)
            {
                var resolvedNamespace = combine_namespace(currentNamespace, namespaceDecl.name.name);
                if (namespaceDecl.declarations.Count == 0)
                    currentNamespace = resolvedNamespace;
                else
                    collect_declarations(namespaceDecl.declarations, resolvedNamespace, semantics, functions, lets, types,
                        traits,
                        typeAliases, traitAliases, implys, classDeclarations);

                continue;
            }

            if (declaration is FunctionDecl { body: not null } functionDecl)
            {
                var func = build_function(functionDecl, currentNamespace);
                functions.Add(func);
                continue;
            }

            if (declaration is FunctionDecl externalDecl && is_external_declaration(externalDecl))
            {
                functions.Add(build_function(externalDecl, currentNamespace));
                continue;
            }

            if (declaration is LetDeclaration letDeclaration)
            {
                lets.Add(build_let(letDeclaration, currentNamespace));
                continue;
            }

            if (declaration is ClassDecl classDecl)
            {
                var inheritanceEdges = build_inheritance_edges(classDecl.inheritance);
                var inheritedTypes = inheritanceEdges.Select(edge => edge.base_type).ToArray();
                var qualifiedName = build_qualified_name(currentNamespace, null, classDecl.name?.name);
                types.Add(new HirClassDef(
                    qualifiedName,
                    build_data_shape(semantics, qualifiedName),
                    build_methods(classDecl.body, currentNamespace, classDecl.name?.name,
                        HirTypeRef.named(classDecl.name?.name ?? string.Empty), null, HirMethodKind.inherent),
                    build_surface_attributes(classDecl.annotations),
                    HirAttributeSemantics.collect_type_external_import_links(
                        build_surface_attributes(classDecl.annotations)),
                    inheritedTypes.FirstOrDefault(),
                    inheritedTypes,
                    inheritanceEdges));
                classDeclarations[qualifiedName] = classDecl;
                continue;
            }

            if (declaration is StructureDecl structDecl)
            {
                var qualifiedName = build_qualified_name(currentNamespace, null, structDecl.name?.name);
                types.Add(new HirStructDef(
                    qualifiedName,
                    build_data_shape(semantics, qualifiedName),
                    build_methods(structDecl.body, currentNamespace, structDecl.name?.name,
                        HirTypeRef.named(structDecl.name?.name ?? string.Empty), null,
                        HirMethodKind.inherent),
                    build_surface_attributes(structDecl.annotations),
                    HirAttributeSemantics.collect_type_external_import_links(
                        build_surface_attributes(structDecl.annotations))));
                continue;
            }

            if (declaration is EnumDecl enumDecl)
            {
                var variants = enumDecl.members
                    .Select(member => new HirVariantDef(
                        member.name?.name ?? string.Empty,
                        null,
                        [],
                        extract_discriminant(member.value)))
                    .ToArray();
                types.Add(new HirEnumsDef(
                    build_qualified_name(currentNamespace, null, enumDecl.name.name),
                    variants));
                continue;
            }

            if (declaration is FlagsDecl flagsDecl)
            {
                var variants = flagsDecl.members
                    .Select(member => new HirVariantDef(
                        member.name?.name ?? string.Empty,
                        null,
                        [],
                        extract_discriminant(member.value)))
                    .ToArray();
                types.Add(new HirFlagsDef(
                    build_qualified_name(currentNamespace, null, flagsDecl.name.name),
                    variants));
                continue;
            }

            if (declaration is UniteDecl uniteDecl)
            {
                var variants = assign_unite_discriminants(uniteDecl.variants);
                var ownerType = HirTypeRef.named(uniteDecl.name?.name ?? string.Empty);
                var methods = uniteDecl.methods
                    .Select(method => build_method(
                        method,
                        currentNamespace,
                        uniteDecl.name?.name ?? string.Empty,
                        ownerType,
                        null,
                        HirMethodKind.inherent,
                        null))
                    .ToArray();
                var qualifiedName = build_qualified_name(currentNamespace, null, uniteDecl.name?.name);
                types.Add(new HirUniteDef(
                    qualifiedName,
                    methods,
                    variants));
                continue;
            }

            if (declaration is TraitDecl traitDecl)
            {
                var baseTraits = build_inherited_types(traitDecl.inheritance);
                var qualifiedName = build_qualified_name(currentNamespace, null, traitDecl.name?.name);
                traits.Add(new HirTraitDef(
                    qualifiedName,
                    build_methods(traitDecl.body, currentNamespace, traitDecl.name?.name,
                        HirTypeRef.named(traitDecl.name?.name ?? string.Empty), null, HirMethodKind.trait),
                    baseTraits,
                    traitDecl.body?.associated_types
                        .Select(type => type.name?.name ?? string.Empty)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToArray() ?? []));
                continue;
            }

            if (declaration is TypeAliasDecl typeAliasDecl)
            {
                var aliasName = typeAliasDecl.name?.name ?? string.Empty;
                typeAliases.Add(new HirTypeAliasRef(
                    aliasName,
                    ValkyrieNamePath.parse(build_qualified_name(currentNamespace, null, aliasName)),
                    get_type_ref(typeAliasDecl.target_type)));
                continue;
            }

            if (declaration is DeclareTraitAlias traitAliasDecl)
            {
                var aliasName = traitAliasDecl.name?.name ?? string.Empty;
                traitAliases.Add(new HirTraitAliasRef(
                    aliasName,
                    ValkyrieNamePath.parse(build_qualified_name(currentNamespace, null, aliasName)),
                    build_trait_alias_targets(traitAliasDecl.target_type)));
                continue;
            }

            if (declaration is DeclareImply implyDecl)
            {
                var targetType = get_type_ref(implyDecl.target_type);
                var contractType = implyDecl.contract_type is null ? null : get_type_ref(implyDecl.contract_type);
                var methods = implyDecl.methods
                    .Select(method => build_method(method, currentNamespace, get_type_name(implyDecl.target_type),
                        targetType,
                        contractType, HirMethodKind.imply, null))
                    .ToArray();
                var typeBindings = implyDecl.associated_types
                    .Select(a => build_type_binding(a, contractType?.name_path))
                    .Where(b => b is not null)
                    .Select(b => b!)
                    .ToArray();
                implys.Add(new HirImplyDef(
                    targetType,
                    contractType,
                    currentNamespace,
                    methods,
                    [],
                    typeBindings,
                    HirImplySourceKind.explicit_declaration));
            }
        }
    }

    #endregion

    #region 公共构建

    public HirModule build(CompilationUnit syntax, SemanticModel semantics, string moduleName)
    {
        return build([syntax], semantics, moduleName);
    }

    public HirModule build(IReadOnlyList<CompilationUnit> syntaxUnits, SemanticModel semantics, string moduleName)
    {
        var normalizedSyntaxUnits = syntaxUnits
            .Select(normalize_syntax_unit)
            .ToArray();
        var functions = new List<HirFunction>();
        var lets = new List<HirLetDefs>();
        var types = new List<HirTypeDef>();
        var traits = new List<HirTraitDef>();
        var typeAliases = new List<HirTypeAliasRef>();
        var traitAliases = new List<HirTraitAliasRef>();
        var implys = new List<HirImplyDef>();
        var classDeclarations = new Dictionary<string, DeclareClass>(StringComparer.Ordinal);

        foreach (var unit in normalizedSyntaxUnits)
            collect_declarations(unit.declarations, null, semantics, functions, lets, types, traits, typeAliases,
                traitAliases,
                implys, classDeclarations);

        validate_class_inheritance(types, classDeclarations, semantics);

        var inferenceEntries = build_inference_entries(functions, types, traits, implys);
        var typeInference = new ValkyrieTypeInference(inferenceEntries, new ValkyrieSemanticBridge());

        functions =
        [
            .. functions
                .Select(function => rebuild_function(function, typeInference))
        ];
        types =
        [
            .. types
                .Select(type => type with
                {
                    methods = [.. type.methods.Select(method => rebuild_method(method, typeInference))]
                })
        ];
        traits =
        [
            .. traits
                .Select(trait => trait with
                {
                    methods = [.. trait.methods.Select(method => rebuild_method(method, typeInference))]
                })
        ];
        implys =
        [
            .. implys
                .Select(imply => imply with
                {
                    methods = [.. imply.methods.Select(method => rebuild_method(method, typeInference))]
                })
        ];
        implys = synthesize_trait_default_implys(types, traits, traitAliases, implys);
        (functions, types, traits, implys) = uniquify_callable_names(functions, types, traits, implys);
        implys = resolve_witness_bindings(implys, traits, traitAliases);

        var mergedSyntax = new CompilationUnit(
            [.. normalizedSyntaxUnits.SelectMany(unit => unit.declarations)],
            normalizedSyntaxUnits.FirstOrDefault()?.file_path ?? string.Empty,
            default);

        var hir = new HirModule(moduleName, mergedSyntax, normalizedSyntaxUnits, semantics, functions, lets, types, traits,
            typeAliases, traitAliases, implys);
        hir.validate_call_sites();
        return hir;
    }

    #endregion

    #region 推断重建

    private static IReadOnlyList<FunctionDecl> build_inference_entries(
        IReadOnlyList<HirFunction> functions,
        IReadOnlyList<HirTypeDef> types,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirImplyDef> implys)
    {
        var entries = new List<DeclareMicro>(functions.Count);
        entries.AddRange(functions.Select(function => (FunctionDecl)function.syntax));

        foreach (var type in types)
            entries.AddRange(type.methods
                .Where(method => method.body is not null)
                .Select(create_inference_declaration));

        foreach (var trait in traits)
            entries.AddRange(trait.methods
                .Where(method => method.body is not null)
                .Select(create_inference_declaration));

        foreach (var imply in implys)
            entries.AddRange(imply.methods
                .Where(method => method.body is not null)
                .Select(create_inference_declaration));

        return entries;
    }

    private static HirFunction build_function(FunctionDecl declaration, string? currentNamespace)
    {
        var normalized = normalize_function(declaration, currentNamespace);
        return new HirFunctionDef(
            normalized.name?.name ?? string.Empty,
            normalized,
            build_parameters(normalized.parameters),
            get_declared_return_type_ref(normalized.return_type),
            HirCallableSemantics.from_annotations(normalized.annotations),
            build_surface_attributes(normalized.annotations),
            currentNamespace,
            EffectSet.pure);
    }

    private static HirLetDefs build_let(LetDeclaration declaration, string? currentNamespace)
    {
        var name = declaration.name?.name ?? string.Empty;
        var namepath = SemanticNamePath.qualify(
            ValkyrieNameSpace.parse(currentNamespace),
            null,
            ValkyrieNamePath.parse(name));
        return new HirLetDefs(
            namepath,
            declaration,
            get_let_type_ref(declaration),
            build_surface_attributes(declaration.annotations));
    }

    private static HirFunction rebuild_function(HirFunction function, ValkyrieTypeInference typeInference)
    {
        return new HirFunctionDef(
            function.name,
            (FunctionDecl)function.syntax,
            function.parameters,
            infer_return_type_ref((FunctionDecl)function.syntax, typeInference),
            function.semantics,
            function.surface_attributes,
            function.namespace_name,
            function.effect_set);
    }

    private static HirTypeRef get_let_type_ref(LetDeclaration declaration)
    {
        if (declaration.var_type is not null)
        {
            return get_type_ref(declaration.var_type);
        }

        if (declaration.initializer is TermLiteralObjectNode objectLiteral &&
            try_resolve_let_initializer_type(objectLiteral.constructor, out var initializerType))
        {
            return HirTypeRef.from_name_path(initializerType);
        }

        return HirTypeRef.unknown();
    }

    private static bool try_resolve_let_initializer_type(AstNode constructor, out SemanticNamePath namePath)
    {
        switch (constructor)
        {
            case IdentifierNode { name: { Length: > 0 } identifierName }:
                namePath = ValkyrieNamePath.parse(identifierName);
                return true;
            case QualifiedPathNode { full_name: { Length: > 0 } qualifiedName }:
                namePath = ValkyrieNamePath.parse(qualifiedName);
                return true;
            case TypeLiteralNamePathNode { path.full_name: { Length: > 0 } typePathName }:
                namePath = ValkyrieNamePath.parse(typePathName);
                return true;
            case TermLiteralNamePathNode { path.full_name: { Length: > 0 } literalName }:
                namePath = ValkyrieNamePath.parse(literalName);
                return true;
            default:
                namePath = ValkyrieNamePath.parse(null);
                return false;
        }
    }

    private static IReadOnlyList<HirMethod> build_methods(
        ObjectBody? body,
        string? currentNamespace,
        string? ownerName,
        HirTypeRef ownerType,
        HirTypeRef? contractType,
        HirMethodKind kind)
    {
        if (body is null || string.IsNullOrWhiteSpace(ownerName)) return [];

        return
        [
            .. body.methods
                .Select((method, index) => build_method(
                    method,
                    currentNamespace,
                    ownerName,
                    ownerType,
                    contractType,
                    kind,
                    kind == HirMethodKind.trait ? index : null))
        ];
    }

    private static HirDataShape? build_data_shape(SemanticModel semantics, string qualifiedName)
    {
        var dataType = semantics.find_data_type(ValkyrieNamePath.parse(qualifiedName));
        if (dataType is null) return null;

        return new HirDataShape(
            HirDataContainerKind.object_fields,
            [
                .. dataType.fields
                    .Select(field => new HirDataField(
                        field.name,
                        HirTypeRef.from_type(field.field_type, field.field_type.name),
                        field.binding_name,
                        field.aliases,
                        field.ignore,
                        field.flatten,
                        field.order,
                        field.skip_when_null,
                        field.skip_when_default))
            ]);
    }

    private static HirMethod build_method(
        DeclareObjectMethod declaration,
        string? currentNamespace,
        string ownerName,
        HirTypeRef ownerType,
        HirTypeRef? contractType,
        HirMethodKind kind,
        int? slotIndex)
    {
        var memberName = build_method_member_name(declaration);
        var implementationName = build_method_implementation_name(declaration);
        var qualifiedName = build_qualified_name(currentNamespace, ownerName, implementationName);
        return new HirMethodDef(
            qualifiedName,
            memberName,
            declaration,
            build_parameters(declaration.parameters),
            get_declared_return_type_ref(declaration.return_type),
            HirCallableSemantics.from_annotations(declaration.annotations) with { is_logical_entry = false },
            build_surface_attributes(declaration.annotations),
            currentNamespace,
            ownerType,
            contractType,
            kind,
            slotIndex,
            EffectSet.pure);
    }

    private static HirMethod rebuild_method(HirMethod method, ValkyrieTypeInference typeInference)
    {
        return new HirMethodDef(
            method.name,
            method.member_name,
            (DeclareObjectMethod)method.syntax,
            method.parameters,
            infer_return_type_ref(create_inference_declaration(method), typeInference),
            method.semantics,
            method.surface_attributes,
            method.namespace_name,
            method.owner_type,
            method.contract_type,
            method.kind,
            method.slot_index,
            method.effect_set);
    }

    private static IReadOnlyList<HirFunction> uniquify_function_names(IReadOnlyList<HirFunction> functions)
    {
        return uniquify_callables(
            functions,
            static function => function.name,
            static function => function.parameters,
            rename_function);
    }

    private static IReadOnlyList<HirMethod> uniquify_method_names(IReadOnlyList<HirMethod> methods)
    {
        return uniquify_callables(
            methods,
            static method => method.name,
            static method => method.parameters,
            rename_method);
    }

    private static IReadOnlyList<TCallable> uniquify_callables<TCallable>(
        IReadOnlyList<TCallable> callables,
        Func<TCallable, string> getName,
        Func<TCallable, IReadOnlyList<HirSymbolRef>> getParameters,
        Func<TCallable, string, TCallable> rename)
    {
        var duplicates = callables
            .GroupBy(getName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        if (duplicates.Count == 0) return callables;

        return
        [
            .. callables
                .Select(callable =>
                {
                    var name = getName(callable);
                    if (!duplicates.TryGetValue(name, out var overloads)) return callable;

                    var suffix = build_overload_suffix(getParameters(callable));
                    var uniqueName = name + suffix;
                    if (overloads.Count(candidate =>
                            string.Equals(getName(candidate), uniqueName, StringComparison.Ordinal)) >
                        0)
                        uniqueName += "__dup";

                    return rename(callable, uniqueName);
                })
        ];
    }

    private static (List<HirFunction> functions, List<HirTypeDef> types, List<HirTraitDef> traits, List<HirImplyDef>
        implys)
        uniquify_callable_names(
            IReadOnlyList<HirFunction> functions,
            IReadOnlyList<HirTypeDef> types,
            IReadOnlyList<HirTraitDef> traits,
            IReadOnlyList<HirImplyDef> implys)
    {
        var uniqueFunctions = uniquify_function_names(functions).ToList();
        var uniqueTypes = types
            .Select(type => type with
            {
                methods = [.. uniquify_method_names(type.methods)]
            })
            .ToList();
        var uniqueTraits = traits
            .Select(trait => trait with
            {
                methods = [.. uniquify_method_names(trait.methods)]
            })
            .ToList();
        var uniqueImplys = implys
            .Select(imply => imply with
            {
                methods = [.. uniquify_method_names(imply.methods)]
            })
            .ToList();

        var seenNames = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < uniqueFunctions.Count; index++)
            uniqueFunctions[index] = ensure_globally_unique_callable_name(uniqueFunctions[index], rename_function,
                seenNames);

        for (var index = 0; index < uniqueTypes.Count; index++)
            uniqueTypes[index] = uniqueTypes[index] with
            {
                methods =
                [
                    .. uniqueTypes[index].methods
                        .Select(method => ensure_globally_unique_callable_name(method, rename_method, seenNames))
                ]
            };

        for (var index = 0; index < uniqueTraits.Count; index++)
            uniqueTraits[index] = uniqueTraits[index] with
            {
                methods =
                [
                    .. uniqueTraits[index].methods
                        .Select(method => ensure_globally_unique_callable_name(method, rename_method, seenNames))
                ]
            };

        for (var index = 0; index < uniqueImplys.Count; index++)
            uniqueImplys[index] = uniqueImplys[index] with
            {
                methods =
                [
                    .. uniqueImplys[index].methods
                        .Select(method => ensure_globally_unique_callable_name(method, rename_method, seenNames))
                ]
            };

        return (uniqueFunctions, uniqueTypes, uniqueTraits, uniqueImplys);
    }

    private static TCallable ensure_globally_unique_callable_name<TCallable>(
        TCallable callable,
        Func<TCallable, string, TCallable> rename,
        IDictionary<string, int> seenNames)
        where TCallable : HirCallable
    {
        if (!seenNames.TryGetValue(callable.name, out var duplicateCount))
        {
            seenNames[callable.name] = 1;
            return callable;
        }

        seenNames[callable.name] = duplicateCount + 1;
        return rename(callable, $"{callable.name}__dup{duplicateCount + 1}");
    }

    private static HirFunction rename_function(HirFunction function, string name)
    {
        return new HirFunctionDef(
            name,
            (FunctionDecl)function.syntax,
            function.parameters,
            function.return_type,
            function.semantics,
            function.surface_attributes,
            function.namespace_name,
            function.effect_set);
    }

    private static HirMethod rename_method(HirMethod method, string name)
    {
        return new HirMethodDef(
            name,
            method.member_name,
            (DeclareObjectMethod)method.syntax,
            method.parameters,
            method.return_type,
            method.semantics,
            method.surface_attributes,
            method.namespace_name,
            method.owner_type,
            method.contract_type,
            method.kind,
            method.slot_index,
            method.effect_set);
    }

    private static string build_overload_suffix(IReadOnlyList<HirSymbolRef> parameters)
    {
        if (parameters.Count == 0) return "__ovl_void";

        return "__ovl_" + string.Join("__", parameters
            .Select(parameter => sanitize_generated_name(parameter.type.display_name)));
    }

    private static HirTypeRef infer_return_type_ref(DeclareMicro declaration, ValkyrieTypeInference typeInference)
    {
        var functionType = typeInference.build_function_type(declaration);
        if (functionType is FunctionType signature)
            return HirTypeRef.from_type(signature.return_type, signature.return_type.name);

        return HirTypeRef.from_type(functionType, functionType.name);
    }

    #endregion

    #region trait 默认实现合成

    private static List<HirImplyDef> synthesize_trait_default_implys(
        IReadOnlyList<HirTypeDef> types,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases,
        IReadOnlyList<HirImplyDef> implys)
    {
        var synthesizedImplys = implys.ToList();

        foreach (var type in types.Where(candidate => candidate is HirClassDef or HirStructDef))
        foreach (var trait in traits)
        {
            if (!can_satisfy_trait(type, trait, types, traits, traitAliases, synthesizedImplys)) continue;

            var defaultMethods = collect_trait_default_methods(trait, traits, traitAliases)
                .Where(defaultMethod =>
                    !has_concrete_implementation(type, trait, defaultMethod, types, traits, traitAliases,
                        synthesizedImplys))
                .Select(defaultMethod => clone_trait_default_method(type, trait, defaultMethod))
                .ToArray();
            if (defaultMethods.Length == 0) continue;

            var existingImplyIndex = synthesizedImplys.FindIndex(candidate =>
                type_ref_matches(candidate.target_type, type) &&
                candidate.contract_type is not null &&
                contract_type_matches_trait(candidate.contract_type, candidate.namespace_name, trait, traits,
                    traitAliases));
            if (existingImplyIndex >= 0)
            {
                var existingImply = synthesizedImplys[existingImplyIndex];
                synthesizedImplys[existingImplyIndex] = existingImply with
                {
                    methods = [.. existingImply.methods, .. defaultMethods]
                };
                continue;
            }

            synthesizedImplys.Add(new HirImplyDef(
                HirTypeRef.named(type.namepath),
                HirTypeRef.named(trait.name_path),
                type.namepath.@namespace.is_empty ? null : type.namepath.@namespace.ToString(),
                defaultMethods,
                [],
                [],
                HirImplySourceKind.synthesized_default));
        }

        return synthesizedImplys;
    }

    private static bool can_satisfy_trait(
        HirTypeDef type,
        HirTraitDef trait,
        IReadOnlyList<HirTypeDef> types,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases,
        IReadOnlyList<HirImplyDef> implys)
    {
        foreach (var contractMethod in collect_trait_contract_methods(trait, traits, traitAliases))
        {
            if (has_concrete_implementation(type, trait, contractMethod, types, traits, traitAliases, implys)) continue;

            if (contractMethod.body is not null) continue;

            return false;
        }

        return true;
    }

    private static bool has_concrete_implementation(
        HirTypeDef type,
        HirTraitDef trait,
        HirMethod contractMethod,
        IReadOnlyList<HirTypeDef> types,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases,
        IReadOnlyList<HirImplyDef> implys)
    {
        return find_inherent_method(type, contractMethod, types) is not null
               || find_imply_method(type, trait, contractMethod, traits, traitAliases, implys) is not null;
    }

    private static HirMethod? find_inherent_method(
        HirTypeDef type,
        HirMethod contractMethod,
        IReadOnlyList<HirTypeDef> types)
    {
        foreach (var candidateType in enumerate_type_hierarchy(type, types))
        {
            var method =
                candidateType.methods.FirstOrDefault(candidate => signature_matches(candidate, contractMethod));
            if (method is not null) return method;
        }

        return null;
    }

    private static HirMethod? find_imply_method(
        HirTypeDef type,
        HirTraitDef trait,
        HirMethod contractMethod,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases,
        IReadOnlyList<HirImplyDef> implys)
    {
        var imply = implys.FirstOrDefault(candidate =>
            type_ref_matches(candidate.target_type, type) &&
            candidate.contract_type is not null &&
            contract_type_matches_trait(candidate.contract_type, candidate.namespace_name, trait, traits,
                traitAliases));
        return imply?.methods.FirstOrDefault(candidate => signature_matches(candidate, contractMethod));
    }

    private static IEnumerable<HirTypeDef> enumerate_type_hierarchy(HirTypeDef rootType,
        IReadOnlyList<HirTypeDef> types)
    {
        var visited = new HashSet<SemanticNamePath>();
        var pending = new Queue<HirTypeDef>();
        pending.Enqueue(rootType);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (!visited.Add(current.namepath)) continue;

            yield return current;

            foreach (var inheritedType in current.inherited_types)
            {
                var resolvedBaseType = resolve_type(
                    types,
                    inheritedType.name_path,
                    current.namepath.@namespace.is_empty ? null : current.namepath.@namespace.ToString());
                if (resolvedBaseType is not null) pending.Enqueue(resolvedBaseType);
            }
        }
    }

    private static IReadOnlyList<HirMethod> collect_trait_contract_methods(
        HirTraitDef trait,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases)
    {
        var methods = new Dictionary<string, HirMethod>(StringComparer.Ordinal);

        foreach (var baseTrait in trait.base_traits)
        foreach (var resolvedBaseTrait in resolve_trait_targets(
                     traits,
                     traitAliases,
                     baseTrait.name_path,
                     trait.name_path.@namespace.is_empty ? null : trait.name_path.@namespace.ToString()))
        foreach (var inheritedMethod in collect_trait_contract_methods(resolvedBaseTrait, traits, traitAliases))
            methods[inheritedMethod.member_name] = inheritedMethod;

        foreach (var method in trait.methods) methods[method.member_name] = method;

        return [.. methods.Values];
    }

    private static IReadOnlyList<HirMethod> collect_trait_default_methods(
        HirTraitDef trait,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases)
    {
        return
        [
            .. collect_trait_contract_methods(trait, traits, traitAliases)
                .Where(method => method.body is not null)
        ];
    }

    private static HirMethod clone_trait_default_method(HirTypeDef type, HirTraitDef trait, HirMethod method)
    {
        var synthesizedName = build_qualified_name(
            type.namepath.@namespace.is_empty ? null : type.namepath.@namespace.ToString(),
            type.name,
            $"__trait_{sanitize_generated_name(trait.name)}_{method.member_name}");
        return new HirMethodDef(
            synthesizedName,
            method.member_name,
            (DeclareObjectMethod)method.syntax,
            method.parameters,
            method.return_type,
            method.semantics,
            method.surface_attributes,
            type.namepath.@namespace.is_empty ? null : type.namepath.@namespace.ToString(),
            HirTypeRef.named(type.namepath),
            HirTypeRef.named(trait.name_path),
            HirMethodKind.imply,
            method.slot_index,
            method.effect_set);
    }

    private static string sanitize_generated_name(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_";

        var builder = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                builder.Append(ch);
                continue;
            }

            builder.Append("_u");
            builder.Append(((int)ch).ToString("X4"));
        }

        return builder.ToString();
    }

    private static string build_method_member_name(DeclareObjectMethod declaration)
    {
        var baseName = declaration.name?.name ?? string.Empty;
        var operatorModifier = try_get_operator_modifier(declaration.annotations);
        if (operatorModifier is null) return baseName;

        return $"{operatorModifier} {baseName}";
    }

    private static string build_method_implementation_name(DeclareObjectMethod declaration)
    {
        var baseName = declaration.name?.name ?? string.Empty;
        var operatorModifier = try_get_operator_modifier(declaration.annotations);
        if (operatorModifier is null) return baseName;

        return $"__operator_{sanitize_generated_name(operatorModifier)}_{sanitize_generated_name(baseName)}";
    }

    private static string? try_get_operator_modifier(Annotations annotations)
    {
        foreach (var modifier in annotations.modifier_texts())
            if (string.Equals(modifier, "infix", StringComparison.Ordinal) ||
                string.Equals(modifier, "prefix", StringComparison.Ordinal) ||
                string.Equals(modifier, "postfix", StringComparison.Ordinal))
                return modifier;

        return null;
    }

    #endregion

    #region witness 绑定

    private static List<HirImplyDef> resolve_witness_bindings(
        IReadOnlyList<HirImplyDef> implys,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases)
    {
        return
        [
            .. implys
                .Select(imply => imply with { witness_bindings = build_witness_bindings(imply, traits, traitAliases) })
        ];
    }

    private static IReadOnlyList<HirWitnessBinding> build_witness_bindings(
        HirImplyDef imply,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases)
    {
        if (imply.contract_type is null) return [];

        var contract = resolve_trait(traits, traitAliases, imply.contract_type.name_path, imply.namespace_name);
        if (contract is null) return [];

        var bindings = new List<HirWitnessBinding>();
        foreach (var traitMethod in contract.methods)
        {
            var implementation = imply.methods.FirstOrDefault(method =>
                string.Equals(method.member_name, traitMethod.member_name, StringComparison.Ordinal));
            if (implementation is null || traitMethod.slot_index is null) continue;

            var slottedImplementation = new HirMethodDef(
                implementation.name,
                implementation.member_name,
                (DeclareObjectMethod)implementation.syntax,
                implementation.parameters,
                implementation.return_type,
                implementation.semantics,
                implementation.surface_attributes,
                implementation.namespace_name,
                implementation.owner_type,
                implementation.contract_type,
                implementation.kind,
                traitMethod.slot_index,
                implementation.effect_set);
            bindings.Add(new HirWitnessBinding(contract.name_path, traitMethod.slot_index.Value,
                traitMethod.member_name, slottedImplementation));
        }

        return bindings;
    }

    #endregion

    #region 继承校验

    private static void validate_class_inheritance(
        IReadOnlyList<HirTypeDef> types,
        IReadOnlyDictionary<string, DeclareClass> classDeclarations,
        SemanticModel semantics)
    {
        foreach (var (qualifiedName, declaration) in classDeclarations)
        {
            validate_duplicate_inherited_types(qualifiedName, declaration, semantics);
            validate_inherited_field_names(declaration, semantics);
        }

        validate_inheritance_cycles(types, classDeclarations, semantics);
    }

    private static void validate_duplicate_inherited_types(
        string qualifiedName,
        DeclareClass declaration,
        SemanticModel semantics)
    {
        var inheritanceItems = declaration.inheritance?.bases ?? [];
        if (inheritanceItems.Count == 0) return;

        var groupedByOuterType = inheritanceItems
            .GroupBy(item => extract_outer_type_name(get_type_name(item.base_type)), StringComparer.Ordinal);
        foreach (var group in groupedByOuterType)
        {
            if (group.Count() < 2) continue;

            if (group.Any(item => string.IsNullOrWhiteSpace(item.name?.name)))
                semantics.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.error,
                    $"类型 `{qualifiedName}` 重复继承 `{group.Key}` 时必须显式指定字段名�?,
                    build_source_span(declaration),
                    "VALK3001",
                    semantics.file_path));
        }
    }

    private static void validate_inherited_field_names(DeclareClass declaration, SemanticModel semantics)
    {
        var inheritanceItems = declaration.inheritance?.bases ?? [];
        if (inheritanceItems.Count == 0) return;

        var ownedFieldNames = new HashSet<string>(
            declaration.body?.fields.Select(field => field.name) ?? [],
            StringComparer.Ordinal);
        var inheritedFieldNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in inheritanceItems)
        {
            var baseTypeName = extract_outer_type_name(get_type_name(item.base_type));
            var generatedFieldName = item.name?.name ?? to_snake_case(baseTypeName);

            if (!inheritedFieldNames.Add(generatedFieldName))
                semantics.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.error,
                    $"类型 `{declaration.name?.name}` 的继承字段名 `{generatedFieldName}` 冲突�?,
                    build_source_span(item),
                    "VALK3003",
                    semantics.file_path));

            if (ownedFieldNames.Contains(generatedFieldName))
                semantics.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.error,
                    $"类型 `{declaration.name?.name}` 的继承字段名 `{generatedFieldName}` 与自有字段冲突�?,
                    build_source_span(item),
                    "VALK3002",
                    semantics.file_path));
        }
    }

    private static void validate_inheritance_cycles(
        IReadOnlyList<HirTypeDef> types,
        IReadOnlyDictionary<string, DeclareClass> classDeclarations,
        SemanticModel semantics)
    {
        var classTypes = types
            .OfType<HirClassDef>()
            .ToDictionary(type => type.namepath, type => type);
        var visiting = new HashSet<SemanticNamePath>();
        var visited = new HashSet<SemanticNamePath>();

        foreach (var type in classTypes.Values)
            detect_inheritance_cycle(type, classTypes, classDeclarations, semantics, visiting, visited);
    }

    private static void detect_inheritance_cycle(
        HirTypeDef type,
        IReadOnlyDictionary<SemanticNamePath, HirClassDef> classTypes,
        IReadOnlyDictionary<string, DeclareClass> classDeclarations,
        SemanticModel semantics,
        ISet<SemanticNamePath> visiting,
        ISet<SemanticNamePath> visited)
    {
        if (visited.Contains(type.namepath)) return;

        if (!visiting.Add(type.namepath))
        {
            semantics.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.error,
                $"类型 `{type.namepath}` 存在循环继承�?,
                classDeclarations.TryGetValue(type.namepath.ToString(), out var declaration)
                    ? build_source_span(declaration)
                    : default,
                "VALK3004",
                semantics.file_path));
            return;
        }

        foreach (var inheritedType in type.inherited_types)
        {
            var resolvedBaseType = resolve_type(
                [.. classTypes.Values],
                inheritedType.name_path,
                type.namepath.@namespace.is_empty ? null : type.namepath.@namespace.ToString());
            if (resolvedBaseType is not null)
                detect_inheritance_cycle(resolvedBaseType, classTypes, classDeclarations, semantics, visiting, visited);
        }

        visiting.Remove(type.namepath);
        visited.Add(type.namepath);
    }

    #endregion

    #region 辅助方法

    private static HirTraitDef? try_resolve_trait(
        SemanticNamePath traitName,
        string? currentNamespace,
        IReadOnlyList<HirTraitDef> traits)
    {
        var currentNamespacePath = ValkyrieNameSpace.parse(currentNamespace);
        return traits.FirstOrDefault(trait => trait.matches_name(traitName, currentNamespacePath));
    }

    private static HirTraitDef? resolve_trait(
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases,
        SemanticNamePath traitName,
        string? currentNamespace)
    {
        var targets = resolve_trait_targets(traits, traitAliases, traitName, currentNamespace);
        return targets.Count == 1 ? targets[0] : null;
    }

    private static IReadOnlyList<HirTraitDef> resolve_trait_targets(
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases,
        SemanticNamePath traitName,
        string? currentNamespace)
    {
        return resolve_trait_targets(
            traits,
            traitAliases,
            traitName,
            currentNamespace,
            new HashSet<SemanticNamePath>());
    }

    private static IReadOnlyList<HirTraitDef> resolve_trait_targets(
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases,
        SemanticNamePath traitName,
        string? currentNamespace,
        ISet<SemanticNamePath> visitedAliases)
    {
        var direct = try_resolve_trait(traitName, currentNamespace, traits);
        if (direct is not null) return [direct];

        var currentNamespacePath = ValkyrieNameSpace.parse(currentNamespace);
        var alias = traitAliases.FirstOrDefault(candidate =>
            HirTypeRef.matches_name_path(
                candidate.simple_name_path,
                candidate.name_path,
                traitName,
                currentNamespacePath));
        if (alias is null || !visitedAliases.Add(alias.name_path)) return [];

        return
        [
            .. alias.target_traits
                .SelectMany(target =>
                    resolve_trait_targets(traits, traitAliases, target.name_path, currentNamespace, visitedAliases))
                .GroupBy(trait => trait.name_path)
                .Select(group => group.First())
        ];
    }

    private static HirTypeDef? resolve_type(
        IReadOnlyList<HirTypeDef> types,
        SemanticNamePath typeName,
        string? currentNamespace)
    {
        var currentNamespacePath = ValkyrieNameSpace.parse(currentNamespace);
        return types.FirstOrDefault(type =>
            HirTypeRef.matches_name_path(
                type.namepath,
                type.namepath,
                typeName,
                currentNamespacePath));
    }

    private static bool type_ref_matches(HirTypeRef typeRef, HirTypeDef type)
    {
        return HirTypeRef.matches_name_path(
            type.namepath,
            type.namepath,
            typeRef.name_path);
    }

    private static bool type_ref_matches(HirTypeRef typeRef, HirTraitDef trait)
    {
        return HirTypeRef.matches_name_path(
            new([trait.name]),
            trait.name_path,
            typeRef.name_path);
    }

    private static bool contract_type_matches_trait(
        HirTypeRef contractType,
        string? currentNamespace,
        HirTraitDef trait,
        IReadOnlyList<HirTraitDef> traits,
        IReadOnlyList<HirTraitAliasRef> traitAliases)
    {
        if (type_ref_matches(contractType, trait)) return true;

        var resolved = resolve_trait(traits, traitAliases, contractType.name_path, currentNamespace);
        return resolved is not null &&
               resolved.name_path.semantically_equals(trait.name_path);
    }

    private static bool signature_matches(HirMethod implementation, HirMethod contractMethod)
    {
        if (!string.Equals(implementation.member_name, contractMethod.member_name, StringComparison.Ordinal) ||
            !implementation.return_type.semantically_equals(contractMethod.return_type) ||
            implementation.parameters.Count != contractMethod.parameters.Count)
            return false;

        for (var index = 0; index < implementation.parameters.Count; index++)
            if (!implementation.parameters[index].type.semantically_equals(contractMethod.parameters[index].type))
                return false;

        return true;
    }

    private static IReadOnlyList<HirTypeRef> build_inherited_types(InheritanceList? inheritance)
    {
        if (inheritance is null || inheritance.bases.Count == 0) return [];

        return
        [
            .. inheritance.bases
                .Select(item => get_type_ref(item.base_type))
                .Where(type => !string.IsNullOrWhiteSpace(type.name))
        ];
    }

    private static IReadOnlyList<HirInheritanceEdge> build_inheritance_edges(InheritanceList? inheritance)
    {
        if (inheritance is null || inheritance.bases.Count == 0) return [];

        var edges = new List<HirInheritanceEdge>(inheritance.bases.Count);
        foreach (var item in inheritance.bases)
        {
            var baseType = get_type_ref(item.base_type);
            if (string.IsNullOrWhiteSpace(baseType.name)) continue;

            var baseTypeName = extract_outer_type_name(get_type_name(item.base_type));
            var fieldName = item.name?.name ?? to_snake_case(baseTypeName);
            var storageKind = get_inheritance_storage_kind(item);
            var storageType = storageKind == HirInheritanceStorageKind.physical
                ? baseType
                : HirTypeRef.named($"PhantomData<{baseType.name}>");
            edges.Add(new HirInheritanceEdge(fieldName, baseType, storageType, storageKind));
        }

        return edges;
    }

    private static HirInheritanceStorageKind get_inheritance_storage_kind(InheritanceItem item)
    {
        return item.annotations.modifier_texts()
            .Any(modifier => string.Equals(modifier, "virtual", StringComparison.Ordinal))
            ? HirInheritanceStorageKind.phantom
            : HirInheritanceStorageKind.physical;
    }

    private static DeclareMicro create_inference_declaration(HirMethod method)
    {
        var syntax = (DeclareObjectMethod)method.syntax;
        return new DeclareMicro
        {
            annotations = syntax.annotations,
            name = new IdentifierNode(method.name),
            parameters = syntax.parameters,
            return_type = syntax.return_type,
            body = syntax.body,
            span = syntax.span
        };
    }

    private static IReadOnlyList<HirSymbolRef> build_parameters(IReadOnlyList<TermParameterList> parameters)
    {
        return
        [
            .. parameters
                .SelectMany(parameterList => parameterList.items)
                .Select(parameter => new HirSymbolRef(
                    parameter.name?.name ?? string.Empty,
                    get_type_ref(parameter.bound_type)))
        ];
    }

    private static IReadOnlyList<HirAttribute> build_surface_attributes(Annotations annotations)
    {
        return
        [
            .. annotations.attributes()
                .Where(attr => !string.Equals(attr.name, "intrinsic", StringComparison.Ordinal) &&
                               !string.Equals(attr.name, "main", StringComparison.Ordinal))
                .Select(attr => new HirAttribute(attr.name,
                    attr.arguments?.items.Select(a => extract_text_literal_value(a.value)).ToArray() ?? []))
        ];
    }

    /// <summary>
    ///     从属性参数值中提取文本字面量内容�?
    ///     处理 <see cref="TermLiteralTextNode" /> 和普通字符串�?
    /// </summary>
    private static string extract_text_literal_value(AstNode? node)
    {
        return node switch
        {
            TermLiteralTextNode textNode => textNode.value,
            _ => node?.ToString() ?? string.Empty
        };
    }

    private static CompilationUnit normalize_syntax_unit(CompilationUnit syntax)
    {
        return syntax with
        {
            declarations =
            [
                .. syntax.declarations
                    .Select(normalize_declaration)
            ]
        };
    }

    private static AstNode normalize_declaration(AstNode declaration)
    {
        return declaration switch
        {
            DeclareNamespace namespaceDecl => namespaceDecl with
            {
                declarations =
                [
                    .. namespaceDecl.declarations
                        .Select(normalize_declaration)
                ]
            },
            DeclareMicro functionDecl => functionDecl with
            {
                body = normalize_function_body(functionDecl.body)
            },
            DeclareClass classDecl => classDecl with
            {
                body = normalize_object_body(classDecl.body)
            },
            DeclareStructure structDecl => structDecl with
            {
                body = normalize_object_body(structDecl.body)
            },
            DeclareTrait traitDecl => traitDecl with
            {
                body = normalize_object_body(traitDecl.body)
            },
            DeclareImply implyDecl => implyDecl with
            {
                methods =
                [
                    .. implyDecl.methods
                        .Select(normalize_object_method)
                ]
            },
            DeclareUnite uniteDecl => uniteDecl with
            {
                methods =
                [
                    .. uniteDecl.methods
                        .Select(normalize_object_method)
                ]
            },
            _ => declaration
        };
    }

    private static ObjectBody? normalize_object_body(ObjectBody? body)
    {
        if (body is null) return null;

        return body with
        {
            methods =
            [
                .. body.methods
                    .Select(normalize_object_method)
            ]
        };
    }

    private static DeclareObjectMethod normalize_object_method(DeclareObjectMethod method)
    {
        return method with
        {
            body = normalize_function_body(method.body)
        };
    }

    private static BlockStmt? normalize_function_body(BlockStmt? body)
    {
        if (body is null) return null;

        return body with
        {
            statements =
            [
                .. body.statements
                    .Select(normalize_statement)
            ]
        };
    }

    private static AstNode normalize_statement(AstNode statement)
    {
        return statement switch
        {
            AssignmentStatement assignment => assignment with
            {
                @operator = TermBinaryOperator.assign,
                target = normalize_assignment_target(assignment.target),
                value = normalize_assignment_value(assignment)
            },
            DeclareLet let => let with
            {
                initializer = normalize_expression_node(let.initializer)
            },
            ReturnStatement ret => ret with
            {
                value = normalize_expression_node(ret.value)
            },
            BreakStatement or ContinueStatement => statement,
            ResumeStatement resume => resume with
            {
                value = normalize_expression_node(resume.value)
            },
            YieldStatement yieldStmt => desugar_yield(yieldStmt),
            RaiseStatement raiseStmt => raiseStmt with
            {
                value = normalize_term_node(raiseStmt.value)
            },
            TryStatement tryStmt => tryStmt with
            {
                body = normalize_function_body(tryStmt.body) ?? tryStmt.body
            },
            IfStatement ifStmt => ifStmt with
            {
                condition = normalize_expression_node(ifStmt.condition) ?? ifStmt.condition,
                then_block = normalize_function_body(ifStmt.then_block) ?? ifStmt.then_block,
                else_block = normalize_embedded_node(ifStmt.else_block)
            },
            WhileStatement whileStmt => whileStmt with
            {
                condition = normalize_expression_node(whileStmt.condition) ?? whileStmt.condition,
                body = normalize_function_body(whileStmt.body) ?? whileStmt.body
            },
            UntilStatement untilStmt => untilStmt with
            {
                condition = normalize_expression_node(untilStmt.condition) ?? untilStmt.condition,
                body = normalize_function_body(untilStmt.body) ?? untilStmt.body
            },
            LoopStatement loop => loop with
            {
                initializer = normalize_embedded_node(loop.initializer),
                condition = normalize_expression_node(loop.condition),
                update = normalize_embedded_node(loop.update),
                body = normalize_function_body(loop.body) ?? loop.body
            },
            LoopInStatement loopIn => loopIn with
            {
                iterable = normalize_expression_node(loopIn.iterable),
                body = normalize_function_body(loopIn.body) ?? loopIn.body
            },
            MatchStatementNode matchStmt => matchStmt with
            {
                expression = normalize_term_node(matchStmt.expression),
                arms =
                [
                    .. matchStmt.arms
                        .Select(normalize_arm)
                ]
            },
            CatchStatementNode catchStmt => catchStmt with
            {
                expression = normalize_term_node(catchStmt.expression),
                arms =
                [
                    .. catchStmt.arms
                        .Select(normalize_arm)
                ]
            },
            TermNode term => normalize_term_statement(term),
            _ => statement
        };
    }

    private static AstNode? normalize_embedded_node(AstNode? node)
    {
        return node switch
        {
            null => null,
            FunctionBody body => normalize_function_body(body),
            _ => normalize_statement(node)
        };
    }

    private static MatchArm normalize_arm(MatchArm arm)
    {
        return arm with
        {
            body = normalize_function_body(arm.body)
        };
    }

    private static AstNode? normalize_expression_node(AstNode? node)
    {
        return node switch
        {
            null => null,
            TermNode term => normalize_term_node(term),
            _ => node
        };
    }

    private static TermNode normalize_term_node(TermNode term)
    {
        return term switch
        {
            TermBinaryExpression binary => normalize_binary_expression(binary),
            TermUnaryExpression unary => normalize_unary_expression(unary),
            TermCallExpression call => call with
            {
                caller = normalize_term_node(call.caller),
                call_body = normalize_call_body(call.call_body)
            },
            TermDotExpression dot => dot with
            {
                caller = normalize_term_node(dot.caller),
                call_body = normalize_call_body(dot.call_body)
            },
            TermLiteralArrayNode array => array with
            {
                elements =
                [
                    .. array.elements
                        .Select(normalize_term_node)
                ]
            },
            TermLiteralObjectNode objectLiteral => objectLiteral with
            {
                constructor = normalize_expression_node(objectLiteral.constructor) ?? objectLiteral.constructor,
                fields =
                [
                    .. objectLiteral.fields
                        .Select(normalize_object_field)
                ]
            },
            TermAsExpression cast => desugar_as_expression(cast),
            TermIsExpression isExpression => desugar_is_expression(isExpression),
            TermInExpression inExpression => desugar_in_expression(inExpression),
            TermCatchExpression catchExpr => catchExpr with
            {
                operand = normalize_term_node(catchExpr.operand),
                arms =
                [
                    .. catchExpr.arms
                        .Select(a => a is ArmCaseNode caseArm
                            ? caseArm with { body = caseArm.body is null ? null : normalize_function_body(caseArm.body) }
                            : (ArmNode)(a is ArmElseNode elseArm
                                ? elseArm with
                                {
                                    body = elseArm.body is null ? null : normalize_function_body(elseArm.body)
                                }
                                : a))
                ]
            },
            TermMatchExpression matchExpr => matchExpr with
            {
                operand = normalize_term_node(matchExpr.operand),
                arms =
                [
                    .. matchExpr.arms
                        .Select(a => a is ArmCaseNode caseArm
                            ? caseArm with { body = caseArm.body is null ? null : normalize_function_body(caseArm.body) }
                            : (ArmNode)(a is ArmElseNode elseArm
                                ? elseArm with
                                {
                                    body = elseArm.body is null ? null : normalize_function_body(elseArm.body)
                                }
                                : a))
                ]
            },
            // TermSpreadExpression spread => desugar_spread_expression(spread), // 暂时禁用：预构建 Oak.dll 缺少 TermSpreadExpression 类型
            TermOffsetExpression offset => offset with
            {
                target = normalize_expression_node(offset.target) ?? offset.target,
                indices =
                [
                    .. offset.indices
                        .Select(index => normalize_expression_node(index) ?? index)
                ]
            },
            TermOrdinalExpression ordinal => ordinal with
            {
                target = normalize_expression_node(ordinal.target) ?? ordinal.target,
                indices =
                [
                    .. ordinal.indices
                        .Select(index => normalize_expression_node(index) ?? index)
                ]
            },
            _ => term
        };
    }

    /// <summary>
    ///     �?as/as? 表达式解糖为对应的方法调用或保留原样
    /// </summary>
    private static TermNode desugar_as_expression(TermAsExpression cast)
    {
        var operand = normalize_expression_node(cast.operand) ?? cast.operand;
        if (cast.is_nullable)
        {
            // as? 降级：尝试调�?as_type 方法，失败返�?null
            // 先保持简单：标记�?nullable cast，由 MIR 层处�?
        }

        return cast with { operand = operand };
    }

    /// <summary>
    ///     �?is/is? 表达式解�?
    /// </summary>
    private static TermNode desugar_is_expression(TermIsExpression isExpression)
    {
        var operand = normalize_expression_node(isExpression.operand) ?? isExpression.operand;
        return isExpression with { operand = operand };
    }

    /// <summary>
    ///     �?in 表达式解糖为方法调用或编译期检�?
    /// </summary>
    private static TermNode desugar_in_expression(TermInExpression inExpression)
    {
        var operand = normalize_expression_node(inExpression.operand) ?? inExpression.operand;
        var target = normalize_expression_node(inExpression.target) ?? inExpression.target;
        // in 目前保留原样，由 MIR 层根�?target 类型分发
        return inExpression with { operand = operand, target = target };
    }

    /// 暂时禁用：预构建 Oak.dll 缺少 TermSpreadExpression 类型
    // private static TermNode desugar_spread_expression(TermSpreadExpression spread)
    // {
    //     var target = normalize_expression_node(spread.Target);
    //     var targetTerm = target switch
    //     {
    //         null => throw new InvalidOperationException("展开表达式缺少目�?),
    //         TermNode term => term,
    //         _ => as_term_node(target)
    //     };
    //     var methodName = spread.IsDoubleDot ? "into_iter" : "into_values";
    //     return create_method_call(targetTerm, methodName, [], spread.Span);
    // }
    private static CallBody? normalize_call_body(CallBody? body)
    {
        if (body is null) return null;

        return body with
        {
            term_arguments = body.term_arguments is null
                ? null
                : body.term_arguments with
                {
                    items =
                    [
                        .. body.term_arguments.items
                            .Select(normalize_argument_item)
                    ]
                },
            function_body = normalize_function_body(body.function_body)
        };
    }

    private static TermArgumentItem normalize_argument_item(TermArgumentItem argument)
    {
        return argument with
        {
            value = normalize_term_node(argument.value)
        };
    }

    private static TermObjectField normalize_object_field(TermObjectField field)
    {
        return field with
        {
            value = normalize_expression_node(field.value)
        };
    }

    private static TermNode normalize_binary_expression(TermBinaryExpression binary)
    {
        var left = normalize_expression_node(binary.left) ?? binary.left;
        var right = normalize_expression_node(binary.right) ?? binary.right;

        if (binary.@operator == TermBinaryOperator.assign ||
            try_get_compound_assignment_member_name(binary.@operator, out _))
            throw new NotSupportedException(
                $"HIR ��Ӧ�ٰѸ�ֵ `{binary.@operator}` ��������ʽ����������������һ���׶�ת��Ϊ��ʽ `AssignmentStatement`��");

        if (binary.@operator is TermBinaryOperator.logical_and or TermBinaryOperator.logical_or)
            return new TermBinaryExpression(binary.@operator, as_term_node(left), as_term_node(right))
            {
                span = binary.span
            };

        if (try_get_binary_operator_member_name(binary.@operator, out var memberName))
            return create_method_call(as_term_node(left), memberName, [as_term_node(right)], binary.span);

        throw new NotSupportedException(
            $"HIR ��Ӧ�ٱ�����Ԫ���� `{binary.@operator}`�������� `AST -> HIR` ��һ���׶����?`operator -> method call` ���ǡ�");
    }

    private static TermNode normalize_unary_expression(TermUnaryExpression unary)
    {
        var operand = normalize_term_node(unary.operand);

        if (unary.@operator is TermUnaryOperator.try_operator)
            return desugar_try_operator(operand, unary.span);

        if (try_get_unary_operator_member_name(unary.@operator, out var memberName))
            return create_method_call(operand, memberName, [], unary.span);

        if (unary.@operator is TermUnaryOperator.increment or TermUnaryOperator.decrement)
            throw new NotSupportedException(
                $"HIR ��Ӧ�ٰ�����/�Լ� `{unary.@operator}` ��������ʽ����������������һ���׶�ת��Ϊ��ʽ `AssignmentStatement`��");

        throw new NotSupportedException(
            $"HIR ��Ӧ�ٱ���һԪ���� `{unary.@operator}`�������� `AST -> HIR` ��һ���׶����?`operator -> method call` ���ǡ�");
    }

    /// <summary>
    ///     �?<c>expr?</c> 解糖为：
    ///     <code>
    /// match expr {
    ///     case Fine(__try_value): __try_value
    ///     case Fail(__try_error): return Fail(__try_error)
    /// }
    ///     </code>
    /// </summary>
    private static TermMatchExpression desugar_try_operator(TermNode operand, TextSpan span)
    {
        var valueName = "__try_value";
        var errorName = "__try_error";

        var finePattern = new PatternLiteralObjectNode
        {
            path = new QualifiedPathNode
            {
                segments = [new IdentifierNode("Fine")]
            },
            fields =
            [
                new PatternLiteralFieldNode
                {
                    name = "",
                    pattern = new PatternLiteralVariableNode { name = valueName }
                }
            ]
        };

        var failPattern = new PatternLiteralObjectNode
        {
            path = new QualifiedPathNode
            {
                segments = [new IdentifierNode("Fail")]
            },
            fields =
            [
                new PatternLiteralFieldNode
                {
                    name = "",
                    pattern = new PatternLiteralVariableNode { name = errorName }
                }
            ]
        };

        var valueRef = new TermLiteralNamePathNode
        {
            path = new QualifiedPathNode
            {
                segments = [new IdentifierNode(valueName)]
            }
        };

        var errorRef = new TermLiteralNamePathNode
        {
            path = new QualifiedPathNode
            {
                segments = [new IdentifierNode(errorName)]
            }
        };

        var failCall = new TermCallExpression(
            new TermLiteralNamePathNode
            {
                path = new QualifiedPathNode
                {
                    segments = [new IdentifierNode("Fail")]
                }
            },
            new CallBody
            {
                term_arguments = new TermArgumentList
                {
                    items =
                    [
                        new TermArgumentItem { value = errorRef }
                    ]
                }
            });

        var returnStmt = new ReturnStatement { value = failCall };

        var fineArm = new ArmCaseNode
        {
            pattern = finePattern,
            body = new FunctionBody([valueRef])
        };

        var failArm = new ArmCaseNode
        {
            pattern = failPattern,
            body = new FunctionBody([returnStmt])
        };

        return new TermMatchExpression(operand, [fineArm, failArm], span);
    }

    private static bool try_get_binary_operator_member_name(TermBinaryOperator op, out string memberName)
    {
        memberName = op switch
        {
            TermBinaryOperator.equal => "infix ==",
            TermBinaryOperator.not_equal => "infix !=",
            TermBinaryOperator.less_than => "infix <",
            TermBinaryOperator.greater_than => "infix >",
            TermBinaryOperator.less_than_or_equal => "infix <=",
            TermBinaryOperator.greater_than_or_equal => "infix >=",
            TermBinaryOperator.logical_and => "infix &&",
            TermBinaryOperator.logical_or => "infix ||",
            TermBinaryOperator.power => "infix ^",
            TermBinaryOperator.addition => "infix +",
            TermBinaryOperator.subtraction => "infix -",
            TermBinaryOperator.multiplication => "infix *",
            TermBinaryOperator.division => "infix /",
            TermBinaryOperator.modulus => "infix %",
            TermBinaryOperator.bitwise_and => "bit_and",
            TermBinaryOperator.bitwise_or => "bit_or",
            TermBinaryOperator.bitwise_xor => "bit_xor",
            TermBinaryOperator.left_shift => "bit_shift_left",
            TermBinaryOperator.right_shift => "bit_shift_right",
            _ => string.Empty
        };

        return memberName.Length > 0;
    }

    private static bool try_get_compound_assignment_member_name(TermBinaryOperator op, out string memberName)
    {
        memberName = op switch
        {
            TermBinaryOperator.plus_assign => "infix +",
            TermBinaryOperator.minus_assign => "infix -",
            TermBinaryOperator.multiply_assign => "infix *",
            TermBinaryOperator.divide_assign => "infix /",
            TermBinaryOperator.modulus_assign => "infix %",
            TermBinaryOperator.and_assign => "bit_and",
            TermBinaryOperator.or_assign => "bit_or",
            TermBinaryOperator.xor_assign => "bit_xor",
            TermBinaryOperator.left_shift_assign => "bit_shift_left",
            TermBinaryOperator.right_shift_assign => "bit_shift_right",
            _ => string.Empty
        };

        return memberName.Length > 0;
    }

    private static bool try_get_unary_operator_member_name(TermUnaryOperator op, out string memberName)
    {
        memberName = op switch
        {
            TermUnaryOperator.logical_not => "prefix !",
            TermUnaryOperator.negate => "prefix -",
            TermUnaryOperator.bitwise_not => "bit_not",
            _ => string.Empty
        };

        return memberName.Length > 0;
    }

    private static TermDotExpression create_method_call(TermNode receiver, string memberName,
        IReadOnlyList<TermNode> arguments, TextSpan span = default)
    {
        return new TermDotExpression(
            receiver,
            new QualifiedPathNode
            {
                segments =
                [
                    new IdentifierNode(memberName)
                ],
                span = span
            },
            new CallBody
            {
                term_arguments = new TermArgumentList
                {
                    items =
                    [
                        .. arguments
                            .Select(argument => new TermArgumentItem
                            {
                                value = argument,
                                span = argument.span
                            })
                    ]
                },
                span = span
            })
        {
            span = span
        };
    }

    private static TermNode as_term_node(AstNode node)
    {
        return node switch
        {
            TermNode term => term,
            IdentifierNode identifier => create_name_path_term(identifier.name, identifier.is_raw, identifier.span),
            QualifiedPathNode path => new TermLiteralNamePathNode
            {
                path = path,
                span = path.span
            },
            _ => throw new NotSupportedException(
                $"operator ���ǵ�ǰ��֧�ֿ���Ϊ�����߲����ı���ʽ�ڵ㣬ʵ���յ� `{node.GetType().Name}`��")
        };
    }

    private static TermLiteralNamePathNode create_name_path_term(string name, bool isRaw = false,
        TextSpan span = default)
    {
        return new TermLiteralNamePathNode
        {
            path = new QualifiedPathNode
            {
                segments =
                [
                    new IdentifierNode(name, isRaw)
                ],
                span = span
            },
            span = span
        };
    }

    private static TermLiteralNumberNode create_integer_literal(string value)
    {
        return new TermLiteralNumberNode
        {
            value = value
        };
    }

    private static DeclareMicro normalize_function(DeclareMicro declaration, string? currentNamespace)
    {
        var qualifiedName = build_qualified_name(currentNamespace, null, declaration.name?.name);
        return declaration with
        {
            name = new IdentifierNode(qualifiedName),
            span = declaration.span
        };
    }

    private static string build_qualified_name(string? currentNamespace, string? ownerName, string? memberName)
    {
        return HirTypeRef.qualify_name_path(
            ValkyrieNameSpace.parse(currentNamespace),
            ValkyrieNamePath.parse(ownerName),
            memberName).ToString();
    }

    private static string extract_outer_type_name(string typeName)
    {
        var genericStart = typeName.IndexOf('<');
        return genericStart >= 0 ? typeName[..genericStart] : typeName;
    }

    private static string to_snake_case(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var builder = new StringBuilder(name.Length + 8);
        for (var index = 0; index < name.Length; index++)
        {
            var current = name[index];
            if (index > 0 && char.IsUpper(current))
            {
                var previous = name[index - 1];
                var nextIsLower = index + 1 < name.Length && char.IsLower(name[index + 1]);
                if (char.IsLower(previous) || nextIsLower) builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    private static SourceSpan build_source_span(AstNode node)
    {
        return new SourceSpan(string.Empty, 0, node.span.start, 0, node.span.end);
    }

    /// <summary>
    ///     判断函数声明是否为外部链接声明（�?<c>[clr]</c>�?c>[jvm]</c>�?c>[wasm]</c> �?<c>[import]</c> 属性但无函数体）�?
    /// </summary>
    private static bool is_external_declaration(FunctionDecl functionDecl)
    {
        if (functionDecl.body is not null) return false;

        return functionDecl.annotations.attributes().Any(attr =>
        {
            var name = attr.name;
            return string.Equals(name, "clr", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "jvm", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "wasm", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "import", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "js_builtin", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static long? extract_discriminant(TermNode? value)
    {
        if (value is TermLiteralNumberNode literal)
            return ValkyrieNumberLiteralFacts.try_parse_i64(literal.value, out var result) ? result : null;

        return null;
    }

    #endregion

    #region 效应脱糖

    /// <summary>
    ///     �?YieldStatement 脱糖�?RaiseStatement�?
    ///     <list type="bullet">
    ///         <item><c>yield expr</c> �?<c>raise Yielder::Yield { value: expr }</c></item>
    ///         <item><c>yield break</c> �?<c>raise Yielder::YieldBreak</c></item>
    ///         <item><c>yield return expr</c> �?<c>{ raise Yielder::Yield{value:expr}; raise Yielder::YieldBreak }</c></item>
    ///     </list>
    /// </summary>
    private static AstNode desugar_yield(YieldStatement yieldStmt)
    {
        return yieldStmt.keyword switch
        {
            YieldKeyword.Yield => new RaiseStatement
            {
                value = create_yielder_yield_term(yieldStmt.value),
                span = yieldStmt.span
            },
            YieldKeyword.YieldBreak => new RaiseStatement
            {
                value = create_qualified_path_term("Yielder", "YieldBreak"),
                span = yieldStmt.span
            },
            YieldKeyword.YieldReturn => new FunctionBody(
            [
                new RaiseStatement
                    {
                        value = create_yielder_yield_term(yieldStmt.value),
                        span = yieldStmt.span
                    },
                    new RaiseStatement
                    {
                        value = create_qualified_path_term("Yielder", "YieldBreak"),
                        span = yieldStmt.span
                    }
            ])
            {
                span = yieldStmt.span
            },
            _ => yieldStmt
        };
    }

    /// <summary>
    ///     将作为语句使用的 TermNode 归一化，处理 <c>.await</c> / <c>.awake</c> 脱糖�?
    ///     <list type="bullet">
    ///         <item><c>expr.await</c> �?<c>raise Await { future: expr }</c></item>
    ///         <item><c>expr.awake</c> �?<c>raise Awake { future: expr }</c></item>
    ///     </list>
    /// </summary>
    private static AstNode normalize_term_statement(TermNode term)
    {
        if (try_normalize_assignment_statement(term, out var assignmentStatement)) return assignmentStatement;

        if (term is TermDotExpression { call_body: null } dot)
        {
            var memberName = dot.callee.name;
            if (memberName == "await")
                return new RaiseStatement
                {
                    value = create_effect_construct_term("Await", dot.caller),
                    span = term.span
                };

            if (memberName == "awake")
                return new RaiseStatement
                {
                    value = create_effect_construct_term("Awake", dot.caller),
                    span = term.span
                };
        }

        return normalize_term_node(term);
    }

    private static bool try_normalize_assignment_statement(TermNode term, out AssignmentStatement assignmentStatement)
    {
        switch (term)
        {
            case TermBinaryExpression { @operator: TermBinaryOperator.assign } binary:
            {
                var left = normalize_expression_node(binary.left) ?? binary.left;
                var right = normalize_expression_node(binary.right) ?? binary.right;
                assignmentStatement = create_assignment_statement(left, as_term_node(right), binary.span);
                return true;
            }
            case TermBinaryExpression binary when
                try_get_compound_assignment_member_name(binary.@operator, out var assignmentMemberName):
            {
                var left = normalize_expression_node(binary.left) ?? binary.left;
                var right = normalize_expression_node(binary.right) ?? binary.right;
                var target = as_term_node(left);
                var value = create_method_call(target, assignmentMemberName, [as_term_node(right)], binary.span);
                assignmentStatement = create_assignment_statement(left, value, binary.span);
                return true;
            }
            case TermUnaryExpression { @operator: TermUnaryOperator.increment or TermUnaryOperator.decrement } unary:
            {
                var operand = normalize_term_node(unary.operand);
                var updateMemberName = unary.@operator == TermUnaryOperator.increment ? "infix +" : "infix -";
                var value = create_method_call(operand, updateMemberName, [create_integer_literal("1")], unary.span);
                assignmentStatement = create_assignment_statement(operand, value, unary.span);
                return true;
            }
            default:
                assignmentStatement = new AssignmentStatement();
                return false;
        }
    }

    private static AssignmentStatement create_assignment_statement(AstNode target, TermNode value, TextSpan span)
    {
        return new AssignmentStatement
        {
            @operator = TermBinaryOperator.assign,
            target = target switch
            {
                ValkyrieNode node => node,
                _ => throw new NotSupportedException(
                    $"��ֵĿ�� `{target.GetType().Name}` ������֧�ֵ� AST �ڵ㣬�޷���һ��Ϊ `AssignmentStatement`��")
            },
            value = value,
            span = span
        };
    }

    private static AstNode normalize_assignment_target(AstNode target)
    {
        return normalize_expression_node(target) ?? target;
    }

    private static AstNode normalize_assignment_value(AssignmentStatement assignment)
    {
        var normalizedTarget = normalize_assignment_target(assignment.target);
        var normalizedValue = normalize_expression_node(assignment.value) ?? assignment.value;

        if (assignment.@operator == TermBinaryOperator.assign) return normalizedValue;

        if (!try_get_compound_assignment_member_name(assignment.@operator, out var assignmentMemberName))
            throw new NotSupportedException(
                $"��ֵ���?`{assignment.@operator}` ��ǰ��δ���� `AST -> HIR` ��һ����");

        return create_method_call(as_term_node(normalizedTarget), assignmentMemberName, [as_term_node(normalizedValue)],
            assignment.span);
    }

    /// <summary>
    ///     创建 <c>Yielder::Yield { value: expr }</c> 对象字面量�?
    /// </summary>
    private static TermLiteralObjectNode create_yielder_yield_term(TermNode? value)
    {
        return new TermLiteralObjectNode
        {
            constructor = create_qualified_path_term("Yielder", "Yield"),
            fields = value is null
                ? []
                :
                [
                    new TermObjectField { name = "value", value = value }
                ]
        };
    }

    /// <summary>
    ///     创建效应构造表达式，如 <c>Await { future: expr }</c> �?<c>Awake { future: expr }</c>�?
    /// </summary>
    private static TermLiteralObjectNode create_effect_construct_term(string effectName, TermNode operand)
    {
        return new TermLiteralObjectNode
        {
            constructor = new TermLiteralNamePathNode
            {
                path = new QualifiedPathNode
                {
                    segments = [new IdentifierNode(effectName)]
                }
            },
            fields =
            [
                new TermObjectField { name = "future", value = operand }
            ]
        };
    }

    /// <summary>
    ///     创建带命名空间的路径项，�?<c>Yielder::YieldBreak</c>�?
    /// </summary>
    private static TermLiteralNamePathNode create_qualified_path_term(string namespaceName, string memberName)
    {
        return new TermLiteralNamePathNode
        {
            path = new QualifiedPathNode
            {
                segments =
                [
                    new IdentifierNode(namespaceName),
                    new IdentifierNode(memberName)
                ]
            }
        };
    }

    private static HirTypeRef? extract_payload_type(DeclareUniteVariant variant)
    {
        var fields = variant.body?.fields ?? [];
        if (fields.Count == 0) return null;

        if (fields.Count == 1) return get_type_ref(fields[0].field_type);

        var productTypeName = string.Join(" * ",
            fields.Select(field => get_type_name(field.field_type)));
        return HirTypeRef.named(productTypeName);
    }

    private static IReadOnlyList<HirFieldDef> extract_variant_fields(DeclareUniteVariant variant)
    {
        return
        [
            .. (variant.body?.fields ?? [])
            .Select(field => new HirFieldDef(
                field.name,
                get_type_ref(field.field_type),
                field.name,
                [],
                false,
                false,
                null,
                false,
                false))
        ];
    }

    /// <summary>
    ///     �?unite/union 类型的变体分配判别值�?
    ///     若变体标注了 <c>[tag(N)]</c> 则使用显式值并将自动计数器推进�?N+1�?
    ///     否则使用当前自动计数器值并递增�?
    /// </summary>
    private static HirVariantDef[] assign_unite_discriminants(IReadOnlyList<DeclareUniteVariant> variants)
    {
        var result = new HirVariantDef[variants.Count];
        var nextAuto = 0L;

        for (var index = 0; index < variants.Count; index++)
        {
            var variant = variants[index];
            var explicitTag = extract_tag_discriminant(variant.annotations);
            long discriminant;

            if (explicitTag.HasValue)
            {
                discriminant = explicitTag.Value;
                nextAuto = explicitTag.Value + 1;
            }
            else
            {
                discriminant = nextAuto;
                nextAuto++;
            }

            result[index] = new HirVariantDef(
                variant.name?.name ?? string.Empty,
                extract_payload_type(variant),
                extract_variant_fields(variant),
                discriminant);
        }

        return result;
    }

    /// <summary>
    ///     从注解中提取 <c>[tag(N)]</c> 属性的整数值�?
    /// </summary>
    private static long? extract_tag_discriminant(Annotations annotations)
    {
        foreach (var attr in annotations.attributes())
        {
            if (!string.Equals(attr.name, "tag", StringComparison.Ordinal)) continue;

            if (attr.arguments is null || attr.arguments.items.Count == 0) continue;

            return extract_discriminant(attr.arguments.items[0].value);
        }

        return null;
    }

    private static string get_type_name(TypeNode? typeNode, string fallback = "unknown")
    {
        return typeNode switch
        {
            TypeLiteralNamePathNode literal => get_named_type_name(literal),
            TypeLiteralTupleNode tuple => get_tuple_type_name(tuple, fallback),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.product } product => get_product_type_name(product,
                fallback),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.or } union => join_type_names(union,
                TypeBinaryOperator.or, " | ", fallback),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.and } intersection => join_type_names(intersection,
                TypeBinaryOperator.and, " & ", fallback),
            TypeExpressionUnaryNode { @operator: TypeUnaryOperator.nullable } nullable =>
                $"{get_type_name(nullable.operand, fallback)}?",
            TypeMicroNode => "micro",
            null => fallback,
            _ => typeNode.ToString() ?? fallback
        };
    }

    private static string get_named_type_name(TypeLiteralNamePathNode literal)
    {
        var specialType = resolve_special_type(literal.path.full_name);
        return project_type_name(specialType) ?? literal.path.full_name;
    }

    private static HirTypeRef get_declared_return_type_ref(TypeNode? typeNode)
    {
        return has_auto_return_type(typeNode) ? HirTypeRef.auto() : get_type_ref(typeNode);
    }

    private static bool has_auto_return_type(TypeNode? typeNode)
    {
        return typeNode is null || ValkyrieBuiltinTypeFacts.is_auto_return_type(typeNode);
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
            TypeLiteralTupleNode tuple => build_tuple_type_ref(tuple, fallback),
            null => HirTypeRef.from_name_path(ValkyrieNamePath.parse(fallback)),
            _ => HirTypeRef.from_name_path(ValkyrieNamePath.parse(get_type_name(typeNode, fallback)))
        };
    }

    private static HirTypeRef build_tuple_type_ref(TypeLiteralTupleNode tupleNode, string fallback)
    {
        var elementTypes = tupleNode.elements
            .Select(element => get_type_ref(element.type, fallback))
            .ToArray();
        var elementLabels = tupleNode.elements
            .Select(element => element.label?.name)
            .ToArray();
        return HirTypeRef.tuple(elementTypes, elementLabels);
    }

    private static HirTypeRef build_named_type_ref(TypeLiteralNamePathNode literal)
    {
        var specialType = resolve_special_type(literal.path.full_name);
        var typeRef = HirTypeRef.from_type(specialType, get_named_type_name(literal));

        if (literal.type_arguments is null || literal.type_arguments.items.Count == 0) return typeRef;

        var positionalTypeArguments = literal.type_arguments.items
            .Where(item => item.slot is null)
            .Select(item => get_type_ref(item.argument))
            .ToArray();
        if (positionalTypeArguments.Length > 0) typeRef = typeRef.with_type_arguments(positionalTypeArguments);

        var namedTypeArguments = literal.type_arguments.items
            .Where(item => item.slot is not null)
            .Select(item => new HirTypeArgumentBinding(
                item.slot!.name,
                get_type_ref(item.argument)))
            .ToArray();
        if (namedTypeArguments.Length > 0) typeRef = typeRef.with_named_type_arguments(namedTypeArguments);

        return typeRef;
    }

    /// <summary>
    ///     �?`AST` 关联类型声明转换�?`HIR` 类型绑定�?
    /// </summary>
    private static HirTypeBinding? build_type_binding(DeclareAssociatedType associatedType, SemanticNamePath? contractName)
    {
        if (associatedType.default_type is null || associatedType.name is null) return null;

        return new HirTypeBinding(
            contractName,
            associatedType.name.name,
            get_type_ref(associatedType.default_type));
    }

    private static IReadOnlyList<HirTypeRef> build_trait_alias_targets(TypeNode? typeNode)
    {
        if (typeNode is null) return [];

        var items = typeNode switch
        {
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.product } product =>
                flatten_type_binary(product, TypeBinaryOperator.product),
            TypeExpressionBinaryNode { @operator: TypeBinaryOperator.and } intersection =>
                flatten_type_binary(intersection, TypeBinaryOperator.and),
            _ => [typeNode]
        };

        return
        [
            .. items
                .Select(item => get_type_ref(item))
                .Where(type => !type.is_unknown_type)
        ];
    }

    private static string get_product_type_name(TypeExpressionBinaryNode productNode, string fallback)
    {
        var items = flatten_type_binary(productNode, TypeBinaryOperator.product);
        if (items.Count == 0) return fallback;

        if (items[0] is TypeLiteralNamePathNode constructor) return constructor.path.full_name;

        return get_type_name(items[0], fallback);
    }

    private static string get_tuple_type_name(TypeLiteralTupleNode tupleNode, string fallback)
    {
        if (tupleNode.elements.Count == 0) return "()";

        var items = tupleNode.elements
            .Select(element =>
            {
                var typeName = get_type_name(element.type, fallback);
                return element.label is null ? typeName : $"{element.label.name}: {typeName}";
            });
        var rendered = string.Join(", ", items);
        if (tupleNode.elements.Count == 1) rendered += ",";

        return $"({rendered})";
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

    private static string combine_namespace(string? currentNamespace, string declaredNamespace)
    {
        if (string.IsNullOrWhiteSpace(currentNamespace)) return declaredNamespace;

        if (string.IsNullOrWhiteSpace(declaredNamespace)) return currentNamespace;

        if (declaredNamespace.Equals(currentNamespace, StringComparison.Ordinal)) return declaredNamespace;

        if (declaredNamespace.StartsWith(currentNamespace + ".", StringComparison.Ordinal)) return declaredNamespace;

        return $"{currentNamespace}.{declaredNamespace}";
    }

    #endregion
}
