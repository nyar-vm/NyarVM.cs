using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Lexer;
using Std.Data.Text.Valkyrie.Parser;
using Nyar.Language.Von;
using Nyar.PackageManager.Package;

namespace Legion.CLI.Document;

/// <summary>
///     表示解析后的项目及其全部文档信息
/// </summary>
public sealed record DocProject
{
    /// <summary>
    ///     项目名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     项目描述
    /// </summary>
    public string description { get; init; } = string.Empty;

    /// <summary>
    ///     模块列表
    /// </summary>
    public List<DocModule> modules { get; init; } = [];
}

/// <summary>
///     表示一个命名空间（模块）
/// </summary>
public sealed record DocModule
{
    /// <summary>
    ///     命名空间名称
    /// </summary>
    public string namespace_name { get; init; } = string.Empty;

    /// <summary>
    ///     模块级文档注释
    /// </summary>
    public string doc_comment { get; init; } = string.Empty;

    /// <summary>
    ///     该模块中的类型列表
    /// </summary>
    public List<DocType> types { get; init; } = [];

    /// <summary>
    ///     该模块中的独立函数列表
    /// </summary>
    public List<DocFunction> functions { get; init; } = [];
}

/// <summary>
///     表示一个类型（class、structure、trait、unite、union、enums、flags）
/// </summary>
public sealed record DocType
{
    /// <summary>
    ///     类型名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     类型种类："class"、"structure"、"trait"、"unite"、"union"、"enums"、"flags"
    /// </summary>
    public string kind { get; init; } = string.Empty;

    /// <summary>
    ///     类型文档注释
    /// </summary>
    public string doc_comment { get; init; } = string.Empty;

    /// <summary>
    ///     泛型参数名称列表
    /// </summary>
    public List<string> generic_params { get; init; } = [];

    /// <summary>
    ///     字段列表
    /// </summary>
    public List<DocField> fields { get; init; } = [];

    /// <summary>
    ///     方法列表
    /// </summary>
    public List<DocFunction> methods { get; init; } = [];

    /// <summary>
    ///     imply 扩展块列表
    /// </summary>
    public List<DocImply> imply_blocks { get; init; } = [];

    /// <summary>
    ///     所在源文件路径
    /// </summary>
    public string source_file { get; init; } = string.Empty;
}

/// <summary>
///     表示一个字段
/// </summary>
public sealed record DocField
{
    /// <summary>
    ///     字段名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     字段类型
    /// </summary>
    public string field_type { get; init; } = string.Empty;

    /// <summary>
    ///     字段文档注释
    /// </summary>
    public string doc_comment { get; init; } = string.Empty;
}

/// <summary>
///     表示一个函数
/// </summary>
public sealed record DocFunction
{
    /// <summary>
    ///     函数名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     完整签名串
    /// </summary>
    public string signature { get; init; } = string.Empty;

    /// <summary>
    ///     函数文档注释
    /// </summary>
    public string doc_comment { get; init; } = string.Empty;

    /// <summary>
    ///     返回类型
    /// </summary>
    public string return_type { get; init; } = string.Empty;

    /// <summary>
    ///     参数列表
    /// </summary>
    public List<DocParam> parameters { get; init; } = [];

    /// <summary>
    ///     所在源文件路径
    /// </summary>
    public string source_file { get; init; } = string.Empty;
}

/// <summary>
///     表示一个函数参数
/// </summary>
public sealed record DocParam
{
    /// <summary>
    ///     参数名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     参数类型
    /// </summary>
    public string param_type { get; init; } = string.Empty;
}

/// <summary>
///     表示一个 imply 扩展块
/// </summary>
public sealed record DocImply
{
    /// <summary>
    ///     被扩展的目标类型
    /// </summary>
    public string target_type { get; init; } = string.Empty;

    /// <summary>
    ///     承诺实现的 contract 类型，可为空
    /// </summary>
    public string contract_type { get; init; } = string.Empty;

    /// <summary>
    ///     扩展块中的方法列表
    /// </summary>
    public List<DocFunction> methods { get; init; } = [];
}

/// <summary>
///     表示一个可解析的类型引用目标
/// </summary>
/// <param name="project">所属项目名称</param>
/// <param name="module">所属模块命名空间</param>
/// <param name="typeName">类型短名称</param>
public sealed record ResolvedTypeRef(string project, string module, string typeName);

