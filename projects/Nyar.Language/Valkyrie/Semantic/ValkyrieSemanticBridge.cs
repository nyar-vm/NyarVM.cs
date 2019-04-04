using System.Globalization;
using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Diagnostics;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Shader;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;
using TypeChecker_Symbol = Nyar.Language.Valkyrie.TypeChecker.Symbol;
using TypeChecker_SymbolKind = Nyar.Language.Valkyrie.TypeChecker.SymbolKind;

namespace Nyar.Language.Valkyrie.Semantic;

/// <summary>
///     Valkyrie 语义桥接器 —— 将 Valkyrie 类型检查结果转换为 Nyar 语义模型
/// </summary>
public sealed class ValkyrieSemanticBridge : ISemanticAnalysisProvider, IReferenceProvider {
    private readonly Dictionary<string, IType> _declared_types;
    private readonly Dictionary<string, string> _type_alias_map;
    private readonly Dictionary<ValkyrieType, IType> _type_cache;

    public ValkyrieSemanticBridge() {
        _declared_types = new Dictionary<string, IType>(StringComparer.Ordinal);
        _type_alias_map = new Dictionary<string, string>(StringComparer.Ordinal);
        _type_cache = new Dictionary<ValkyrieType, IType>();
    }

    #region 公共 API

    public SemanticModel build_semantic_model(
        TypeCheckResult typeCheckResult,
        CompilationUnit compilationUnit,
        string filePath) {
        return build_semantic_model(typeCheckResult, [compilationUnit], filePath);
    }

    public SemanticModel build_semantic_model(
        TypeCheckResult typeCheckResult,
        IReadOnlyList<CompilationUnit> compilationUnits,
        string compilationId) {
        _type_cache.Clear();
        _declared_types.Clear();
        _type_alias_map.Clear();
        var globalScope = new Scope("global");
        var symbolTable = new SymbolTable(globalScope);
        var model = new SemanticModel(compilationId, symbolTable);
        var typeInference = new ValkyrieTypeInference(collect_functions(compilationUnits), this);

        foreach (var compilationUnit in compilationUnits) predeclare_named_types(compilationUnit.declarations);

        foreach (var compilationUnit in compilationUnits) {
            var filePath = string.IsNullOrWhiteSpace(compilationUnit.file_path)
                ? compilationId
                : compilationUnit.file_path;
            validate_imports(compilationUnit, filePath, model);
            index_declarations(compilationUnit, globalScope, filePath, model, typeInference);
        }

        foreach (var compilationUnit in compilationUnits) {
            var filePath = string.IsNullOrWhiteSpace(compilationUnit.file_path)
                ? compilationId
                : compilationUnit.file_path;
            validate_entry_functions(compilationUnit, filePath, model, typeInference);
            validate_call_argument_types(compilationUnit, filePath, model, typeInference);
            validate_explicit_return_annotations(compilationUnit, filePath, model);
        }

        foreach (var diag in typeCheckResult.diagnostics) model.add_diagnostic(convert_diagnostic(diag, compilationId));

        return model;
    }

    public SemanticModel analyze(string filePath, object syntaxRoot) {
        if (syntaxRoot is not CompilationUnit compilationUnit) throw new ArgumentException("Valkyrie semantic analysis requires ProgramRoot syntax.", nameof(syntaxRoot));

        return build_semantic_model(new TypeCheckResult([]), [compilationUnit], filePath);
    }

    public IReadOnlyList<SymbolStub> build_stubs(string filePath, object syntaxRoot) {
        var model = analyze(filePath, syntaxRoot);
        var stubs = new List<SymbolStub>();

        foreach (var symbol in model.get_all_declared_symbols()) {
            if (symbol is not Symbol concreteSymbol) continue;

            stubs.Add(new SymbolStub(
                symbol.name,
                symbol.kind,
                symbol.accessibility,
                concreteSymbol.definition_span,
                concreteSymbol.definition_source_span,
                symbol.type?.name,
                filePath));
        }

        return stubs;
    }

    public IReadOnlyList<ReferenceEntry> collect_references(string filePath, object syntaxRoot, SemanticModel model) {
        return [];
    }

    public IType convert_type(ValkyrieType valkyrieType) {
        if (_type_cache.TryGetValue(valkyrieType, out var cached)) return cached;

        var result = convert_type_core(valkyrieType);
        _type_cache[valkyrieType] = result;
        return result;
    }

    public Symbol convert_symbol(TypeChecker_Symbol valkyrieSymbol, Scope containingScope,
        string? filePath = null) {
        var kind = convert_symbol_kind(valkyrieSymbol.kind);
        var type = convert_type(valkyrieSymbol.type);
        var accessibility = valkyrieSymbol.is_exported
            ? SymbolAccessibility.@public
            : SymbolAccessibility.@private;

        return new Symbol(
            valkyrieSymbol.name,
            kind,
            accessibility,
            type,
            containingScope,
            isReadOnly: !valkyrieSymbol.is_mutable,
            filePath: filePath);
    }

    public SemanticDiagnostic
        convert_diagnostic(TypeDiagnostic diagnostic, string? filePath = null) {
        var level = diagnostic.severity;

        var sourceSpan = SourceSpan.single_line(
            diagnostic.line > 0 ? diagnostic.line : 1,
            diagnostic.column > 0 ? diagnostic.column : 1,
            diagnostic.message.Length);

        if (filePath is not null) sourceSpan = sourceSpan with { file_path = filePath };

        return new SemanticDiagnostic(level, diagnostic.message, sourceSpan, diagnostic.code, filePath);
    }

    public SymbolKind convert_symbol_kind(TypeChecker_SymbolKind valkyrieKind) {
        return valkyrieKind switch {
            TypeChecker_SymbolKind.variable => SymbolKind.variable,
            TypeChecker_SymbolKind.parameter => SymbolKind.parameter,
            TypeChecker_SymbolKind.function => SymbolKind.function,
            TypeChecker_SymbolKind.component => SymbolKind.@class,
            TypeChecker_SymbolKind.system => SymbolKind.@class,
            TypeChecker_SymbolKind.widget => SymbolKind.@class,
            TypeChecker_SymbolKind.plugin => SymbolKind.module,
            TypeChecker_SymbolKind.@struct => SymbolKind.@class,
            TypeChecker_SymbolKind.@enum => SymbolKind.@enum,
            TypeChecker_SymbolKind.union => SymbolKind.@class,
            TypeChecker_SymbolKind.import => SymbolKind.import,
            TypeChecker_SymbolKind.field => SymbolKind.field,
            _ => SymbolKind.variable
        };
    }

    #endregion

    #region 类型转换

    private IType convert_type_core(ValkyrieType valkyrieType) {
        if (valkyrieType.is_error) return ErrorType.instance;

        return valkyrieType.kind switch {
            TypeKind.primitive => convert_primitive_type(valkyrieType),
            TypeKind.function => convert_function_type(valkyrieType),
            TypeKind.array => convert_array_type(valkyrieType),
            TypeKind.fixed_array => convert_fixed_array_type(valkyrieType),
            TypeKind.tuple => convert_tuple_type(valkyrieType),
            TypeKind.map => convert_map_type(valkyrieType),
            TypeKind.nullable => convert_nullable_type(valkyrieType),
            TypeKind.component => new NamedType(valkyrieType.name, "component"),
            TypeKind.system => new NamedType(valkyrieType.name, "system"),
            TypeKind.widget => new NamedType(valkyrieType.name, "widget"),
            TypeKind.plugin => new NamedType(valkyrieType.name, "plugin"),
            TypeKind.@struct => new NamedType(valkyrieType.name, "struct"),
            TypeKind.@enum => new NamedType(valkyrieType.name, "enum"),
            TypeKind.union => new NamedType(valkyrieType.name, "union"),
            TypeKind.shader => new NamedType(valkyrieType.name, "shader"),
            TypeKind.generic => convert_generic_type(valkyrieType),
            TypeKind.unknown when valkyrieType.is_auto => AutoType.instance,
            TypeKind.unknown => new NamedType(valkyrieType.name, "unknown"),
            TypeKind.error => ErrorType.instance,
            _ => UnknownType.instance
        };
    }

    private IType convert_primitive_type(ValkyrieType valkyrieType) {
        return new PrimitiveType(valkyrieType.name);
    }

    private IType convert_function_type(ValkyrieType valkyrieType) {
        var paramTypes = new List<IType>();

        if (valkyrieType.parameters is not null)
            foreach (var param in valkyrieType.parameters)
                paramTypes.Add(convert_type(param.type));

        var returnType = valkyrieType.return_type is not null
            ? convert_type(valkyrieType.return_type)
            : UnknownType.instance;

        return new FunctionType(paramTypes, returnType);
    }

    private IType convert_array_type(ValkyrieType valkyrieType) {
        if (valkyrieType.generic_args.Count > 0) return new GenericType(valkyrieType.name, convert_generic_args(valkyrieType.generic_args));

        return new GenericType(valkyrieType.name, []);
    }

    private IType convert_fixed_array_type(ValkyrieType valkyrieType) {
        if (valkyrieType.generic_args.Count > 0) return new NamedType(valkyrieType.name, "struct", typeArguments: convert_generic_args(valkyrieType.generic_args));

        return new NamedType(valkyrieType.name, "struct");
    }

    private IType convert_tuple_type(ValkyrieType valkyrieType)
    {
        var elements = convert_generic_args(valkyrieType.generic_args);
        return create_tuple_type(elements);
    }

    private IType convert_map_type(ValkyrieType valkyrieType) {
        return new GenericType(valkyrieType.name, convert_generic_args(valkyrieType.generic_args));
    }

    private IType convert_nullable_type(ValkyrieType valkyrieType) {
        if (valkyrieType.generic_args.Count > 0) return new NullableType(convert_type(valkyrieType.generic_args[0]));

        return UnknownType.instance;
    }

    private IType convert_generic_type(ValkyrieType valkyrieType) {
        return new GenericType(valkyrieType.name, convert_generic_args(valkyrieType.generic_args));
    }

    private List<IType> convert_generic_args(IReadOnlyList<ValkyrieType> genericArgs) {
        var result = new List<IType>(genericArgs.Count);
        foreach (var arg in genericArgs) result.Add(convert_type(arg));

        return result;
    }

    #endregion

    #region SourceSpan 转换

    private static SourceSpan to_source_span(SourceSpan? span, string? filePath = null) {
        if (span is null) return default;

        var s = span.Value;
        return new SourceSpan(filePath ?? s.file_path, s.start_line, s.start_column, s.end_line, s.end_column);
    }

    private static SourceSpan to_source_span(TextSpan span, string? filePath = null) {
        return new SourceSpan(filePath ?? string.Empty, 0, span.start, 0, span.end);
    }

    #endregion

    #region 声明索引