/// <summary>
///     Valkyrie 源文件文档生成器，解析 .v 文件并提取文档信息
/// </summary>
public sealed class LegionDocGenerator
{
    /// <summary>
    ///     从多个 DocProject 构建全局类型索引（用于 workspace 模式）
    /// </summary>
    /// <param name="projects">所有成员项目</param>
    /// <returns>全限定名 → 类型引用信息的映射</returns>
    public static Dictionary<string, ResolvedTypeRef> build_type_index(IReadOnlyList<DocProject> projects)
    {
        var index = new Dictionary<string, ResolvedTypeRef>(StringComparer.Ordinal);

        foreach (var project in projects)
        {
            foreach (var module in project.modules)
            {
                foreach (var type in module.types)
                {
                    var fqn = string.IsNullOrEmpty(module.namespace_name)
                        ? type.name
                        : $"{module.namespace_name}.{type.name}";

                    index[fqn] = new ResolvedTypeRef(project.name, module.namespace_name, type.name);
                }
            }
        }

        return index;
    }

    /// <summary>
    ///     从单个 DocProject 构建本地类型索引（用于单项目/包模式）
    /// </summary>
    /// <param name="project">单个项目</param>
    /// <returns>全限定名 → 类型引用信息的映射</returns>
    public static Dictionary<string, ResolvedTypeRef> build_type_index(DocProject project)
    {
        return build_type_index([project]);
    }
    /// <summary>
    ///     从项目目录生成文档数据模型
    /// </summary>
    /// <param name="projectDir">项目根目录路径</param>
    /// <returns>解析后的 DocProject</returns>
    public DocProject generate(string projectDir)
    {
        var manifest = try_load_manifest(projectDir);
        var projectName = manifest?.name ?? Path.GetFileName(projectDir);
        var projectDescription = manifest?.description ?? string.Empty;

        var docProject = new DocProject
        {
            name = projectName,
            description = projectDescription,
            modules = []
        };

        var sourceDir = Path.Combine(projectDir, "source");
        if (!Directory.Exists(sourceDir))
        {
            return docProject;
        }

        var moduleMap = new Dictionary<string, DocModule>(StringComparer.Ordinal);

        var vFiles = Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories);

        foreach (var file in vFiles)
        {
            try
            {
                var source = File.ReadAllText(file);

                // 预处理：移除 parser 暂不支持的 [tag(...)] 注解行
                source = System.Text.RegularExpressions.Regex.Replace(
                    source,
                    @"^\s*\[tag\([^)]*\)\]\s*$",
                    string.Empty,
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                var diagnostics = new DiagnosticSink();
                var lexer = new ValkyrieLexer(diagnostics);
                var tokens = lexer.tokenize(source);

                if (diagnostics.has_errors)
                {
                    Console.Error.WriteLine($"lexer 错误: {file}");
                    continue;
                }

                var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
                var unit = parser.parse(tokens);

                if (diagnostics.has_errors || unit is not ProgramRoot programRoot)
                {
                    if (diagnostics.has_errors)
                    {
                        Console.Error.WriteLine($"parser 错误: {file}");
                        foreach (var diag in diagnostics.messages)
                        {
                            Console.Error.WriteLine($"  {diag}");
                        }
                    }
                    continue;
                }

                collect_declarations(programRoot, file, source, moduleMap);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"解析异常: {file}: {ex.Message}");
            }
        }