    private void validate_imports(CompilationUnit compilationUnit, string filePath, SemanticModel model) {
        validate_imports(
            compilationUnit.declarations,
            null,
            filePath,
            model,
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal),
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal));

        validate_namespace_entries(compilationUnit.declarations, null, filePath, model,
            new Dictionary<string, List<string>>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal));
    }

    /// <summary>
    ///     校验 namespace 主入口相关规则：
    ///     <para>
    ///         - 单文件内重复声明 <c>namespace!</c> → <c>VALKYRIE_NAMESPACE_PRIMARY_DUPLICATE_IN_FILE</c>。
    ///         - 同一 namespace 有多个 <c>namespace!</c> 主入口 → <c>VALKYRIE_NAMESPACE_PRIMARY_CONFLICT</c>。
    ///         - <c>namespace!</c> 与普通 <c>namespace</c> 在同名 scope 形成冲突语义 →
    ///           <c>VALKYRIE_NAMESPACE_PRIMARY_SCOPE_CONFLICT</c>。
    ///     </para>
    /// </summary>
    private void validate_namespace_entries(
        IReadOnlyList<AstNode> declarations,
        string? currentNamespace,
        string filePath,
        SemanticModel model,
        IDictionary<string, List<string>> primaryEntriesByNamespace,
        ISet<string> primarySeenInFile) {
        var currentResolved = currentNamespace;

        foreach (var decl in declarations) {
            if (decl is not NamespaceDecl namespaceDecl) {
                continue;
            }

            var resolvedNamespace = combine_namespace(currentResolved, namespaceDecl.name.name);

            if (namespaceDecl.is_primary) {
                if (!primarySeenInFile.Add(resolvedNamespace)) {
                    report_namespace_primary_duplicate_in_file(namespaceDecl, resolvedNamespace, filePath, model);
                }

                if (!primaryEntriesByNamespace.TryGetValue(resolvedNamespace, out var entries)) {
                    entries = [];
                    primaryEntriesByNamespace[resolvedNamespace] = entries;
                }

                entries.Add(filePath);
                if (entries.Count > 1) {
                    report_namespace_primary_conflict(namespaceDecl, resolvedNamespace, filePath, model);
                }
            }

            if (namespaceDecl.declarations.Count > 0) {
                validate_namespace_entries(
                    namespaceDecl.declarations,
                    resolvedNamespace,
                    filePath,
                    model,
                    primaryEntriesByNamespace,
                    primarySeenInFile);
            }
            else {
                currentResolved = resolvedNamespace;
            }
        }
    }

    private static void report_namespace_primary_duplicate_in_file(
        NamespaceDecl namespaceDecl,
        string namespaceName,
        string filePath,
        SemanticModel model) {
        var message = $"文件 `{filePath}` 内重复声明 `namespace! {namespaceName}`。";
        model.add_diagnostic(ValkyrieDiagnostics.build_semantic_diagnostic(
            ValkyrieRuleNames.NAMESPACE_PRIMARY_DUPLICATE_IN_FILE,
            message,
            to_source_span(namespaceDecl.span, filePath),
            filePath));
    }

    private static void report_namespace_primary_conflict(
        NamespaceDecl namespaceDecl,
        string namespaceName,
        string filePath,
        SemanticModel model) {
        var message = $"同一 namespace `{namespaceName}` 有多个 `namespace!` 主入口。";
        model.add_diagnostic(ValkyrieDiagnostics.build_semantic_diagnostic(
            ValkyrieRuleNames.NAMESPACE_PRIMARY_CONFLICT,
            message,
            to_source_span(namespaceDecl.span, filePath),
            filePath));
    }

    private void validate_imports(
        IReadOnlyList<AstNode> declarations,
        string? initialNamespace,
        string filePath,
        SemanticModel model,
        IDictionary<string, HashSet<string>> seenImportsByNamespace,
        IDictionary<string, Dictionary<string, string>> aliasesByNamespace) {
        var currentNamespace = initialNamespace;

        foreach (var decl in declarations) {
            if (decl is NamespaceDecl namespaceDecl) {
                var resolvedNamespace = combine_namespace(currentNamespace, namespaceDecl.name.name);
                if (namespaceDecl.declarations.Count == 0) {
                    currentNamespace = resolvedNamespace;
                }
                else {
                    validate_imports(
                        namespaceDecl.declarations,
                        resolvedNamespace,
                        filePath,
                        model,
                        seenImportsByNamespace,
                        aliasesByNamespace);
                }

                continue;
            }

            if (decl is ImportDecl importDecl) {
                validate_import_decl(
                    importDecl,
                    currentNamespace,
                    filePath,
                    model,
                    seenImportsByNamespace,
                    aliasesByNamespace);
            }
        }
    }

    private void index_declarations(CompilationUnit compilationUnit, Scope globalScope, string filePath,
        SemanticModel model, ValkyrieTypeInference typeInference) {
        index_declarations(compilationUnit.declarations, globalScope, globalScope, null, filePath, model, typeInference);
    }

    private void index_declarations(IReadOnlyList<AstNode> declarations, Scope globalScope, Scope initialScope,
        string? initialNamespace, string filePath, SemanticModel model, ValkyrieTypeInference typeInference) {
        var currentScope = initialScope;
        var currentNamespace = initialNamespace;

        foreach (var decl in declarations) {
            if (decl is NamespaceDecl namespaceDecl) {
                var resolvedNamespace = combine_namespace(currentNamespace, namespaceDecl.name.name);
                var namespaceScope = get_or_create_namespace_scope(globalScope, resolvedNamespace);

                if (namespaceDecl.declarations.Count == 0) {
                    currentNamespace = resolvedNamespace;
                    currentScope = namespaceScope;
                    continue;
                }

                index_declarations(namespaceDecl.declarations, globalScope, namespaceScope, resolvedNamespace, filePath, model, typeInference);
                continue;
            }

            index_declaration(decl, currentScope, currentNamespace, filePath, model, typeInference);
        }
    }

    private void index_declaration(AstNode node, Scope scope, string? currentNamespace, string filePath, SemanticModel model, ValkyrieTypeInference typeInference) {
        validate_data_annotation_usage(node, currentNamespace, filePath, model);

        switch (node) {
            case ComponentDeclaration comp:
                index_component_decl(comp, scope, filePath, model);
                break;
            case SystemDeclaration sys:
                index_system_decl(sys, scope, filePath, model);
                break;
            case FunctionDecl func:
                index_function_decl(func, scope, filePath, model, typeInference);
                break;
            case LetDeclaration varDecl:
                index_variable_decl(varDecl, scope, filePath, model, typeInference);
                break;
            case StructureDecl structDecl:
                index_struct_decl(structDecl, scope, currentNamespace, filePath, model);
                break;
            case ClassDecl classDecl:
                index_class_decl(classDecl, scope, currentNamespace, filePath, model);
                break;
            case TraitDecl traitDecl:
                index_trait_decl(traitDecl, scope, filePath, model);
                break;
            case UniteDecl uniteDecl:
                index_unite_decl(uniteDecl, scope, filePath, model);
                break;
            case ShaderDecl shaderDecl:
                index_shader_decl(shaderDecl, scope, filePath, model);
                break;
            case ImportDecl importDecl:
                index_import_decl(importDecl, scope, filePath, model);
                break;
            case TypeAliasDecl typeAliasDecl:
                index_type_alias(typeAliasDecl.name?.name, typeAliasDecl.target_type, scope, filePath, model);
                break;
            case DeclareTraitAlias traitAlias:
                index_type_alias(traitAlias.name?.name, traitAlias.target_type, scope, filePath, model);
                break;
        }
    }

    private void index_component_decl(ComponentDeclaration comp, Scope scope, string filePath, SemanticModel model) {
        var compName = comp.name?.name ?? "";
        var members = collect_object_members(comp.body, scope, filePath);
        var compType = new NamedType(compName, "component", members: members);
        register_declared_type(compType);
        var symbol = new Symbol(compName, SymbolKind.@class, SymbolAccessibility.@public,
            compType, scope,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(comp.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(comp.GetHashCode(), symbol);
        model.bind_type(comp.GetHashCode(), compType);
    }

    private void index_system_decl(SystemDeclaration sys, Scope scope, string filePath, SemanticModel model) {
        var sysName = sys.name?.name ?? "";
        var sysType = new NamedType(sysName, "system");
        register_declared_type(sysType);
        var symbol = new Symbol(sysName, SymbolKind.@class, SymbolAccessibility.@public,
            sysType, scope,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(sys.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(sys.GetHashCode(), symbol);
        model.bind_type(sys.GetHashCode(), sysType);
    }

    private void index_function_decl(FunctionDecl func, Scope scope, string filePath, SemanticModel model, ValkyrieTypeInference typeInference) {
        var funcName = func.name?.name ?? "";
        var funcType = typeInference.build_function_type(func);
        var symbol = new Symbol(funcName, SymbolKind.function, SymbolAccessibility.@public,
            funcType, scope,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(func.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(func.GetHashCode(), symbol);
        model.bind_type(func.GetHashCode(), funcType);
    }

    private void index_variable_decl(LetDeclaration varDecl, Scope scope, string filePath, SemanticModel model, ValkyrieTypeInference typeInference) {
        var varType = varDecl.var_type is not null
            ? convert_type_annotation(varDecl.var_type)
            : varDecl.initializer is not null
                ? typeInference.infer_expression_type(varDecl.initializer)
                : UnknownType.instance;
        var symbol = new Symbol(varDecl.name?.name ?? "", SymbolKind.variable, SymbolAccessibility.@private,
            varType, scope,
            isReadOnly: !varDecl.is_mutable,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(varDecl.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(varDecl.GetHashCode(), symbol);
        model.bind_type(varDecl.GetHashCode(), varType);
    }

    private void index_struct_decl(StructureDecl structDecl, Scope scope, string filePath, SemanticModel model) {
        index_struct_decl(structDecl, scope, null, filePath, model);
    }

    private void index_struct_decl(StructureDecl structDecl, Scope scope, string? currentNamespace, string filePath, SemanticModel model) {
        var structName = structDecl.name?.name ?? "";
        var qualifiedName = qualify_name(currentNamespace, structName);
        var members = collect_object_members(structDecl.body, scope, filePath);
        var structType = new NamedType(structName, "struct", members: members);
        register_declared_type(structType);
        var symbol = new Symbol(structName, SymbolKind.@class, SymbolAccessibility.@public,
            structType, scope,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(structDecl.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(structDecl.GetHashCode(), symbol);
        model.bind_type(structDecl.GetHashCode(), structType);
        bind_data_type_if_present(
            structDecl.annotations,
            structDecl.body,
            structName,
            qualifiedName,
            "structure",
            structDecl.GetHashCode(),
            filePath,
            model);
    }

    private void index_class_decl(ClassDecl classDecl, Scope scope, string filePath, SemanticModel model) {
        index_class_decl(classDecl, scope, null, filePath, model);
    }

    private void index_class_decl(ClassDecl classDecl, Scope scope, string? currentNamespace, string filePath, SemanticModel model) {
        var className = classDecl.name?.name ?? "";
        var qualifiedName = qualify_name(currentNamespace, className);
        var members = collect_object_members(classDecl.body, scope, filePath);
        var classType = new NamedType(className, "class", members: members);
        register_declared_type(classType);
        var symbol = new Symbol(className, SymbolKind.@class, SymbolAccessibility.@public,
            classType, scope,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(classDecl.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(classDecl.GetHashCode(), symbol);
        model.bind_type(classDecl.GetHashCode(), classType);
        bind_data_type_if_present(
            classDecl.annotations,
            classDecl.body,
            className,
            qualifiedName,
            "class",
            classDecl.GetHashCode(),
            filePath,
            model);
    }

    private void index_trait_decl(TraitDecl traitDecl, Scope scope, string filePath, SemanticModel model) {
        var traitName = traitDecl.name?.name ?? "";
        var members = collect_object_members(traitDecl.body, scope, filePath, false);
        var traitType = new NamedType(traitName, "trait", members: members);
        register_declared_type(traitType);
        var symbol = new Symbol(traitName, SymbolKind.@interface, SymbolAccessibility.@public,
            traitType, scope,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(traitDecl.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(traitDecl.GetHashCode(), symbol);
        model.bind_type(traitDecl.GetHashCode(), traitType);
    }

    private void index_unite_decl(UniteDecl uniteDecl, Scope scope, string filePath, SemanticModel model) {
        var uniteName = uniteDecl.name?.name ?? "";
        var members = collect_method_members(uniteDecl.methods, scope, filePath);
        var uniteType = new NamedType(uniteName, "unite", members: members);
        register_declared_type(uniteType);
        var symbol = new Symbol(uniteName, SymbolKind.@class, SymbolAccessibility.@public,
            uniteType, scope,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(uniteDecl.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(uniteDecl.GetHashCode(), symbol);
        model.bind_type(uniteDecl.GetHashCode(), uniteType);
    }

    private IReadOnlyList<ISymbol> collect_object_members(ObjectBody? body, Scope scope, string filePath,
        bool includeFieldAccessors = true) {
        var members = new List<ISymbol>();
        if (body is null) return members;

        foreach (var field in body.fields) {
            var fieldType = convert_type_annotation(field.field_type);
            var accessibility = get_accessibility(field.annotations);
            members.Add(new Symbol(field.name, SymbolKind.property, accessibility,
                fieldType, scope,
                definitionSpan: default,
                definitionSourceSpan: to_source_span(field.span, filePath),
                filePath: filePath));

            if (!includeFieldAccessors || accessibility != SymbolAccessibility.@public) continue;

            members.Add(new Symbol($"get_{field.name}", SymbolKind.method, SymbolAccessibility.@public,
                new FunctionType([], fieldType), scope,
                isReadOnly: true,
                definitionSpan: default,
                definitionSourceSpan: to_source_span(field.span, filePath),
                filePath: filePath));
            members.Add(new Symbol($"set_{field.name}", SymbolKind.method, SymbolAccessibility.@public,
                new FunctionType([fieldType], ValkyrieBuiltinTypeFacts.unit_type), scope,
                definitionSpan: default,
                definitionSourceSpan: to_source_span(field.span, filePath),
                filePath: filePath));
        }

        members.AddRange(collect_method_members(body.methods, scope, filePath));
        return members;
    }

    private IReadOnlyList<ISymbol> collect_method_members(IReadOnlyList<DeclareObjectMethod> methods, Scope scope,
        string filePath) {
        var members = new List<ISymbol>(methods.Count);
        foreach (var method in methods)
            members.Add(new Symbol(method.name?.name ?? string.Empty, SymbolKind.method, get_accessibility(method.annotations),
                build_method_type(method), scope,
                definitionSpan: default,
                definitionSourceSpan: to_source_span(method.span, filePath),
                filePath: filePath));

        return members;
    }

    private IType build_method_type(DeclareObjectMethod method) {
        var parameters = method.parameters
            .SelectMany(parameter => parameter.items)
            .ToArray();
        var parameterOffset = parameters.Length > 0 &&
                              string.Equals(parameters[0].name?.name, ValkyrieBuiltinTypeFacts.self_value_name, StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;
        var parameterTypes = parameters
            .Skip(parameterOffset)
            .Select(parameter => convert_type_annotation(parameter.bound_type ?? create_any_type()))
            .ToArray();
        var returnType = method.return_type is not null && !is_auto_return_type(method.return_type)
            ? convert_type_annotation(method.return_type)
            : UnknownType.instance;
        return new FunctionType(parameterTypes, returnType);
    }

    private static SymbolAccessibility get_accessibility(Annotations annotations) {
        var modifiers = annotations.modifier_texts();
        if (modifiers.Any(modifier => string.Equals(modifier, "private", StringComparison.OrdinalIgnoreCase))) return SymbolAccessibility.@private;

        if (modifiers.Any(modifier => string.Equals(modifier, "protected", StringComparison.OrdinalIgnoreCase))) return SymbolAccessibility.@protected;

        if (modifiers.Any(modifier => string.Equals(modifier, "internal", StringComparison.OrdinalIgnoreCase))) return SymbolAccessibility.@internal;

        return SymbolAccessibility.@public;
    }

    private void validate_data_annotation_usage(AstNode node, string? currentNamespace, string filePath, SemanticModel model)
    {
        var annotations = get_annotations(node);
        if (!has_attribute(annotations, "data"))
        {
            return;
        }

        switch (node)
        {
            case DeclareStructure:
                return;
            case DeclareClass klass when klass.body?.methods.Count > 0:
                model.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.error,
                    $"`[data] class {qualify_name(currentNamespace, klass.name?.name ?? string.Empty)}` 不能包含实例方法。",
                    to_source_span(klass.span, filePath),
                    "VALK_DATA_CLASS_WITH_METHODS",
                    filePath));
                return;
            case DeclareClass:
                return;
            default:
                model.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.error,
                    $"`[data]` 当前只允许标注在 `structure` 或纯数据 `class` 上，不能用于 `{node.GetType().Name}`。",
                    to_source_span(node.span, filePath),
                    "VALK_DATA_UNSUPPORTED_TARGET",
                    filePath));
                return;
        }
    }

    private void bind_data_type_if_present(
        Annotations annotations,
        ObjectBody? body,
        string typeName,
        string qualifiedName,
        string declarationKind,
        int nodeId,
        string filePath,
        SemanticModel model)
    {
        if (!has_attribute(annotations, "data"))
        {
            return;
        }

        validate_data_fields(qualifiedName, body, filePath, model);
        var fields = body?.fields.Select(collect_data_field_info).ToArray() ?? [];
        model.bind_data_type(
            nodeId,
            new DataAttributeInfo(typeName, qualifiedName, declarationKind, fields),
            ValkyrieNamePath.parse(qualifiedName));
    }

    private void validate_data_fields(
        string qualifiedName,
        ObjectBody? body,
        string filePath,
        SemanticModel model)
    {
        if (body is null)
        {
            return;
        }

        var occupiedBindings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in body.fields)
        {
            validate_single_data_field(qualifiedName, field, filePath, model, occupiedBindings);
        }
    }

    private void validate_single_data_field(
        string qualifiedName,
        DeclareObjectField field,
        string filePath,
        SemanticModel model,
        IDictionary<string, string> occupiedBindings)
    {
        var fieldAttribute = get_attribute(field.annotations, "field");
        var ignore = has_attribute(field.annotations, "ignore");
        var flatten = has_attribute(field.annotations, "flatten");
        var bindingName = get_first_attribute_argument(fieldAttribute) ?? field.name;
        var aliases = get_attributes(field.annotations, "alias")
            .Select(get_first_attribute_argument)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Cast<string>()
            .ToArray();

        if (ignore && flatten)
        {
            model.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.error,
                $"`[data]` 字段 `{qualifiedName}.{field.name}` 不能同时声明 `[ignore]` 和 `[flatten]`。",
                to_source_span(field.span, filePath),
                "VALK_DATA_IGNORE_FLATTEN_CONFLICT",
                filePath));
        }

        foreach (var duplicateAlias in aliases
                     .GroupBy(alias => alias, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            model.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.error,
                $"`[data]` 字段 `{qualifiedName}.{field.name}` 重复声明别名 `{duplicateAlias}`。",
                to_source_span(field.span, filePath),
                "VALK_DATA_DUPLICATE_ALIAS",
                filePath));
        }

        if (ignore)
        {
            return;
        }

        register_data_binding_name(qualifiedName, field, bindingName, filePath, model, occupiedBindings);
        foreach (var alias in aliases.Distinct(StringComparer.Ordinal))
        {
            register_data_binding_name(qualifiedName, field, alias, filePath, model, occupiedBindings);
        }
    }

    private static void register_data_binding_name(
        string qualifiedName,
        DeclareObjectField field,
        string bindingName,
        string filePath,
        SemanticModel model,
        IDictionary<string, string> occupiedBindings)
    {
        if (string.IsNullOrWhiteSpace(bindingName))
        {
            return;
        }

        if (occupiedBindings.TryGetValue(bindingName, out var existingField))
        {
            model.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.error,
                $"`[data]` 类型 `{qualifiedName}` 的字段绑定名 `{bindingName}` 冲突，涉及 `{existingField}` 与 `{field.name}`。",
                to_source_span(field.span, filePath),
                "VALK_DATA_FIELD_NAME_CONFLICT",
                filePath));
            return;
        }

        occupiedBindings[bindingName] = field.name;
    }

    private DataFieldInfo collect_data_field_info(DeclareObjectField field)
    {
        var fieldAttribute = get_attribute(field.annotations, "field");
        var bindingName = get_first_attribute_argument(fieldAttribute) ?? field.name;
        var aliases = get_attributes(field.annotations, "alias")
            .Select(get_first_attribute_argument)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new DataFieldInfo(
            field.name,
            convert_type_annotation(field.field_type),
            bindingName,
            aliases,
            has_attribute(field.annotations, "ignore"),
            has_attribute(field.annotations, "flatten"),
            try_get_attribute_int_argument(fieldAttribute, 1, out var order) ? order : null,
            field.default_value is not null,
            field.default_value?.ToString(),
            try_get_attribute_bool_argument(fieldAttribute, 2, out var skipWhenNull) && skipWhenNull,
            try_get_attribute_bool_argument(fieldAttribute, 3, out var skipWhenDefault) && skipWhenDefault);
    }

    private static Annotations get_annotations(AstNode node)
    {
        return node switch
        {
            DeclareAssociatedType associatedType => associatedType.annotations,
            DeclareClass classDecl => classDecl.annotations,
            DeclareEnums enumsDecl => enumsDecl.annotations,
            DeclareFlags flagsDecl => flagsDecl.annotations,
            DeclareImply implyDecl => implyDecl.annotations,
            DeclareLet letDecl => letDecl.annotations,
            DeclareMacro macroDecl => macroDecl.annotations,
            DeclareMezzo mezzoDecl => mezzoDecl.annotations,
            DeclareMicro microDecl => microDecl.annotations,
            DeclareObjectMethod methodDecl => methodDecl.annotations,
            DeclareStructure structureDecl => structureDecl.annotations,
            DeclareTrait traitDecl => traitDecl.annotations,
            DeclareTraitAlias traitAliasDecl => traitAliasDecl.annotations,
            DeclareUnite uniteDecl => uniteDecl.annotations,
            DeclareUniteVariant uniteVariantDecl => uniteVariantDecl.annotations,
            PluginDecl pluginDecl => pluginDecl.annotations,
            TypeAliasDecl typeAliasDecl => typeAliasDecl.annotations,
            UnionDecl unionDecl => unionDecl.annotations,
            _ => new Annotations()
        };
    }

    private static bool has_attribute(Annotations annotations, string attributeName)
    {
        return get_attribute(annotations, attributeName) is not null;
    }

    private static AttributeItem? get_attribute(Annotations annotations, string attributeName)
    {
        return annotations.attributes()
            .FirstOrDefault(attribute => string.Equals(attribute.name, attributeName, StringComparison.Ordinal));
    }

    private static IReadOnlyList<AttributeItem> get_attributes(Annotations annotations, string attributeName)
    {
        return
        [
            .. annotations.attributes()
                .Where(attribute => string.Equals(attribute.name, attributeName, StringComparison.Ordinal))
        ];
    }

    private static string? get_first_attribute_argument(AttributeItem? attribute)
    {
        if (attribute?.arguments?.items.Count is not > 0)
        {
            return null;
        }

        return extract_attribute_argument_text(attribute.arguments.items[0]);
    }

    private static bool try_get_attribute_int_argument(AttributeItem? attribute, int index, out int value)
    {
        value = default;
        if (!try_get_attribute_argument(attribute, index, out var text))
        {
            return false;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool try_get_attribute_bool_argument(AttributeItem? attribute, int index, out bool value)
    {
        value = default;
        if (!try_get_attribute_argument(attribute, index, out var text))
        {
            return false;
        }

        return bool.TryParse(text, out value);
    }

    private static bool try_get_attribute_argument(AttributeItem? attribute, int index, out string text)
    {
        text = string.Empty;
        if (attribute?.arguments?.items.Count is null || attribute.arguments.items.Count <= index)
        {
            return false;
        }

        text = extract_attribute_argument_text(attribute.arguments.items[index]);
        return !string.IsNullOrWhiteSpace(text);
    }

    private static string extract_attribute_argument_text(TermArgumentItem argument)
    {
        return argument.value switch
        {
            TermLiteralTextNode textNode => textNode.value,
            TermLiteralNumberNode numberNode => numberNode.value,
            TermLiteralBooleanNode booleanNode => booleanNode.value ? "true" : "false",
            _ => argument.value.ToString() ?? string.Empty
        };
    }

    private static string qualify_name(string? currentNamespace, string localName)
    {
        if (string.IsNullOrWhiteSpace(currentNamespace) || string.IsNullOrWhiteSpace(localName))
        {
            return localName;
        }

        return $"{currentNamespace}.{localName}";
    }

    private void register_declared_type(IType type)
    {
        if (string.IsNullOrWhiteSpace(type.name)) return;

        _declared_types[type.name] = type;
    }

    /// <summary>
    ///     注册类型别名（如 <c>type utf8 = Utf8Text</c>），
    ///     使得后续类型引用自动解析到目标类型。
    /// </summary>
    private void index_type_alias(string? aliasName, TypeNode targetType, Scope scope, string filePath, SemanticModel model)
    {
        if (string.IsNullOrWhiteSpace(aliasName)) return;

        var resolvedTargetType = convert_type_annotation(targetType);
        var canonicalName = resolvedTargetType.name;

        if (string.IsNullOrWhiteSpace(canonicalName) || string.Equals(aliasName, canonicalName, StringComparison.Ordinal)) return;

        // 注册别名映射：utf8 → Utf8Text
        _type_alias_map[aliasName] = canonicalName;

        // 同时注册到 declared_types，使 resolve_declared_named_type 能查找到
        _declared_types[aliasName] = resolvedTargetType;
    }

    private void predeclare_named_types(IReadOnlyList<AstNode> declarations)
    {
        foreach (var declaration in declarations)
            switch (declaration)
            {
                case DeclareNamespace namespaceDecl:
                    predeclare_named_types(namespaceDecl.declarations);
                    break;
                case DeclareClass classDecl:
                    register_declared_type(new NamedType(classDecl.name?.name ?? string.Empty, "class"));
                    break;
                case DeclareTrait traitDecl:
                    register_declared_type(new NamedType(traitDecl.name?.name ?? string.Empty, "trait"));
                    break;
                case DeclareStructure structDecl:
                    register_declared_type(new NamedType(structDecl.name?.name ?? string.Empty, "struct"));
                    break;
                case DeclareUnite uniteDecl:
                    register_declared_type(new NamedType(uniteDecl.name?.name ?? string.Empty, "unite"));
                    break;
                case ComponentDeclaration componentDecl:
                    register_declared_type(new NamedType(componentDecl.name?.name ?? string.Empty, "component"));
                    break;
                case SystemDeclaration systemDecl:
                    register_declared_type(new NamedType(systemDecl.name?.name ?? string.Empty, "system"));
                    break;
                case ShaderDecl shaderDecl:
                    register_declared_type(new NamedType(shaderDecl.name?.name ?? string.Empty, "shader"));
                    break;
            }
    }

    private void index_shader_decl(ShaderDecl shaderDecl, Scope scope, string filePath, SemanticModel model) {
        var shaderName = shaderDecl.name?.name ?? "";
        var shaderType = new NamedType(shaderName, "shader");
        var symbol = new Symbol(shaderName, SymbolKind.@class, SymbolAccessibility.@public,
            shaderType, scope,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(shaderDecl.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(shaderDecl.GetHashCode(), symbol);
        model.bind_type(shaderDecl.GetHashCode(), shaderType);
    }

    private void index_import_decl(DeclareUsing importDecl, Scope scope, string filePath, SemanticModel model) {
        var importName = string.IsNullOrWhiteSpace(importDecl.alias?.name)
            ? importDecl.module_path
            : importDecl.alias!.name;

        if (!string.IsNullOrWhiteSpace(importDecl.alias?.name)) {
            var aliasName = importDecl.alias!.name;
            var existing = scope.lookup(aliasName);
            if (existing is not null && existing.kind != SymbolKind.import) {
                report_import_alias_shadows_local(importDecl, aliasName, filePath, model);
            }
        }

        var symbol = new Symbol(importName, SymbolKind.import,
            containingScope: scope,
            definitionSpan: default,
            definitionSourceSpan: to_source_span(importDecl.span, filePath),
            filePath: filePath);
        scope.define(symbol);
        model.bind_symbol(importDecl.GetHashCode(), symbol);
    }

    private static void report_import_alias_shadows_local(
        DeclareUsing importDecl,
        string aliasName,
        string filePath,
        SemanticModel model) {
        var message = $"导入别名 `{aliasName}` 与当前作用域已有声明同名，造成遮蔽。";
        model.add_diagnostic(ValkyrieDiagnostics.build_semantic_diagnostic(
            ValkyrieRuleNames.IMPORT_ALIAS_SHADOWS_LOCAL,
            message,
            to_source_span(importDecl.span, filePath),
            filePath));
    }

    private static void validate_import_decl(
        DeclareUsing importDecl,
        string? currentNamespace,
        string filePath,
        SemanticModel model,
        IDictionary<string, HashSet<string>> seenImportsByNamespace,
        IDictionary<string, Dictionary<string, string>> aliasesByNamespace) {
        var namespaceKey = currentNamespace ?? string.Empty;
        var importSignature = build_import_signature(importDecl);

        validate_import_selections(importDecl, filePath, model);
        validate_reexport_location(importDecl, filePath, model);

        if (!seenImportsByNamespace.TryGetValue(namespaceKey, out var seenImports)) {
            seenImports = new HashSet<string>(StringComparer.Ordinal);
            seenImportsByNamespace[namespaceKey] = seenImports;
        }

        if (!seenImports.Add(importSignature)) {
            report_duplicate_import(importDecl, filePath, model);
        }

        if (!string.IsNullOrWhiteSpace(importDecl.alias?.name)) {
            var aliasName = importDecl.alias!.name;
            if (!aliasesByNamespace.TryGetValue(namespaceKey, out var aliases)) {
                aliases = new Dictionary<string, string>(StringComparer.Ordinal);
                aliasesByNamespace[namespaceKey] = aliases;
            }

            if (aliases.TryGetValue(aliasName, out var existingSignature)) {
                if (!string.Equals(existingSignature, importSignature, StringComparison.Ordinal)) {
                    report_import_alias_conflict(importDecl, aliasName, filePath, model);
                }
            }
            else {
                aliases[aliasName] = importSignature;
            }
        }

        if (!importDecl.is_reexport ||
            string.IsNullOrWhiteSpace(currentNamespace) ||
            !string.Equals(importDecl.module_path, currentNamespace, StringComparison.Ordinal)) {
            return;
        }

        report_redundant_reexport(importDecl, filePath, model);
    }

    private static string build_import_signature(DeclareUsing importDecl) {
        var orderedSelections = importDecl.selections
            .Select(selection => selection.name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        return string.Join("|",
            importDecl.is_reexport ? "reexport" : "import",
            importDecl.module_path,
            importDecl.alias?.name ?? string.Empty,
            string.Join(",", orderedSelections));
    }

    private static void report_duplicate_import(
        DeclareUsing importDecl,
        string filePath,
        SemanticModel model) {
        var message = $"重复的 `using` 导入：`{describe_import(importDecl)}` 已在当前 namespace 中声明过。";
        model.add_diagnostic(ValkyrieDiagnostics.build_semantic_diagnostic(
            ValkyrieRuleNames.DUPLICATE_USING,
            message,
            to_source_span(importDecl.span, filePath),
            filePath));
    }

    private static void report_import_alias_conflict(
        DeclareUsing importDecl,
        string aliasName,
        string filePath,
        SemanticModel model) {
        var message = $"导入别名 `{aliasName}` 冲突：`{describe_import(importDecl)}` 与当前 namespace 中已有导入使用了相同别名。";
        model.add_diagnostic(ValkyrieDiagnostics.build_semantic_diagnostic(
            ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT,
            message,
            to_source_span(importDecl.span, filePath),
            filePath));
    }

    private static void report_redundant_reexport(
        DeclareUsing importDecl,
        string filePath,
        SemanticModel model) {
        var selectionSuffix = importDecl.selections.Count == 0
            ? string.Empty
            : $".{{{string.Join(", ", importDecl.selections.Select(selection => selection.name))}}}";
        var message = $"`using! {importDecl.module_path}{selectionSuffix}` 是冗余的：同一 namespace 中定义的符号天然可导出，无需再次 reexport。";
        model.add_diagnostic(ValkyrieDiagnostics.build_semantic_diagnostic(
            ValkyrieRuleNames.REDUNDANT_REEXPORT,
            message,
            to_source_span(importDecl.span, filePath),
            filePath));
    }

    /// <summary>
    ///     校验 <c>using</c> 的选择列表：
    ///     <para>
    ///         - <c>using foo.{}</c> 空选择列表 → <c>VALKYRIE_IMPORT_EMPTY_SELECTION</c>。
    ///         - 同一条导入内重复选择同名项 → <c>VALKYRIE_IMPORT_DUPLICATE_SELECTION</c>。
    ///     </para>
    /// </summary>
    private static void validate_import_selections(
        DeclareUsing importDecl,
        string filePath,
        SemanticModel model) {
        if (importDecl.selections.Count == 0) {
            return;
        }

        if (importDecl.selections.Count == 1 &&
            importDecl.selections[0].name.Length == 0) {
            report_empty_selection(importDecl, filePath, model);
            return;
        }

        var seenSelections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var selection in importDecl.selections) {
            if (!seenSelections.Add(selection.name)) {
                report_duplicate_selection(importDecl, selection.name, filePath, model);
            }
        }
    }

    private static void report_empty_selection(
        DeclareUsing importDecl,
        string filePath,
        SemanticModel model) {
        var message = $"`using {importDecl.module_path}.{{}}` 的选择列表为空，请提供至少一个导入项。";
        model.add_diagnostic(ValkyrieDiagnostics.build_semantic_diagnostic(
            ValkyrieRuleNames.IMPORT_EMPTY_SELECTION,
            message,
            to_source_span(importDecl.span, filePath),
            filePath));
    }

    private static void report_duplicate_selection(
        DeclareUsing importDecl,
        string duplicatedName,
        string filePath,
        SemanticModel model) {
        var message = $"`using {importDecl.module_path}` 选择项 `{duplicatedName}` 在同一条导入中重复声明。";
        model.add_diagnostic(ValkyrieDiagnostics.build_semantic_diagnostic(
            ValkyrieRuleNames.IMPORT_DUPLICATE_SELECTION,
            message,
            to_source_span(importDecl.span, filePath),
            filePath));
    }

    /// <summary>
    ///     校验 <c>using!</c> reexport 是否位于主入口文件（约定为 <c>_.v</c>）。
    ///     <para>
    ///         启发式：仅当 <paramref name="filePath" /> 的文件名以 <c>_.v</c> 结尾时视为合法主入口，
    ///         否则 reexport 视为出现在非主入口文件，触发 <c>VALKYRIE_REEXPORT_OUTSIDE_PRIMARY_ENTRY</c>。
    ///     </para>
    /// </summary>
    private static void validate_reexport_location(
        DeclareUsing importDecl,
        string filePath,
        SemanticModel model) {
        if (!importDecl.is_reexport) {
            return;
        }

        if (is_primary_entry_file(filePath)) {
            return;
        }

        var message = $"`using!` reexport 出现在非主入口文件 `{filePath}` 中，应只在 `_.v` 入口文件聚合公开面。";
        model.add_diagnostic(ValkyrieDiagnostics.build_semantic_diagnostic(
            ValkyrieRuleNames.REEXPORT_OUTSIDE_PRIMARY_ENTRY,
            message,
            to_source_span(importDecl.span, filePath),
            filePath));
    }

    /// <summary>
    ///     判断给定文件路径是否为 namespace 主入口文件（约定为 <c>_.v</c>）。
    /// </summary>
    private static bool is_primary_entry_file(string? filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            return true;
        }

        var fileName = System.IO.Path.GetFileName(filePath);
        return string.Equals(fileName, "_.v", StringComparison.OrdinalIgnoreCase);
    }

    private static string describe_import(DeclareUsing importDecl) {
        var selectionSuffix = importDecl.selections.Count == 0
            ? string.Empty
            : $".{{{string.Join(", ", importDecl.selections.Select(selection => selection.name))}}}";
        var aliasSuffix = string.IsNullOrWhiteSpace(importDecl.alias?.name)
            ? string.Empty
            : $" as {importDecl.alias!.name}";
        return $"{(importDecl.is_reexport ? "using!" : "using")} {importDecl.module_path}{selectionSuffix}{aliasSuffix}";
    }

    private static IReadOnlyList<DeclareMicro> collect_functions(IReadOnlyList<CompilationUnit> compilationUnits) {
        var functions = new List<DeclareMicro>();

        foreach (var compilationUnit in compilationUnits) collect_functions(compilationUnit.declarations, functions);

        return functions;
    }

    private static void collect_functions(IReadOnlyList<AstNode> declarations, List<DeclareMicro> functions) {
        foreach (var declaration in declarations)
            switch (declaration) {
                case DeclareMicro functionDecl:
                    functions.Add(functionDecl);
                    break;
                case DeclareNamespace namespaceDecl:
                    collect_functions(namespaceDecl.declarations, functions);
                    break;
            }
    }

    private static Scope get_or_create_namespace_scope(Scope globalScope, string namespaceName) {
        var currentScope = globalScope;
        foreach (var segment in namespaceName.Split('.', StringSplitOptions.RemoveEmptyEntries)) currentScope = currentScope.get_or_create_child_scope(segment);

        return currentScope;
    }

    private static string combine_namespace(string? currentNamespace, string declaredNamespace) {
        if (string.IsNullOrWhiteSpace(currentNamespace)) return declaredNamespace;

        if (string.IsNullOrWhiteSpace(declaredNamespace)) return currentNamespace;

        if (declaredNamespace.Equals(currentNamespace, StringComparison.Ordinal)) return declaredNamespace;

        if (declaredNamespace.StartsWith(currentNamespace + ".", StringComparison.Ordinal)) return declaredNamespace;

        return $"{currentNamespace}.{declaredNamespace}";
    }

    #endregion

    #region TypeAnnotation 转换

    private void validate_entry_functions(CompilationUnit compilationUnit, string filePath, SemanticModel model,
        ValkyrieTypeInference typeInference)
    {
        validate_entry_functions(compilationUnit.declarations, filePath, model, typeInference);
    }

    private void validate_entry_functions(IReadOnlyList<AstNode> declarations, string filePath, SemanticModel model,
        ValkyrieTypeInference typeInference)
    {
        foreach (var declaration in declarations)
            switch (declaration)
            {
                case DeclareNamespace namespaceDecl:
                    validate_entry_functions(namespaceDecl.declarations, filePath, model, typeInference);
                    break;
                case DeclareMicro function:
                    validate_entry_function(function, filePath, model, typeInference);
                    break;
            }
    }

    private void validate_entry_function(DeclareMicro function, string filePath, SemanticModel model,
        ValkyrieTypeInference typeInference)
    {
        var isMain = function.annotations.attributes().Any(attribute => attribute.name == "main");
        if (!isMain) return;

        var returnType = function.return_type is not null
            ? convert_type_annotation(function.return_type)
            : typeInference.infer_function_return_type(function);
        if (is_allowed_main_return_type(returnType)) return;

        model.add_diagnostic(new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"[main] 入口不能返回 `{returnType.name}`；只允许 `unit` 或 `ExitCode`。",
            to_source_span(function.span, filePath),
            "VALK_MAIN_RETURN",
            filePath));
    }

    private static bool is_allowed_main_return_type(IType returnType)
    {
        return returnType switch
        {
            PrimitiveType { name: "unit" } => true,
            NamedType { name: "ExitCode" } => true,
            _ => false
        };
    }

    internal IType convert_type_annotation(TypeNode typeNode) {
        if (typeNode is null) return UnknownType.instance;

        if (typeNode is TypeMicroNode functionNode) {
            var parameterTypes = is_empty_function_parameter_list(functionNode.parameter_type)
                ? []
                : flatten_product(functionNode.parameter_type)
                    .Select(convert_type_annotation)
                    .ToList();
            return new FunctionType(parameterTypes, convert_type_annotation(functionNode.return_type));
        }

        if (typeNode is TypeExpressionUnaryNode { @operator: TypeUnaryOperator.nullable } nullableNode) {
            var inner = convert_type_annotation(nullableNode.operand);
            return new NullableType(inner);
        }

        return convert_type_annotation_core(typeNode);
    }

    private IType convert_type_annotation_core(TypeNode typeNode) {
        if (typeNode is TypeLiteralTupleNode tupleNode)
        {
            var elements = tupleNode.elements
                .Select(element => (element.label?.name, convert_type_annotation(element.type)))
                .ToArray();
            return create_tuple_type(elements);
        }

        if (typeNode is TypeExpressionBinaryNode { @operator: TypeBinaryOperator.or } unionNode) {
            var members = new List<IType>();
            foreach (var member in flatten_binary(unionNode, TypeBinaryOperator.or)) members.Add(convert_type_annotation(member));

            return new NamedType("union", "union", typeArguments: members);
        }

        if (typeNode is TypeExpressionBinaryNode { @operator: TypeBinaryOperator.and } intersectionNode) {
            var members = new List<IType>();
            foreach (var member in flatten_binary(intersectionNode, TypeBinaryOperator.and)) members.Add(convert_type_annotation(member));

            return new NamedType("intersection", "intersection", typeArguments: members);
        }

        if (typeNode is TypeExpressionBinaryNode { @operator: TypeBinaryOperator.product } productNode) {
            var items = flatten_product(productNode);
            if (items is [TypeLiteralNamePathNode constructorNode, _, ..]) {
                var constructorName = get_type_name(constructorNode);
                var arguments = items.Skip(1).Select(convert_type_annotation).ToList();

                if (constructorName == "Array") return new NamedType(constructorName, constructorName, typeArguments: arguments);

                if (constructorName == "FixedArray") return new NamedType(constructorName, "struct", typeArguments: arguments);

                if (constructorName == "micro") return new FunctionType(arguments, UnknownType.instance);

                if (constructorName == "&") {
                    return new NamedType("&", "&", typeArguments: arguments);
                }

                return resolve_declared_named_type(constructorName, arguments);
            }

            return create_tuple_type([.. items.Select(convert_type_annotation)]);
        }

        if (typeNode is TypeLiteralNamePathNode literalNode)
        {
            var typeName = get_type_name(literalNode);
            var typeArguments = convert_type_arguments(literalNode.type_arguments);
            if (typeName is "string" or "String")
            {
                if (_declared_types.TryGetValue(typeName, out var declaredLegacyNamedType))
                {
                    return declaredLegacyNamedType;
                }

                // 历史遗留的 `string` / `String` 别名归一化为 utf8
                return ValkyrieTextTypeFacts.create_semantic_owned_text_type(ValkyrieTextTypeFacts.utf8_name);
            }
            if (ValkyrieBuiltinTypeFacts.try_create_non_text_annotation_type(typeName) is { } builtinType)
            {
                return builtinType;
            }

            // 类型别名解析（如 utf8 → Utf8Text）
            if (_type_alias_map.TryGetValue(typeName, out var canonicalName)
                && _declared_types.TryGetValue(canonicalName, out var canonicalType))
                return canonicalType;

            return typeName switch
            {
                "i8" or "i16" or "i32" or "i64" or "isize" or
                "u8" or "u16" or "u32" or "u64" or "usize" or
                "f32" or "f64" or "bool" => new PrimitiveType(typeName),
                "char" => ValkyrieTextTypeFacts.create_semantic_char_type(),
                _ when ValkyrieTextTypeFacts.try_create_semantic_text_type(typeName) is { } textType
                    => resolve_owned_text_type(typeName, textType),
                _ => resolve_declared_named_type(typeName, typeArguments)
            };
        }

        return UnknownType.instance;
    }

    /// <summary>
    ///     将 owned_text 类型名解析为对应的 class 类型。
    ///     例如 <c>utf16</c> → <c>Utf16Text</c>，<c>utf8</c> → <c>Utf8Text</c>。
    ///     优先使用类型别名注册的映射，其次尝试推断类名。
    /// </summary>
    private IType resolve_owned_text_type(string typeName, IType textType)
    {
        if (_declared_types.TryGetValue(typeName, out var declaredType)
            && declaredType is NamedType { kind_tag: not "owned_text" })
        {
            return declaredType;
        }

        var className = ValkyrieTextTypeFacts.try_get_class_name_for_owned_text(typeName);
        if (className is not null && _declared_types.TryGetValue(className, out var classType))
        {
            return classType;
        }

        return textType;
    }


    private IType resolve_declared_named_type(string typeName, IReadOnlyList<IType>? typeArguments = null)
    {
        if (typeName is "i8" or "i16" or "i32" or "i64" or "i128" or "isize" or
            "u8" or "u16" or "u32" or "u64" or "u128" or "usize" or
            "f32" or "f64" or "f128" or "bool")
        {
            return new PrimitiveType(typeName);
        }

        if (typeName is "unit" or "void")
        {
            return new PrimitiveType(typeName);
        }

        if (_declared_types.TryGetValue(typeName, out var declaredType))
        {
            if (declaredType is NamedType namedType)
                return new NamedType(
                    namedType.name,
                    namedType.kind_tag,
                    namedType.base_type,
                    typeArguments ?? namedType.type_arguments,
                    namedType.members);

            if (typeArguments is null or { Count: 0 }) return declaredType;
        }

        return new NamedType(typeName, typeName, typeArguments: typeArguments);
    }

    private static NamedType create_tuple_type(IReadOnlyList<IType> elementTypes)
    {
        var namedElements = elementTypes
            .Select((elementType, index) => ((string?)null, elementType))
            .ToArray();
        return create_tuple_type(namedElements);
    }

    private static NamedType create_tuple_type(IReadOnlyList<(string? label, IType type)> elements)
    {
        var typeArguments = elements.Select(element => element.type).ToArray();
        var renderedElements = elements.Select(element =>
            string.IsNullOrWhiteSpace(element.label)
                ? describe_type(element.type)
                : $"{element.label}: {describe_type(element.type)}");
        var tupleName = $"({string.Join(", ", renderedElements)})";
        return new NamedType(tupleName, ValkyrieBuiltinTypeFacts.tuple_kind_tag, typeArguments: typeArguments, members: create_tuple_members(elements));
    }

    private static IReadOnlyList<ISymbol> create_tuple_members(IReadOnlyList<(string? label, IType type)> elements)
    {
        var scope = new Scope("tuple");
        var members = new List<ISymbol>(elements.Count);
        foreach (var element in elements)
        {
            if (string.IsNullOrWhiteSpace(element.label))
            {
                continue;
            }

            members.Add(new Symbol(
                element.label!,
                SymbolKind.field,
                SymbolAccessibility.@public,
                element.type,
                scope,
                isReadOnly: true));
        }

        return members;
    }

    private void validate_call_argument_types(CompilationUnit compilationUnit, string filePath, SemanticModel model,
        ValkyrieTypeInference typeInference)
    {
        validate_call_argument_types(compilationUnit.declarations, filePath, model, typeInference);
    }

    private void validate_call_argument_types(IReadOnlyList<AstNode> declarations, string filePath, SemanticModel model,
        ValkyrieTypeInference typeInference)
    {
        foreach (var declaration in declarations)
            switch (declaration)
            {
                case DeclareNamespace namespaceDecl:
                    validate_call_argument_types(namespaceDecl.declarations, filePath, model, typeInference);
                    break;
                case DeclareMicro function:
                    validate_callable_body(function.body, function.parameters, filePath, model, typeInference);
                    break;
                case DeclareStructure { body: not null } structure:
                    foreach (var method in structure.body.methods) validate_callable_body(method.body, method.parameters, filePath, model, typeInference);
                    break;
                case DeclareClass { body: not null } klass:
                    foreach (var method in klass.body.methods) validate_callable_body(method.body, method.parameters, filePath, model, typeInference);
                    break;
                case DeclareTrait { body: not null } trait:
                    foreach (var method in trait.body.methods) validate_callable_body(method.body, method.parameters, filePath, model, typeInference);
                    break;
                case DeclareImply imply:
                    var ownerTypeName = get_type_name_from_type_node(imply.target_type);
                    foreach (var method in imply.methods) validate_callable_body(method.body, method.parameters, filePath, model, typeInference, ownerTypeName);
                    break;
            }
    }

    private void validate_callable_body(BlockStmt? body, IReadOnlyList<TermParameterList> parameters, string filePath,
        SemanticModel model, ValkyrieTypeInference typeInference, string? ownerTypeName = null)
    {
        if (body is null) return;

        validate_function_body(body, create_parameter_scope(parameters, ownerTypeName), filePath, model, typeInference);
    }

    private Dictionary<string, IType> create_parameter_scope(IReadOnlyList<TermParameterList> parameters, string? ownerTypeName = null)
    {
        var scope = new Dictionary<string, IType>(StringComparer.Ordinal);
        foreach (var parameter in parameters)
        foreach (var item in parameter.items)
        {
            if (string.IsNullOrWhiteSpace(item.name?.name)) continue;

            scope[item.name!.name] = convert_parameter_type(item.bound_type, item.name!.name, ownerTypeName);
        }

        return scope;
    }

    /// <summary>
    ///     转换参数类型，处理 <c>Self</c> 和 <c>self</c> 参数在 <c>imply</c> 块中的类型解析。
    /// </summary>
    private IType convert_parameter_type(TypeNode? boundType, string paramName, string? ownerTypeName)
    {
        if (boundType is null)
        {
            if (ownerTypeName is not null) return resolve_declared_named_type(ownerTypeName);

            return create_any_type_instance();
        }

        if (boundType is TypeLiteralNamePathNode literalNode)
        {
            var name = get_type_name(literalNode);

            // 显式 Self 类型
            if (ValkyrieBuiltinTypeFacts.is_self_type_name(name) && ownerTypeName is not null) return resolve_declared_named_type(ownerTypeName);

            // self 参数的 BoundType 可能为空名称或默认 any
            if (ownerTypeName is not null &&
                (string.IsNullOrEmpty(name) || ValkyrieBuiltinTypeFacts.is_any_type_name(name)))
                // 仅当参数名为 self 时才用 ownerTypeName 替换
                if (string.Equals(paramName, ValkyrieBuiltinTypeFacts.self_value_name, StringComparison.OrdinalIgnoreCase))
                    return resolve_declared_named_type(ownerTypeName);
        }

        return convert_type_annotation(boundType);
    }

    private IType create_any_type_instance()
    {
        return ValkyrieBuiltinTypeFacts.any_type;
    }

    private void validate_function_body(BlockStmt body, Dictionary<string, IType> scope, string filePath,
        SemanticModel model, ValkyrieTypeInference typeInference)
    {
        foreach (var statement in body.statements) validate_call_argument_node(statement, scope, filePath, model, typeInference);
    }

    private void validate_call_argument_node(AstNode node, Dictionary<string, IType> scope, string filePath,
        SemanticModel model, ValkyrieTypeInference typeInference)
    {
        switch (node)
        {
            case DeclareLet letDecl:
                if (letDecl.initializer is not null) validate_call_argument_node(letDecl.initializer, scope, filePath, model, typeInference);

                validate_let_initializer_type(letDecl, scope, filePath, model, typeInference);

                if (!string.IsNullOrWhiteSpace(letDecl.name?.name)) scope[letDecl.name!.name] = infer_declared_or_initializer_type(letDecl, scope, typeInference);
                break;
            case ReturnStatement { value: AstNode value }:
                validate_call_argument_node(value, scope, filePath, model, typeInference);
                break;
            case YieldStatement { value: AstNode value }:
                validate_call_argument_node(value, scope, filePath, model, typeInference);
                break;
            case IfStatement ifStatement:
                validate_call_argument_node(ifStatement.condition, scope, filePath, model, typeInference);
                validate_function_body(ifStatement.then_block, new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                    filePath, model, typeInference);
                if (ifStatement.else_block is AstNode elseNode)
                    validate_call_argument_node(elseNode, new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                        filePath, model, typeInference);
                break;
            case TermCallExpression call:
                validate_call_expression(call, scope, filePath, model, typeInference);
                break;
            case TermBinaryExpression binary:
                validate_call_argument_node(binary.left, scope, filePath, model, typeInference);
                validate_call_argument_node(binary.right, scope, filePath, model, typeInference);
                break;
            case TermUnaryExpression unary:
                validate_call_argument_node(unary.operand, scope, filePath, model, typeInference);
                break;
            case TermAsExpression cast:
                validate_call_argument_node(cast.operand, scope, filePath, model, typeInference);
                validate_cast_expression(cast, scope, filePath, model, typeInference);
                break;
            case TermDotExpression dot:
                validate_call_argument_node(dot.caller, scope, filePath, model, typeInference);
                break;
            case TermOrdinalExpression ordinal:
                validate_call_argument_node(ordinal.target, scope, filePath, model, typeInference);
                foreach (var index in ordinal.indices) validate_call_argument_node(index, scope, filePath, model, typeInference);
                break;
            case TermOffsetExpression offset:
                validate_call_argument_node(offset.target, scope, filePath, model, typeInference);
                foreach (var index in offset.indices) validate_call_argument_node(index, scope, filePath, model, typeInference);
                break;
        }
    }

    private void validate_call_expression(TermCallExpression call, Dictionary<string, IType> scope, string filePath,
        SemanticModel model, ValkyrieTypeInference typeInference)
    {
        validate_call_argument_node(call.caller, scope, filePath, model, typeInference);

        if (call.call_body.function_body is not null)
            validate_function_body(call.call_body.function_body, new Dictionary<string, IType>(scope, StringComparer.Ordinal),
                filePath, model, typeInference);

        var arguments = call.call_body.term_arguments?.items ?? [];
        foreach (var argument in arguments) validate_call_argument_node(argument.value, scope, filePath, model, typeInference);

        if (typeInference.infer_expression_type(call.caller, scope) is not FunctionType functionType) return;

        if (arguments.Count != functionType.parameter_types.Count)
        {
            model.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.error,
                $"调用参数个数不匹配：期望 {functionType.parameter_types.Count} 个，实际 {arguments.Count} 个。",
                to_source_span(call.span, filePath),
                "VALK_CALL_ARITY",
                filePath));
            return;
        }

        for (var i = 0; i < arguments.Count; i++)
        {
            var expectedType = functionType.parameter_types[i];
            var actualType = typeInference.infer_expression_type(arguments[i].value, scope);
            // Debug log removed - was hardcoded to e:\RiderProjects\NyarVM.cs\tools\diag_simple_cli\...
            if (is_expression_assignable_to(expectedType, actualType, arguments[i].value)) continue;

            model.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.error,
                $"调用参数类型不匹配：第 {i + 1} 个参数期望 `{describe_type(expectedType)}`，实际 `{describe_type(actualType)}`。",
                to_source_span(arguments[i].value.span, filePath),
                "VALK_CALL_ARGUMENT_TYPE",
                filePath));
        }
    }

    private void validate_let_initializer_type(DeclareLet letDecl, Dictionary<string, IType> scope, string filePath,
        SemanticModel model, ValkyrieTypeInference typeInference)
    {
        if (letDecl.var_type is null || letDecl.initializer is null)
        {
            return;
        }

        var expectedType = convert_type_annotation(letDecl.var_type);
        var actualType = typeInference.infer_expression_type(letDecl.initializer, scope);
        if (is_expression_assignable_to(expectedType, actualType, letDecl.initializer))
        {
            return;
        }

        model.add_diagnostic(new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"变量 `{letDecl.name?.name ?? "<anonymous>"}` 的初始化类型不匹配：期望 `{describe_type(expectedType)}`，实际 `{describe_type(actualType)}`。",
            to_source_span(letDecl.initializer.span, filePath),
            "VALK_LET_INITIALIZER_TYPE",
            filePath));
    }

    private IType infer_declared_or_initializer_type(DeclareLet letDecl, Dictionary<string, IType> scope,
        ValkyrieTypeInference typeInference)
    {
        if (letDecl.var_type is not null)
        {
            return convert_type_annotation(letDecl.var_type);
        }

        if (letDecl.initializer is not null)
        {
            return typeInference.infer_expression_type(letDecl.initializer, scope);
        }

        return UnknownType.instance;
    }

    private void validate_cast_expression(TermAsExpression cast, Dictionary<string, IType> scope, string filePath,
        SemanticModel model, ValkyrieTypeInference typeInference)
    {
        var sourceType = typeInference.infer_expression_type(cast.operand, scope);
        var targetType = convert_type_annotation(cast.target_type);
        if (are_explicit_cast_types_compatible(sourceType, targetType))
        {
            return;
        }

        if (is_user_defined_type(sourceType) && is_user_defined_type(targetType))
        {
            model.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.error,
                $"类型 `{describe_type(sourceType)}` 和 `{describe_type(targetType)}` 之间不存在 `As` 实现，请使用 `to` 或 `into` 进行转换。",
                to_source_span(cast.span, filePath),
                "VALK_INVALID_CAST_NO_AS_TRAIT",
                filePath));
        }
        else
        {
            model.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.error,
                $"显式类型转换不受支持：无法将 `{describe_type(sourceType)}` 转换为 `{describe_type(targetType)}`。",
                to_source_span(cast.span, filePath),
                "VALK_INVALID_CAST",
                filePath));
        }
    }

    /// <summary>
    ///     验证类型测试表达式的合法性
    /// </summary>
    private void validate_is_expression(TermIsExpression isExpression, Dictionary<string, IType> scope, string filePath,
        SemanticModel model, ValkyrieTypeInference typeInference)
    {
        var sourceType = typeInference.infer_expression_type(isExpression.operand, scope);

        var targetType = resolve_is_target_type(isExpression.target_pattern_node, scope);
        if (targetType is null)
        {
            model.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.error,
                $"`is` 目标模式 `{isExpression.target_pattern_node.GetType().Name}` 无法解析为目标类型。",
                to_source_span(isExpression.span, filePath),
                "VALK_INVALID_IS_TARGET",
                filePath));
            return;
        }

        if (isExpression.is_nullable)
        {
            if (sourceType is not NullableType && sourceType is not NamedType { is_union_type: true })
            {
                model.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.warning,
                    $"`is?` 左侧表达式类型 `{describe_type(sourceType)}` 不是可空类型，`is?` 等价于 `is`。",
                    to_source_span(isExpression.span, filePath),
                    "VALK_REDUNDANT_IS_NULLABLE",
                    filePath));
            }
        }

        if (sourceType is PrimitiveType sourcePrim && targetType is PrimitiveType targetPrim)
        {
            if (sourcePrim.name == targetPrim.name)
            {
                return;
            }
        }

        if (sourceType is NamedType { is_union_type: true } sourceUnion)
        {
            var isMember = sourceUnion.type_arguments.Any(m => targetType.is_assignable_from(m));
            if (!isMember)
            {
                model.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.warning,
                    $"`is` 测试 `{describe_type(targetType)}` 不是 `{describe_type(sourceType)}` 的成员，结果始终为 `false`。",
                    to_source_span(isExpression.span, filePath),
                    "VALK_IS_ALWAYS_FALSE",
                    filePath));
            }
            return;
        }

        if (sourceType is NamedType { is_intersection_type: true } sourceIntersection)
        {
            var isCompatible = sourceIntersection.type_arguments.Any(m => targetType.is_assignable_from(m));
            if (!isCompatible)
            {
                model.add_diagnostic(new SemanticDiagnostic(
                    DiagnosticSeverity.warning,
                    $"`is` 测试 `{describe_type(targetType)}` 与 `{describe_type(sourceType)}` 无交集，结果始终为 `false`。",
                    to_source_span(isExpression.span, filePath),
                    "VALK_IS_ALWAYS_FALSE",
                    filePath));
            }
            return;
        }

        if (!are_explicit_cast_types_compatible(sourceType, targetType) &&
            !targetType.is_assignable_from(sourceType))
        {
            model.add_diagnostic(new SemanticDiagnostic(
                DiagnosticSeverity.warning,
                $"`is` 测试 `{describe_type(sourceType)}` 与 `{describe_type(targetType)}` 始终不兼容。",
                to_source_span(isExpression.span, filePath),
                "VALK_IS_ALWAYS_FALSE",
                filePath));
        }
    }

    /// <summary>
    ///     从模式节点解析目标类型
    /// </summary>
    private IType? resolve_is_target_type(PatternNode pattern, Dictionary<string, IType> scope)
    {
        switch (pattern)
        {
            case PatternLiteralObjectNode { path: { full_name: { Length: > 0 } name } }:
            {
                if (scope.TryGetValue(name, out var type))
                {
                    return type;
                }
                return convert_type_annotation(new TypeLiteralNamePathNode
                {
                    path = new QualifiedPathNode { segments = [new IdentifierNode(name)] }
                });
            }
            default:
                return null;
        }
    }

    private static bool are_call_types_compatible(IType expectedType, IType actualType)
    {
        if (expectedType is ErrorType or AutoType || actualType is ErrorType or AutoType)
        {
            return true;
        }

        if (expectedType is UnknownType || actualType is UnknownType)
        {
            return true;
        }

        if (is_generic_placeholder(expectedType))
        {
            return true;
        }

        if (expectedType is NamedType { is_union_type: true } expectedUnion)
        {
            return expectedUnion.type_arguments.Any(member => are_call_types_compatible(member, actualType));
        }

        if (expectedType is NamedType { is_intersection_type: true } expectedIntersection)
        {
            return expectedIntersection.type_arguments.All(member => are_call_types_compatible(member, actualType));
        }

        if (actualType is NamedType { is_union_type: true } actualUnion)
        {
            return actualUnion.type_arguments.All(member => are_call_types_compatible(expectedType, member));
        }

        if (actualType is NamedType { is_intersection_type: true } actualIntersection)
        {
            return actualIntersection.type_arguments.Any(member => are_call_types_compatible(expectedType, member));
        }

        if (is_implicit_numeric_conversion(expectedType, actualType))
        {
            return true;
        }

        if (is_implicit_text_conversion(expectedType, actualType))
        {
            return true;
        }

        if (is_compatible_owned_text_type(expectedType, actualType))
        {
            return true;
        }

        if (are_generic_named_types_compatible(expectedType, actualType))
        {
            return true;
        }

        return expectedType.is_assignable_from(actualType);
    }

    private static bool is_expression_assignable_to(IType expectedType, IType actualType, AstNode expression)
    {
        if (are_call_types_compatible(expectedType, actualType))
        {
            return true;
        }

        return expression switch
        {
            TermLiteralTextNode textLiteral => is_text_literal_assignable_to(expectedType, actualType, textLiteral),
            _ => false
        };
    }

    private static bool is_text_literal_assignable_to(IType expectedType, IType actualType, TermLiteralTextNode literal)
    {
        if (literal.literal_kind == TextLiteralKind.literal_char)
        {
            return is_char_type(expectedType) && has_single_text_element(literal.value);
        }

        if (literal.literal_kind != TextLiteralKind.literal_text)
        {
            return false;
        }

        if (is_char_type(expectedType))
        {
            return has_single_text_element(literal.value);
        }

        if (ValkyrieTextTypeFacts.is_literal_text_name(actualType.name) &&
            is_assignable_text_target(expectedType))
        {
            return true;
        }

        return false;
    }

    private static bool is_assignable_text_target(IType expectedType)
    {
        var expectedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(expectedType.name);
        return ValkyrieTextTypeFacts.is_owned_text_name(expectedTypeName)
               || ValkyrieTextTypeFacts.is_owned_text_class_name(expectedTypeName);
    }

    private static bool is_char_type(IType type)
    {
        return ValkyrieTextTypeFacts.is_char_name(type.name);
    }

    private static bool has_single_text_element(string value)
    {
        return !string.IsNullOrEmpty(value) && StringInfo.ParseCombiningCharacters(value).Length == 1;
    }

    private static bool are_explicit_cast_types_compatible(IType sourceType, IType targetType)
    {
        if (sourceType is ErrorType or AutoType || targetType is ErrorType or AutoType)
        {
            return true;
        }

        if (sourceType is UnknownType || targetType is UnknownType)
        {
            return true;
        }

        if (targetType.is_assignable_from(sourceType))
        {
            return true;
        }

        if (sourceType is NullableType sourceNullable && targetType is NullableType targetNullable)
        {
            return are_explicit_cast_types_compatible(sourceNullable.inner_type, targetNullable.inner_type);
        }

        if (targetType is NullableType nullableTarget)
        {
            return are_explicit_cast_types_compatible(sourceType, nullableTarget.inner_type);
        }

        if (sourceType is NamedType { is_union_type: true } sourceUnion)
        {
            return sourceUnion.type_arguments.All(member => are_explicit_cast_types_compatible(member, targetType));
        }

        if (targetType is NamedType { is_union_type: true } targetUnion)
        {
            return targetUnion.type_arguments.Any(member => are_explicit_cast_types_compatible(sourceType, member));
        }

        if (sourceType is NamedType { is_intersection_type: true } sourceIntersection)
        {
            return sourceIntersection.type_arguments.Any(member => are_explicit_cast_types_compatible(member, targetType));
        }

        if (targetType is NamedType { is_intersection_type: true } targetIntersection)
        {
            return targetIntersection.type_arguments.All(member => are_explicit_cast_types_compatible(sourceType, member));
        }

        if (sourceType is NamedType sourceNamed &&
            targetType is NamedType targetNamed &&
            is_row_like_type(sourceNamed) &&
            is_row_like_type(targetNamed))
        {
            if (targetType.is_assignable_from(sourceType))
            {
                return true;
            }

            if (sourceNamed.members is { Count: > 0 } && targetNamed.members is { Count: > 0 })
            {
                return sourceNamed.members.All(sourceMember =>
                    targetNamed.members.Any(targetMember =>
                        targetMember.name == sourceMember.name));
            }
        }

        return is_explicit_numeric_cast(sourceType, targetType);
    }

    /// <summary>
    ///     判断 NamedType 是否为 row-like 类型（非 union/intersection，且是匿名 row 或具名 class/struct 等）
    /// </summary>
    private static bool is_row_like_type(NamedType type)
    {
        if (type.is_union_type || type.is_intersection_type || type.kind_tag is { Length: > 0 })
        {
            return false;
        }

        return ValkyrieBuiltinTypeFacts.is_anonymous_row_name(type.name)
               || type.base_type is not null
               || type.members is { Count: > 0 };
    }

    /// <summary>
    ///     判断类型是否为用户自定义类型（非原始类型、非 union/intersection、非 AutoType/ErrorType）
    /// </summary>
    private static bool is_user_defined_type(IType type)
    {
        return type is NamedType namedType
               && !namedType.is_union_type
               && !namedType.is_intersection_type
               && namedType.kind_tag.Length == 0
               && !ValkyrieBuiltinTypeFacts.is_intrinsic_type_name(namedType.name);
    }

    private static bool is_explicit_numeric_cast(IType sourceType, IType targetType)
    {
        if (sourceType is not PrimitiveType sourcePrimitive ||
            targetType is not PrimitiveType targetPrimitive)
        {
            return false;
        }

        if (ValkyrieBuiltinTypeFacts.is_numeric_or_castable_type_name(sourcePrimitive.name) &&
            ValkyrieBuiltinTypeFacts.is_numeric_or_castable_type_name(targetPrimitive.name))
        {
            return true;
        }

        return false;
    }

    private static bool is_generic_placeholder(IType type)
    {
        return type is NamedType { type_arguments.Count: 0, name.Length: > 0 } namedType &&
               ValkyrieBuiltinTypeFacts.is_generic_placeholder_name(namedType.name);
    }

    private static bool are_generic_named_types_compatible(IType expectedType, IType actualType)
    {
        if (expectedType is not NamedType expectedNamed || actualType is not NamedType actualNamed)
        {
            return false;
        }

        if (!string.Equals(expectedNamed.name, actualNamed.name, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(expectedNamed.kind_tag, actualNamed.kind_tag, StringComparison.Ordinal))
        {
            return false;
        }

        if (expectedNamed.type_arguments.Count == 0 || expectedNamed.type_arguments.Count != actualNamed.type_arguments.Count)
        {
            return false;
        }

        for (var i = 0; i < expectedNamed.type_arguments.Count; i++)
        {
            if (!are_call_types_compatible(expectedNamed.type_arguments[i], actualNamed.type_arguments[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool is_implicit_numeric_conversion(IType expectedType, IType actualType)
    {
        if (expectedType is not PrimitiveType expectedPrimitive || actualType is not PrimitiveType actualPrimitive)
        {
            return false;
        }

        var actualRank = ValkyrieBuiltinTypeFacts.get_numeric_rank(actualPrimitive.name);
        var expectedRank = ValkyrieBuiltinTypeFacts.get_numeric_rank(expectedPrimitive.name);
        if (actualRank < 0 || expectedRank < 0)
        {
            return false;
        }

        var expectedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(expectedPrimitive.name);
        var actualTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(actualPrimitive.name);
        var expectedIsFloat = expectedTypeName is "f32" or "f64" or "f128";
        var actualIsFloat = actualTypeName is "f32" or "f64" or "f128";

        if (actualIsFloat && !expectedIsFloat)
        {
            return false;
        }

        if (ValkyrieBuiltinTypeFacts.is_unsigned_numeric_name(actualPrimitive.name) &&
            ValkyrieBuiltinTypeFacts.is_signed_numeric_target_name(expectedPrimitive.name))
        {
            return false;
        }

        return expectedRank >= actualRank;
    }

    /// <summary>
    ///     检查文本类型之间是否存在隐式转换。
    ///     当前仅保留 owned_text 与其对应 class 类型之间的兼容入口。
    /// </summary>
    private static bool is_implicit_text_conversion(IType expectedType, IType actualType)
    {
        if (expectedType is not NamedType expectedNamed || actualType is not NamedType actualNamed)
        {
            return false;
        }

        var normalizedActualTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(actualNamed.name);
        if (!ValkyrieTextTypeFacts.is_owned_text_name(normalizedActualTypeName))
        {
            return false;
        }

        if (ValkyrieTextTypeFacts.is_owned_text_name(expectedNamed.name))
        {
            return true;
        }

        if (ValkyrieTextTypeFacts.is_owned_text_class_name(expectedNamed.name))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     检查两个类型是否为兼容的 owned_text 类型。
    ///     例如 owned_text 别名 <c>utf16</c> 与对应的 class <c>Utf16Text</c> 应视为兼容。
    /// </summary>
    private static bool is_compatible_owned_text_type(IType expectedType, IType actualType)
    {
        if (expectedType is not NamedType expectedNamed || actualType is not NamedType actualNamed)
        {
            return false;
        }

        if (expectedNamed.kind_tag == ValkyrieTextTypeFacts.owned_text_kind_tag
            && ValkyrieTextTypeFacts.is_owned_text_name(expectedNamed.name))
        {
            var className = ValkyrieTextTypeFacts.try_get_class_name_for_owned_text(expectedNamed.name);
            if (className is not null && string.Equals(actualNamed.name, className, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (actualNamed.kind_tag == ValkyrieTextTypeFacts.owned_text_kind_tag
            && ValkyrieTextTypeFacts.is_owned_text_name(actualNamed.name))
        {
            var className = ValkyrieTextTypeFacts.try_get_class_name_for_owned_text(actualNamed.name);
            if (className is not null && string.Equals(expectedNamed.name, className, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static TypeNode create_any_type()
    {
        return ValkyrieBuiltinTypeFacts.create_any_type_node();
    }

    private static string describe_type(IType type)
    {
        return type switch
        {
            NamedType { is_union_type: true } unionType => string.Join(" | ", unionType.type_arguments.Select(describe_type)),
            NamedType { is_intersection_type: true } intersectionType => string.Join(" & ", intersectionType.type_arguments.Select(describe_type)),
            NamedType { type_arguments.Count: > 0 } genericType => $"{genericType.name}<{string.Join(", ", genericType.type_arguments.Select(describe_type))}>",
            _ => type.name
        };
    }

    private void validate_explicit_return_annotations(CompilationUnit compilationUnit, string filePath, SemanticModel model)
    {
        if (!is_standard_library_file(filePath))
        {
            return;
        }

        validate_explicit_return_annotations(compilationUnit.declarations, filePath, model);
    }

    private void validate_explicit_return_annotations(IReadOnlyList<ValkyrieNode> declarations, string filePath, SemanticModel model)
    {
        foreach (var declaration in declarations)
        {
            switch (declaration)
            {
                case DeclareNamespace namespaceDecl:
                    validate_explicit_return_annotations(namespaceDecl.declarations, filePath, model);
                    break;
                case DeclareMicro micro:
                    validate_explicit_return_annotation(micro.return_type, micro.span, filePath, model, micro.name?.name ?? "micro");
                    break;
                case DeclareStructure { body: not null } structure:
                    foreach (var method in structure.body.methods)
                    {
                        validate_explicit_return_annotation(method.return_type, method.span, filePath, model, method.name?.name ?? "micro");
                    }
                    break;
                case DeclareClass { body: not null } klass:
                    foreach (var method in klass.body.methods)
                    {
                        validate_explicit_return_annotation(method.return_type, method.span, filePath, model, method.name?.name ?? "micro");
                    }
                    break;
                case DeclareTrait { body: not null } trait:
                    foreach (var method in trait.body.methods)
                    {
                        validate_explicit_return_annotation(method.return_type, method.span, filePath, model, method.name?.name ?? "micro");
                    }
                    break;
                case DeclareImply imply:
                    foreach (var method in imply.methods)
                    {
                        validate_explicit_return_annotation(method.return_type, method.span, filePath, model, method.name?.name ?? "micro");
                    }
                    break;
            }
        }
    }

    private void validate_explicit_return_annotation(TypeNode? returnType, TextSpan span, string filePath, SemanticModel model, string functionName)
    {
        if (returnType is not null && !is_auto_return_type(returnType))
        {
            return;
        }

        model.add_diagnostic(new SemanticDiagnostic(
            DiagnosticSeverity.error,
            $"标准库函数 `{functionName}` 必须显式标注返回类型，不能使用默认 `auto`。",
            to_source_span(span, filePath),
            "VALK_STD_RETURN_EXPLICIT",
            filePath));
    }

    private static bool is_auto_return_type(TypeNode? typeNode)
    {
        return ValkyrieBuiltinTypeFacts.is_auto_return_type(typeNode);
    }

    private static bool is_standard_library_file(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var normalizedPath = filePath.Replace('/', '\\');
        return normalizedPath.Contains("\\examples\\std\\", StringComparison.OrdinalIgnoreCase)
               || normalizedPath.Contains("\\examples\\std.adaptor.", StringComparison.OrdinalIgnoreCase)
               || normalizedPath.Contains("\\std.v\\projects\\std\\", StringComparison.OrdinalIgnoreCase)
               || normalizedPath.Contains("\\std.v\\projects\\std.adaptor.", StringComparison.OrdinalIgnoreCase);
    }

    private static string get_type_name(TypeLiteralNamePathNode node)
    {
        if (node.path.segments.Count == 0)
        {
            return string.Empty;
        }

        return node.path.full_name;
    }

    private List<IType>? convert_type_arguments(TypeArgumentList? typeArguments)
    {
        if (typeArguments is null || typeArguments.items.Count == 0)
        {
            return null;
        }

        return
        [
            .. typeArguments.items
                .Select(argument => convert_type_annotation(argument.argument))
        ];
    }

    /// <summary>
    ///     从 TypeNode 中提取类型名。用于获取 <c>imply</c> 块的所属类型。
    /// </summary>
    private static string? get_type_name_from_type_node(TypeNode? typeNode)
    {
        if (typeNode is TypeLiteralNamePathNode literalNode)
        {
            return get_type_name(literalNode);
        }

        return null;
    }

    private static IReadOnlyList<TypeNode> flatten_product(TypeNode node)
    {
        if (node is TypeExpressionBinaryNode { @operator: TypeBinaryOperator.product } productNode)
        {
            return flatten_binary(productNode, TypeBinaryOperator.product);
        }

        return [node];
    }

    /// <summary>
    ///     判断函数类型的参数列表是否来自 <c>micro()</c> 这样的空参数声明。
    ///     语法层会把空括号折叠为 <c>Unit</c>，但语义层应将其解释为“零参数”，
    ///     不能错误地变成一个 `Unit` 形参。
    /// </summary>
    private static bool is_empty_function_parameter_list(TypeNode node)
    {
        return ValkyrieBuiltinTypeFacts.is_empty_function_parameter_list(node);
    }

    private static IReadOnlyList<TypeNode> flatten_binary(TypeNode node, TypeBinaryOperator op)
    {
        var items = new List<TypeNode>();
        collect_binary(node, op, items);
        return items;
    }

    private static void collect_binary(TypeNode node, TypeBinaryOperator op, List<TypeNode> items)
    {
        if (node is TypeExpressionBinaryNode binary && binary.@operator == op)
        {
            collect_binary(binary.lhs, op, items);
            collect_binary(binary.rhs, op, items);
            return;
        }

        items.Add(node);
    }

    #endregion
}