        return docProject with
        {
            modules =
            [
                .. moduleMap.Values
                    .OrderBy(m => m.namespace_name, StringComparer.Ordinal)
            ]
        };
    }

    /// <summary>
    ///     尝试加载 legion.von 清单
    /// </summary>
    private static LegionManifest? try_load_manifest(string projectDir)
    {
        try
        {
            var diagnostics = new DiagnosticSink();
            var vonParser = new VonParser(diagnostics);
            return LegionManifest.load(projectDir, content => vonParser.deserialize(content));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     遍历 AST 声明列表并收集文档信息
    /// </summary>
    private static void collect_declarations(ProgramRoot programRoot, string filePath, string sourceText, Dictionary<string, DocModule> moduleMap)
    {
        var currentNamespace = string.Empty;

        foreach (var declaration in programRoot.declarations)
        {
            if (declaration is DeclareNamespace namespaceDecl)
            {
                var nsName = namespaceDecl.name.name;

                if (namespaceDecl.declarations.Count > 0)
                {
                    collect_namespace_declarations(namespaceDecl.declarations, nsName, filePath, moduleMap);
                }
                else
                {
                    currentNamespace = nsName;
                }

                continue;
            }

            collect_top_level_declaration(declaration, currentNamespace, filePath, moduleMap);
        }
    }

    /// <summary>
    ///     收集命名空间内的所有声明
    /// </summary>
    private static void collect_namespace_declarations(IReadOnlyList<ValkyrieNode> declarations, string nsName, string filePath, Dictionary<string, DocModule> moduleMap)
    {
        foreach (var declaration in declarations)
        {
            collect_top_level_declaration(declaration, nsName, filePath, moduleMap);
        }
    }

    /// <summary>
    ///     收集单个顶层声明（函数或类型）
    /// </summary>
    private static void collect_top_level_declaration(ValkyrieNode declaration, string nsName, string filePath, Dictionary<string, DocModule> moduleMap)
    {
        var module = get_or_create_module(moduleMap, nsName);

        switch (declaration)
        {
            case DeclareMicro micro:
                module.functions.Add(create_doc_function(micro, filePath));
                break;

            case DeclareMezzo mezzo:
                module.functions.Add(create_doc_function_from_mezzo(mezzo, filePath));
                break;

            case DeclareClass classDecl:
                module.types.Add(create_doc_type(classDecl, "class", classDecl.type_parameters, classDecl.body, filePath));
                break;

            case DeclareStructure structDecl:
                module.types.Add(create_doc_type(structDecl, "structure", structDecl.type_parameters, structDecl.body, filePath));
                break;

            case DeclareTrait traitDecl:
                module.types.Add(create_doc_type(traitDecl, "trait", traitDecl.type_parameters, traitDecl.body, filePath));
                break;

            case DeclareUnite uniteDecl:
            {
                var docType = new DocType
                {
                    name = uniteDecl.name?.name ?? string.Empty,
                    kind = "unite",
                    doc_comment = uniteDecl.annotations.document_text(),
                    generic_params = collect_generic_params(uniteDecl.type_parameters),
                    fields =
                    [
                        .. uniteDecl.variants.Select(v => new DocField
                        {
                            name = v.name?.name ?? string.Empty,
                            field_type = v.body?.fields.Count > 0 ? "variant" : "unit",
                            doc_comment = v.annotations.document_text()
                        })
                    ],
                    methods =
                    [
                        .. uniteDecl.methods.Select(m => create_doc_function_from_method(m, filePath,
                            build_full_type_name(uniteDecl.name?.name ?? string.Empty, uniteDecl.type_parameters)))
                    ],
                    source_file = filePath
                };
                module.types.Add(docType);
                break;
            }

            case UnionDecl unionDecl:
            {
                var docType = new DocType
                {
                    name = unionDecl.name?.name ?? string.Empty,
                    kind = "union",
                    doc_comment = unionDecl.annotations.document_text(),
                    generic_params = [],
                    fields =
                    [
                        .. unionDecl.variants.Select(v => new DocField
                        {
                            name = v.name,
                            field_type = v.fields.Count > 0
                                ? "{" + string.Join(", ",
                                    v.fields.Select(f => $"{f.name}: {type_node_to_string(f.field_type)}")) + "}"
                                : "unit",
                            doc_comment = string.Empty
                        })
                    ],
                    methods = [],
                    source_file = filePath
                };
                module.types.Add(docType);
                break;
            }

            case DeclareEnums enumsDecl:
            {
                var docType = new DocType
                {
                    name = enumsDecl.name?.name ?? string.Empty,
                    kind = "enums",
                    doc_comment = enumsDecl.annotations.document_text(),
                    generic_params = [],
                    fields =
                    [
                        .. enumsDecl.members.Select(m => new DocField
                        {
                            name = m.name?.name ?? string.Empty,
                            field_type = "enum member",
                            doc_comment = m.annotations.document_text()
                        })
                    ],
                    methods = [],
                    source_file = filePath
                };
                module.types.Add(docType);
                break;
            }

            case DeclareFlags flagsDecl:
            {
                var docType = new DocType
                {
                    name = flagsDecl.name?.name ?? string.Empty,
                    kind = "flags",
                    doc_comment = flagsDecl.annotations.document_text(),
                    generic_params = [],
                    fields =
                    [
                        .. flagsDecl.members.Select(m => new DocField
                        {
                            name = m.name?.name ?? string.Empty,
                            field_type = "flag member",
                            doc_comment = m.annotations.document_text()
                        })
                    ],
                    methods = [],
                    source_file = filePath
                };
                module.types.Add(docType);
                break;
            }

            case DeclareImply implyDecl:
            {
                var targetName = type_node_to_string(implyDecl.target_type);
                var targetBaseName = extract_type_base_name(implyDecl.target_type);
                var contractName = implyDecl.contract_type is not null ? type_node_to_string(implyDecl.contract_type) : string.Empty;
                var imply = new DocImply
                {
                    target_type = targetName,
                    contract_type = contractName,
                    methods =
                    [
                        .. implyDecl.methods.Select(m => create_doc_function_from_method(m, filePath, targetName))
                    ]
                };

                var targetType = get_or_create_target_type(module, targetBaseName, filePath);
                targetType.imply_blocks.Add(imply);
                break;
            }
        }
    }

    /// <summary>
    ///     获取或创建模块
    /// </summary>
    private static DocModule get_or_create_module(Dictionary<string, DocModule> moduleMap, string nsName)
    {
        if (!moduleMap.TryGetValue(nsName, out var module))
        {
            module = new DocModule { namespace_name = nsName };
            moduleMap[nsName] = module;
        }

        return module;
    }

    /// <summary>
    ///     获取或创建一个占位类型（用于 imply 块的目标类型）
    /// </summary>
    private static DocType get_or_create_target_type(DocModule module, string typeName, string filePath)
    {
        var existing = module.types.FirstOrDefault(t => t.name == typeName);
        if (existing is not null)
        {
            return existing;
        }

        var docType = new DocType
        {
            name = typeName,
            kind = "external",
            source_file = filePath
        };
        module.types.Add(docType);
        return docType;
    }

    /// <summary>
    ///     从 DeclareMicro 创建 DocFunction
    /// </summary>
    private static DocFunction create_doc_function(DeclareMicro micro, string filePath)
    {
        var name = micro.name?.name ?? string.Empty;
        var returnType = micro.return_type is not null ? type_node_to_string(micro.return_type) : "auto";
        var modifiers = micro.annotations.modifier_texts();
        var parameters = collect_parameters(micro.parameters, modifiers: modifiers);
        var sig = build_function_signature(name, parameters, returnType, collect_generic_params(micro.type_parameters));

        return new DocFunction
        {
            name = name,
            signature = sig,
            doc_comment = micro.annotations.document_text(),
            return_type = returnType,
            parameters = parameters,
            source_file = filePath
        };
    }

    /// <summary>
    ///     从 DeclareMezzo 创建 DocFunction
    /// </summary>
    private static DocFunction create_doc_function_from_mezzo(DeclareMezzo mezzo, string filePath)
    {
        var name = mezzo.name?.name ?? string.Empty;

        return new DocFunction
        {
            name = name,
            signature = $"mezzo {name}",
            doc_comment = mezzo.annotations.document_text(),
            return_type = string.Empty,
            parameters = [],
            source_file = filePath
        };
    }

    /// <summary>
    ///     从 DeclareObjectMethod 创建 DocFunction
    /// </summary>
    private static DocFunction create_doc_function_from_method(DeclareObjectMethod method, string filePath, string? targetType = null)
    {
        var name = method.name?.name ?? string.Empty;
        var returnType = method.return_type is not null ? type_node_to_string(method.return_type) : "auto";
        var modifiers = method.annotations.modifier_texts();
        var parameters = collect_parameters(method.parameters, targetType, modifiers);
        var sig = build_function_signature(name, parameters, returnType, []);

        return new DocFunction
        {
            name = name,
            signature = sig,
            doc_comment = method.annotations.document_text(),
            return_type = returnType,
            parameters = parameters,
            source_file = filePath
        };
    }

    /// <summary>
    ///     创建通用类型的 DocType
    /// </summary>
    private static DocType create_doc_type(IDeclarationNode decl, string kind, IReadOnlyList<TypeParameterList> typeParameters, ObjectBody? body, string filePath)
    {
        return new DocType
        {
            name = decl.name?.name ?? string.Empty,
            kind = kind,
            doc_comment = decl.annotations.document_text(),
            generic_params = collect_generic_params(typeParameters),
            fields = body?.fields.Select(f => new DocField
            {
                name = f.name,
                field_type = type_node_to_string(f.field_type),
                doc_comment = f.annotations.document_text()
            }).ToList() ?? [],
            methods = body?.methods.Select(m => create_doc_function_from_method(m, filePath, build_full_type_name(decl.name?.name ?? string.Empty, typeParameters))).ToList() ?? [],
            source_file = filePath
        };
    }

    /// <summary>
    ///     收集泛型参数名称
    /// </summary>
    private static List<string> collect_generic_params(IReadOnlyList<TypeParameterList> typeParameters)
    {
        var names = new List<string>();
        foreach (var tpList in typeParameters)
        {
            foreach (var item in tpList.items)
            {
                var name = item.name?.name;
                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    /// <summary>
    ///     构建包含泛型参数的完整类型名称
    /// </summary>
    /// <param name="baseName">基础类型名</param>
    /// <param name="typeParameters">泛型参数列表</param>
    /// <returns>完整类型名，如 "IndexSet&lt;T&gt;" 或 "Option&lt;T&gt;"</returns>
    private static string build_full_type_name(string baseName, IReadOnlyList<TypeParameterList> typeParameters)
    {
        var genericParams = collect_generic_params(typeParameters);
        if (genericParams.Count == 0)
        {
            return baseName;
        }

        return $"{baseName}<{string.Join(", ", genericParams)}>";
    }

    /// <summary>
    ///     收集函数参数列表
    /// </summary>
    /// <param name="parameterLists">参数列表（每个 TermParameterList 可能只含单个参数）</param>
    /// <param name="targetType">目标类型（用于解析 self 参数类型），null 表示不解析</param>
    /// <param name="modifiers">函数修饰符列表（如 mut、ref、own），用于合并到 self 参数</param>
    private static List<DocParam> collect_parameters(IReadOnlyList<TermParameterList> parameterLists, string? targetType = null, IReadOnlyList<string>? modifiers = null)
    {
        // 先展平所有参数为 (name, type) 对
        var flatParams = new List<(string name, string type)>();
        foreach (var paramList in parameterLists)
        {
            foreach (var item in paramList.items)
            {
                var name = item.name?.name ?? string.Empty;
                var type = item.bound_type is not null ? type_node_to_string(item.bound_type) : string.Empty;
                flatParams.Add((name, type));
            }
        }

        // 检查 annotations 修饰符中有没有 self 限定符（备用方案），如果展平列表已处理则忽略
        var annotationSelfMod = extract_self_modifier(modifiers);

        var result = new List<DocParam>();
        for (var i = 0; i < flatParams.Count; i++)
        {
            var (paramName, paramType) = flatParams[i];

            // 处理 mut/ref/own + self 模式（跨 TermParameterList）
            if (is_self_qualifier(paramName) && i + 1 < flatParams.Count)
            {
                var (nextName, _) = flatParams[i + 1];
                if (nextName is "self" or "Self")
                {
                    var resolvedType = targetType ?? "any";
                    result.Add(new DocParam
                    {
                        name = $"{paramName} {nextName}",
                        param_type = resolvedType
                    });
                    i++; // 跳过 self
                    annotationSelfMod = null; // 已处理，清除备用修饰符
                    continue;
                }
            }

            // 处理单独的 self/Self 参数
            if (paramName is "self" or "Self")
            {
                var resolvedType = targetType ?? "any";
                var displayName = annotationSelfMod is not null ? $"{annotationSelfMod} {paramName}" : paramName;
                result.Add(new DocParam
                {
                    name = displayName,
                    param_type = resolvedType
                });
                annotationSelfMod = null; // 已消费
                continue;
            }

            var finalType = string.IsNullOrEmpty(paramType) ? "any" : paramType;
            result.Add(new DocParam
            {
                name = paramName,
                param_type = finalType
            });
        }

        return result;
    }

    /// <summary>
    ///     判断参数名是否为 self 限定符
    /// </summary>
    private static bool is_self_qualifier(string name)
    {
        return name is "mut" or "ref" or "own";
    }

    /// <summary>
    ///     从修饰符列表中提取 self 相关的修饰符
    /// </summary>
    /// <param name="modifiers">修饰符列表</param>
    /// <returns>合并后的修饰符字符串（如 "mut"），没有则返回 null</returns>
    private static string? extract_self_modifier(IReadOnlyList<string>? modifiers)
    {
        if (modifiers is null || modifiers.Count == 0)
        {
            return null;
        }

        var selfMods = new List<string>();
        foreach (var mod in modifiers)
        {
            if (is_self_qualifier(mod))
            {
                selfMods.Add(mod);
            }
        }

        return selfMods.Count > 0 ? string.Join(" ", selfMods) : null;
    }

    /// <summary>
    ///     构建函数签名字符串
    /// </summary>
    private static string build_function_signature(string name, List<DocParam> parameters, string returnType, List<string> genericParams)
    {
        var genericPart = genericParams.Count > 0 ? $"<{string.Join(", ", genericParams)}>" : string.Empty;
        var paramPart = string.Join(", ", parameters.Select(p => $"{p.name}: {p.param_type}"));
        return $"micro {name}{genericPart}({paramPart}): {returnType}";
    }

    /// <summary>
    ///     将 TypeNode 转换为字符串表示
    /// </summary>
    private static string type_node_to_string(TypeNode typeNode)
    {
        switch (typeNode)
        {
            case TypeLiteralNamePathNode literalNode:
                return literalNode.path.full_name;

            case TypeMicroNode microNode:
            {
                var paramStr = type_node_to_string(microNode.parameter_type);
                var returnStr = type_node_to_string(microNode.return_type);
                return $"micro({paramStr}) -> {returnStr}";
            }

            case TypeExpressionUnaryNode unaryNode when unaryNode.@operator == TypeUnaryOperator.nullable:
                return $"{type_node_to_string(unaryNode.operand)}?";

            case TypeExpressionUnaryNode unaryNode:
                return $"{unaryNode.@operator}({type_node_to_string(unaryNode.operand)})";

            case TypeExpressionBinaryNode binaryNode when binaryNode.@operator == TypeBinaryOperator.product:
                return render_product_type(binaryNode);

            case TypeExpressionBinaryNode binaryNode:
            {
                var left = type_node_to_string(binaryNode.lhs);
                var right = type_node_to_string(binaryNode.rhs);
                var op = binaryNode.@operator switch
                {
                    TypeBinaryOperator.or => " | ",
                    TypeBinaryOperator.and => " & ",
                    TypeBinaryOperator.difference => " - ",
                    _ => " ? "
                };
                return $"{left}{op}{right}";
            }

            case TypeLiteralArrayNode:
                return "[]";

            case TypeLiteralTupleNode tupleNode:
            {
                if (tupleNode.elements.Count == 0)
                {
                    return "()";
                }

                var renderedElements = tupleNode.elements.Select(element =>
                {
                    var elementType = type_node_to_string(element.type);
                    return element.label is null ? elementType : $"{element.label.name}: {elementType}";
                });
                var renderedTuple = string.Join(", ", renderedElements);
                if (tupleNode.elements.Count == 1)
                {
                    renderedTuple += ",";
                }

                return $"({renderedTuple})";
            }

            default:
                return typeNode.GetType().Name;
        }
    }

    /// <summary>
    ///     将 product 类型节点渲染为泛型形式 Name&lt;A, B, C&gt;
    /// </summary>
    /// <remarks>
    ///     product 在 AST 中是左结合二叉树：product(product(Name, Arg1), Arg2)。
    ///     第一个元素是类型构造器，后续元素是泛型实参。
    ///     需要先展平再渲染为 Name&lt;Arg1, Arg2&gt;。
    /// </remarks>
    private static string render_product_type(TypeExpressionBinaryNode node)
    {
        var flat = flatten_product(node);
        var constructor = type_node_to_string(flat[0]);

        if (flat.Count == 1)
        {
            return constructor;
        }

        var args = string.Join(", ", flat.Skip(1).Select(type_node_to_string));
        return $"{constructor}<{args}>";
    }

    /// <summary>
    ///     展平 product 二叉树为列表
    /// </summary>
    private static List<TypeNode> flatten_product(TypeNode node)
    {
        var result = new List<TypeNode>();

        void flatten(TypeNode n)
        {
            if (n is TypeExpressionBinaryNode bin && bin.@operator == TypeBinaryOperator.product)
            {
                flatten(bin.lhs);
                flatten(bin.rhs);
            }
            else
            {
                result.Add(n);
            }
        }

        flatten(node);
        return result;
    }

    /// <summary>
    ///     提取类型的基础名称（去掉泛型参数），用于 imply 块目标类型匹配
    /// </summary>
    private static string extract_type_base_name(TypeNode typeNode)
    {
        if (typeNode is TypeExpressionBinaryNode bin && bin.@operator == TypeBinaryOperator.product)
        {
            var flat = flatten_product(bin);
            return type_node_to_string(flat[0]);
        }

        return type_node_to_string(typeNode);
    }
}
