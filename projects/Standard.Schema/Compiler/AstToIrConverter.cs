namespace Hermes.Compiler;

public sealed class AstToIrConverter
{
    private string? _sourceText;

    public SchemaIR Convert(CompilationUnit ast, string? filePath, string? sourceText = null)
    {
        _sourceText = sourceText;
        var @namespace = string.Empty;
        var namespaceIsPrimary = false;
        var namespaceAttrs = new List<AttributeDefinition>();
        var usings = new List<UsingEntry>();
        var classes = new List<ClassDefinition>();
        var structures = new List<StructureDefinition>();
        var enums = new List<EnumDefinition>();
        var flags = new List<FlagsDefinition>();
        var unions = new List<UnionDefinition>();
        var storages = new List<StorageDefinition>();
        var services = new List<ServiceDefinition>();
        var micros = new List<MicroDefinition>();

        foreach (var decl in ast.Declarations)
            if (decl is NamespaceDecl ns)
            {
                @namespace = ns.Name;
                namespaceIsPrimary = ns.IsPrimary;
                namespaceAttrs = ns.Attributes.Select(ConvertAttributeDecl).ToList();
            }
            else if (decl is UsingDecl use)
            {
                usings.Add(ConvertUsingDecl(use));
            }

        var topLevelModels = new List<ModelDefinition>();
        var topLevelStreams = new List<StreamDefinition>();
        var topLevelCaches = new List<CacheDefinition>();
        var topLevelExtraClasses = new List<ClassDefinition>();

        foreach (var decl in ast.Declarations)
            switch (decl)
            {
                case ClassDecl c:
                    classes.Add(ConvertClassDecl(c));
                    break;
                case StructureDecl st:
                    structures.Add(ConvertStructureDecl(st));
                    break;
                case EnumDecl e:
                    enums.Add(ConvertEnumDecl(e));
                    break;
                case FlagsDecl f:
                    flags.Add(ConvertFlagsDecl(f));
                    break;
                case UnionDecl u:
                    var (unionDef, extraClasses) = ConvertUnionDecl(u);
                    unions.Add(unionDef);
                    classes.AddRange(extraClasses);
                    break;
                case StorageDecl s:
                    if (s.Name == "__top_level__")
                    {
                        foreach (var m in s.Models)
                        {
                            var (modelDef, modelExtraClasses) = ConvertSingleModel(m);
                            topLevelModels.Add(modelDef);
                            topLevelExtraClasses.AddRange(modelExtraClasses);
                        }

                        foreach (var st in s.Streams)
                        {
                            var (streamDef, streamClasses) = ConvertSingleStream(st);
                            topLevelStreams.Add(streamDef);
                            topLevelExtraClasses.AddRange(streamClasses);
                        }

                        foreach (var c in s.Caches) topLevelCaches.Add(ConvertSingleCache(c));
                    }
                    else
                    {
                        var (storageDef, storageClasses) = ConvertStorageDecl(s);
                        storages.Add(storageDef);
                        classes.AddRange(storageClasses);
                    }

                    break;
                case ServiceDecl sv:
                    services.Add(ConvertServiceDecl(sv));
                    break;
                case FunctionDecl fn:
                    micros.Add(ConvertFunctionDecl(fn));
                    break;
            }

        classes.AddRange(topLevelExtraClasses);

        if (topLevelModels.Count > 0 || topLevelStreams.Count > 0 || topLevelCaches.Count > 0)
        {
            var storageName = !string.IsNullOrEmpty(@namespace)
                ? @namespace
                : Path.GetFileNameWithoutExtension(filePath ?? "schema");
            var kind = InferStorageKindFromAttributes(namespaceAttrs);
            var entityType = new NamedType(storageName);
            var storageDef = new StorageDefinition(storageName, entityType, topLevelModels, topLevelStreams,
                topLevelCaches, namespaceAttrs, kind);

            if (namespaceIsPrimary)
                storages.Insert(0, storageDef);
            else
                storages.Add(storageDef);
        }

        return new SchemaIR(
            classes: classes,
            structures: structures,
            enums: enums,
            flags: flags,
            unions: unions,
            storages: storages,
            services: services,
            @namespace: @namespace,
            usings: usings,
            micros: micros,
            sourceFile: filePath ?? ast.FilePath,
            namespaceIsPrimary: namespaceIsPrimary);
    }

    public ClassDefinition ConvertClassDecl(ClassDecl ast)
    {
        var fields = ast.fields.Select(ConvertFieldDecl).ToList();
        var attrs = ast.Attributes.Select(ConvertAttributeDecl).ToList();
        var baseClass = ast.Inheritances.Count > 0
            ? new BaseClass(ast.Inheritances[0].BaseType.Name)
            : null;

        return new ClassDefinition(ast.Name, fields, attrs, baseClass, null, GetLineCol(ast.Span.Start).Line,
            GetLineCol(ast.Span.Start).Column);
    }

    /// <summary>
    ///     将 StructureDecl AST 节点转换为 StructureDefinition IR
    /// </summary>
    public StructureDefinition ConvertStructureDecl(StructureDecl ast)
    {
        var fields = ast.fields.Select(ConvertFieldDecl).ToList();
        var attrs = ast.Attributes.Select(ConvertAttributeDecl).ToList();

        return new StructureDefinition(ast.Name, fields, attrs, null, GetLineCol(ast.Span.Start).Line,
            GetLineCol(ast.Span.Start).Column);
    }

    public EnumDefinition ConvertEnumDecl(EnumDecl ast)
    {
        var members = new List<EnumMember>();
        var autoValue = 0;

        foreach (var member in ast.Members)
        {
            var value = ResolveEnumValue(member.Value, autoValue);
            members.Add(new EnumMember(member.Name, value));
            autoValue = value + 1;
        }

        return new EnumDefinition(ast.Name, members, null, null, GetLineCol(ast.Span.Start).Line,
            GetLineCol(ast.Span.Start).Column);
    }

    public FlagsDefinition ConvertFlagsDecl(FlagsDecl ast)
    {
        var members = new List<EnumMember>();
        var autoValue = 1;

        foreach (var member in ast.Members)
        {
            var value = ResolveEnumValue(member.Value, autoValue);
            members.Add(new EnumMember(member.Name, value));
            autoValue = value == 0 ? 1 : value << 1;
        }

        return new FlagsDefinition(ast.Name, members, null, null, GetLineCol(ast.Span.Start).Line,
            GetLineCol(ast.Span.Start).Column);
    }

    public (UnionDefinition union, IReadOnlyList<ClassDefinition> extraClasses) ConvertUnionDecl(UnionDecl ast)
    {
        var variants = new List<UnionVariant>();
        var extraClasses = new List<ClassDefinition>();

        foreach (var v in ast.Variants)
        {
            SchemaType? payload = null;

            if (v.fields.Count == 1)
            {
                payload = ConvertTypeAnnotation(v.fields[0].FieldType);
            }
            else if (v.fields.Count > 1)
            {
                var variantClassName = $"{ast.Name}.{v.Name}";
                var fields = v.fields.Select(ConvertFieldDecl).ToList();
                extraClasses.Add(new ClassDefinition(variantClassName, fields, [], null, null,
                    GetLineCol(v.Span.Start).Line, GetLineCol(v.Span.Start).Column));
                payload = new NamedType(variantClassName);
            }

            variants.Add(new UnionVariant(v.Name, payload));
        }

        var unionDef = new UnionDefinition(ast.Name, variants, null, null, GetLineCol(ast.Span.Start).Line,
            GetLineCol(ast.Span.Start).Column);
        return (unionDef, extraClasses);
    }

    public (StorageDefinition storage, IReadOnlyList<ClassDefinition> extraClasses) ConvertStorageDecl(StorageDecl ast)
    {
        var extraClasses = new List<ClassDefinition>();
        var models = new List<ModelDefinition>();
        var streams = new List<StreamDefinition>();
        var caches = new List<CacheDefinition>();

        foreach (var m in ast.Models)
        {
            var (modelDef, modelExtraClasses) = ConvertSingleModel(m);
            models.Add(modelDef);
            extraClasses.AddRange(modelExtraClasses);
        }

        foreach (var s in ast.Streams)
        {
            var (streamDef, streamClasses) = ConvertSingleStream(s);
            streams.Add(streamDef);
            extraClasses.AddRange(streamClasses);
        }

        foreach (var c in ast.Caches) caches.Add(ConvertSingleCache(c));

        var storageAttrs = ast.Attributes.Select(ConvertAttributeDecl).ToList();
        var storageKind = InferStorageKindFromAttributes(storageAttrs);
        var entityType = new NamedType(ast.Name);
        var storageDef = new StorageDefinition(ast.Name, entityType, models, streams, caches, storageAttrs, storageKind,
            GetLineCol(ast.Span.Start).Line, GetLineCol(ast.Span.Start).Column);
        return (storageDef, extraClasses);
    }

    public (ModelDefinition model, IReadOnlyList<ClassDefinition> extraClasses) ConvertSingleModel(StorageModelDecl m)
    {
        var fields = m.fields.Select(ConvertFieldDecl).ToList();
        var keyField = fields.FirstOrDefault(f =>
            f.Attributes is not null &&
            f.Attributes.Any(a => a.Name == "id" || a.Name == "Id" || a.Name == "key" || a.Name == "Key"));

        if (keyField is null && m.Attributes.Count > 0)
        {
            var modelKeyAttr = m.Attributes.FirstOrDefault(a => a.Name is "key" or "Key");
            if (modelKeyAttr is not null && modelKeyAttr.Arguments.Count > 0)
            {
                var keyFieldName = modelKeyAttr.Arguments[0].Value;
                keyField = fields.FirstOrDefault(f => f.Name == keyFieldName);
            }
        }

        var keyType = keyField?.FieldType ?? new NamedType("i32");
        var entityType = new NamedType(m.Name);
        var getters = m.Getters.Select(ConvertGetterDecl).ToList();
        var attrs = m.Attributes.Select(ConvertAttributeDecl).ToList();
        var modelDocComment = m.DocComments.Count > 0
            ? string.Join("\n", m.DocComments.Select(d => d.Content))
            : null;
        return (
            new ModelDefinition(m.Name, keyType, fields, getters, attrs, modelDocComment, GetLineCol(m.Span.Start).Line,
                GetLineCol(m.Span.Start).Column), []);
    }

    public (StreamDefinition stream, IReadOnlyList<ClassDefinition> extraClasses) ConvertSingleStream(
        StorageStreamDecl s)
    {
        var (eventType, streamClasses) = ConvertStreamFields(s.Name, s.fields);
        var attrs = s.Attributes.Select(ConvertAttributeDecl).ToList();
        return (
            new StreamDefinition(s.Name, eventType, attributes: attrs, sourceLine: GetLineCol(s.Span.Start).Line,
                sourceColumn: GetLineCol(s.Span.Start).Column), streamClasses);
    }

    public CacheDefinition ConvertSingleCache(StorageCacheDecl c)
    {
        var fields = c.fields.Select(ConvertFieldDecl).ToList();
        var keyField = fields.FirstOrDefault(f =>
            f.Attributes is not null &&
            f.Attributes.Any(a => a.Name == "id" || a.Name == "Id" || a.Name == "key" || a.Name == "Key"));
        var valueField = fields.FirstOrDefault(f => f != keyField);
        var keyType = keyField?.FieldType ?? new NamedType("utf8");
        var valueType = valueField?.FieldType ?? new NamedType("utf8");
        var attrs = c.Attributes.Select(ConvertAttributeDecl).ToList();
        return new CacheDefinition(c.Name, keyType, valueType, attrs, GetLineCol(c.Span.Start).Line,
            GetLineCol(c.Span.Start).Column);
    }

    public ServiceDefinition ConvertServiceDecl(ServiceDecl ast)
    {
        var endpoints = ast.Endpoints.Select(ConvertServiceEndpointDecl).ToList();
        var serviceAttrs = ast.Attributes.Select(ConvertAttributeDecl).ToList();

        var version = "1.0";
        var versionAttr = serviceAttrs.FirstOrDefault(a => a.Name is "version" or "Version");
        if (versionAttr is not null && versionAttr.Arguments.Count > 0) version = versionAttr.Arguments[0].Value;

        var deprecatedEndpoints = new List<string>();
        var deprecatedAttr = serviceAttrs.FirstOrDefault(a => a.Name is "deprecatedEndpoints" or "DeprecatedEndpoints");
        if (deprecatedAttr is not null)
            foreach (var arg in deprecatedAttr.Arguments)
                if (!string.IsNullOrEmpty(arg.Value))
                    deprecatedEndpoints.Add(arg.Value);

        return new ServiceDefinition(ast.Name, endpoints, serviceAttrs, version, deprecatedEndpoints,
            GetLineCol(ast.Span.Start).Line, GetLineCol(ast.Span.Start).Column);
    }

    public MicroDefinition ConvertFunctionDecl(FunctionDecl ast)
    {
        var parameters = ast.Parameters.Select(ConvertParameterDecl).ToList();
        var returnType = ast.ReturnType is not null
            ? ConvertTypeAnnotation(ast.ReturnType)
            : null;
        var attrs = ast.Attributes.Select(ConvertAttributeDecl).ToList();

        return new MicroDefinition(ast.Name, parameters, returnType, body: null, attributes: attrs,
            GetLineCol(ast.Span.Start).Line, GetLineCol(ast.Span.Start).Column);
    }

    public SchemaType ConvertTypeAnnotation(TypeAnnotation ast)
    {
        SchemaType result;

        if (ast.IsListType && ast.ElementType is not null)
        {
            result = new ListType(ConvertTypeAnnotation(ast.ElementType));
        }
        else if (ast.IsArrayType && ast.ElementType is not null && ast.ArraySize is LiteralExpr { Value: { } sizeObj })
        {
            var sizeStr = sizeObj.ToString();
            var arraySize = int.TryParse(sizeStr, out var parsed) ? parsed : 0;
            result = new ArrayType(ConvertTypeAnnotation(ast.ElementType), arraySize);
        }
        else
        {
            var primitive = PrimitiveType.FromName(ast.Name);
            if (primitive is not null)
                result = ast.GenericArgs.Count > 0
                    ? new NamedType(ast.Name)
                    : primitive;
            else
                result = ast.Name switch
                {
                    "list" when ast.GenericArgs.Count == 1 => new ListType(ConvertTypeAnnotation(ast.GenericArgs[0])),
                    "option" when ast.GenericArgs.Count == 1 => new OptionType(
                        ConvertTypeAnnotation(ast.GenericArgs[0])),
                    "result" when ast.GenericArgs.Count == 1 => new ResultType(
                        ConvertTypeAnnotation(ast.GenericArgs[0])),
                    "result" when ast.GenericArgs.Count == 2 => new ResultType(
                        ConvertTypeAnnotation(ast.GenericArgs[0]),
                        ConvertTypeAnnotation(ast.GenericArgs[1])),
                    "stream" when ast.GenericArgs.Count == 1 => new StreamType(
                        ConvertTypeAnnotation(ast.GenericArgs[0])),
                    "dict" when ast.GenericArgs.Count == 2 => new DictType(
                        ConvertTypeAnnotation(ast.GenericArgs[0]),
                        ConvertTypeAnnotation(ast.GenericArgs[1])),
                    "dict" when ast.GenericArgs.Count == 1 => new DictType(
                        PrimitiveType.Utf8,
                        ConvertTypeAnnotation(ast.GenericArgs[0])),
                    "map" when ast.GenericArgs.Count == 2 => new DictType(
                        ConvertTypeAnnotation(ast.GenericArgs[0]),
                        ConvertTypeAnnotation(ast.GenericArgs[1])),
                    "map" when ast.GenericArgs.Count == 1 => new DictType(
                        PrimitiveType.Utf8,
                        ConvertTypeAnnotation(ast.GenericArgs[0])),
                    "structure" when ast.GenericArgs.Count == 2 => new RecordType(
                        ConvertTypeAnnotation(ast.GenericArgs[0]),
                        ConvertTypeAnnotation(ast.GenericArgs[1])),
                    "&" when ast.GenericArgs.Count == 1 => new ReferenceType(ConvertTypeAnnotation(ast.GenericArgs[0])),
                    _ => new NamedType(ast.Name)
                };
        }

        if (ast.IsNullable) result = new OptionType(result);

        return result;
    }

    public FieldDefinition ConvertFieldDecl(FieldDecl ast)
    {
        var fieldType = ConvertTypeAnnotation(ast.FieldType);
        var isOptional = ast.DefaultValue is not null || ast.FieldType.IsNullable;
        object? defaultValue = null;

        if (ast.DefaultValue is LiteralExpr lit) defaultValue = lit.Value;

        var attrs = ast.Attributes.Select(ConvertAttributeDecl).ToList();
        var docComment = ast.DocComments.Count > 0
            ? string.Join("\n", ast.DocComments.Select(d => d.Content))
            : null;

        return new FieldDefinition(ast.Name, fieldType, isOptional, defaultValue, attrs, docComment,
            sourceLine: GetLineCol(ast.Span.Start).Line, sourceColumn: GetLineCol(ast.Span.Start).Column);
    }

    public AttributeDefinition ConvertAttributeDecl(AttributeDecl ast)
    {
        var args = ast.Arguments.Select(a => new KeyValuePair<string, string>(a.Key, a.Value)).ToList();
        return new AttributeDefinition(ast.Name, args);
    }

    public ParameterDefinition ConvertParameterDecl(ParameterDecl ast)
    {
        var paramType = ConvertTypeAnnotation(ast.ParamType);
        var attrs = ast.Attributes.Select(ConvertAttributeDecl).ToList();

        return new ParameterDefinition(ast.Name, paramType, attrs, GetLineCol(ast.Span.Start).Line,
            GetLineCol(ast.Span.Start).Column);
    }

    public UsingEntry ConvertUsingDecl(UsingDecl ast)
    {
        return new UsingEntry(ast.Namespace, ast.Selections, GetLineCol(ast.Span.Start).Line,
            GetLineCol(ast.Span.Start).Column);
    }

    public GetterDefinition ConvertGetterDecl(GetterDecl ast)
    {
        var returnType = ast.ReturnType is not null
            ? ConvertTypeAnnotation(ast.ReturnType)
            : null;
        var body = ExprToString(ast.Body);
        var docComment = ast.DocComments.Count > 0
            ? string.Join("\n", ast.DocComments.Select(d => d.Content))
            : null;
        return new GetterDefinition(ast.Name, returnType, body, docComment, GetLineCol(ast.Span.Start).Line,
            GetLineCol(ast.Span.Start).Column);
    }

    private string ExprToString(BlockStmt block)
    {
        var parts = new List<string>();
        foreach (var stmt in block.Statements) parts.Add(ExprToString(stmt));
        return string.Join("; ", parts);
    }

    private string ExprToString(AstNode node)
    {
        return node switch
        {
            MemberAccessExpr m => $"{ExprToString(m.Target)}.{m.MemberName}",
            IdentifierNode id => id.Name,
            LiteralExpr lit => lit.Value?.ToString() ?? "null",
            BinaryExpr bin => $"{ExprToString(bin.Left)} {bin.Operator} {ExprToString(bin.Right)}",
            TermCallExpression call =>
                $"{ExprToString(call.Callee)}({string.Join(", ", call.Arguments.Select(ExprToString))})",
            _ => node.ToString() ?? ""
        };
    }

    private EndpointDefinition ConvertServiceEndpointDecl(ServiceEndpointDecl ast)
    {
        var returnType = ast.ResponseType is not null
            ? ConvertTypeAnnotation(ast.ResponseType)
            : null;
        var attrs = ast.Attributes.Select(ConvertAttributeDecl).ToList();

        var requestType = ast.RequestType is not null
            ? ConvertTypeAnnotation(ast.RequestType)
            : null;

        var isInputStream = requestType is StreamType;
        var isOutputStream = returnType is StreamType;

        var streamingMode = (isInputStream, isOutputStream) switch
        {
            (false, false) => StreamingMode.Unary,
            (false, true) => StreamingMode.Server,
            (true, false) => StreamingMode.Client,
            (true, true) => StreamingMode.Duplex
        };

        var actualRequestType = isInputStream ? ((StreamType)requestType!).InnerType : requestType;
        var actualReturnType = isOutputStream ? ((StreamType)returnType!).InnerType : returnType;

        var parameters = new List<ParameterDefinition>();
        if (actualRequestType != null) parameters.Add(new ParameterDefinition("request", actualRequestType, []));

        string? pathFromAttr = null;
        var pathAttr = ast.Attributes.FirstOrDefault(a => a.Name == "path");
        if (pathAttr is not null && pathAttr.Arguments.Count > 0) pathFromAttr = pathAttr.Arguments[0].Value;

        var line = GetLineCol(ast.Span.Start).Line;
        var col = GetLineCol(ast.Span.Start).Column;

        var isDeprecated = false;
        string? deprecatedMessage = null;
        string? replaceWith = null;

        var deprecatedAttr = attrs.FirstOrDefault(a => a.Name is "deprecated" or "Deprecated");
        if (deprecatedAttr is not null)
        {
            isDeprecated = true;
            var msgArg = deprecatedAttr.Arguments.FirstOrDefault(a => a.Key is "message" or "Message");
            if (msgArg.Value is not null)
                deprecatedMessage = msgArg.Value;
            else if (deprecatedAttr.Arguments.Count > 0) deprecatedMessage = deprecatedAttr.Arguments[0].Value;

            var replaceArg = deprecatedAttr.Arguments.FirstOrDefault(a => a.Key is "replaceWith" or "ReplaceWith");
            if (replaceArg.Value is not null) replaceWith = replaceArg.Value;
        }

        return ast.Method switch
        {
            "get" => new HttpEndpoint(ast.Name, parameters, actualReturnType, "GET",
                path: ast.Path ?? pathFromAttr ?? $"/{ast.Name}", isJson: true, attributes: attrs,
                streamingMode: streamingMode, isDeprecated: isDeprecated, deprecatedMessage: deprecatedMessage,
                replaceWith: replaceWith, sourceLine: line, sourceColumn: col),
            "post" => new HttpEndpoint(ast.Name, parameters, actualReturnType, "POST",
                path: ast.Path ?? pathFromAttr ?? $"/{ast.Name}", isJson: true, attributes: attrs,
                streamingMode: streamingMode, isDeprecated: isDeprecated, deprecatedMessage: deprecatedMessage,
                replaceWith: replaceWith, sourceLine: line, sourceColumn: col),
            "put" => new HttpEndpoint(ast.Name, parameters, actualReturnType, "PUT",
                path: ast.Path ?? pathFromAttr ?? $"/{ast.Name}", isJson: true, attributes: attrs,
                streamingMode: streamingMode, isDeprecated: isDeprecated, deprecatedMessage: deprecatedMessage,
                replaceWith: replaceWith, sourceLine: line, sourceColumn: col),
            "delete" => new HttpEndpoint(ast.Name, parameters, actualReturnType, "DELETE",
                path: ast.Path ?? pathFromAttr ?? $"/{ast.Name}", isJson: true, attributes: attrs,
                streamingMode: streamingMode, isDeprecated: isDeprecated, deprecatedMessage: deprecatedMessage,
                replaceWith: replaceWith, sourceLine: line, sourceColumn: col),
            "grpc" => new GrpcEndpoint(ast.Name, parameters, actualReturnType, ast.Name, ast.Name, attributes: attrs,
                streamingMode: streamingMode, isDeprecated: isDeprecated, deprecatedMessage: deprecatedMessage,
                replaceWith: replaceWith, sourceLine: line, sourceColumn: col),
            "ws" => new WsEndpoint(ast.Name, parameters, actualReturnType, path: ast.Path ?? $"/ws/{ast.Name}",
                attributes: attrs, streamingMode: streamingMode, isDeprecated: isDeprecated,
                deprecatedMessage: deprecatedMessage, replaceWith: replaceWith, sourceLine: line, sourceColumn: col),
            "message" => new MessageEndpoint(ast.Name, parameters, actualReturnType, ast.Name, attributes: attrs,
                streamingMode: streamingMode, isDeprecated: isDeprecated, deprecatedMessage: deprecatedMessage,
                replaceWith: replaceWith, sourceLine: line, sourceColumn: col),
            _ => new HttpEndpoint(ast.Name, parameters, actualReturnType, ast.Method.ToUpperInvariant(),
                path: ast.Path ?? pathFromAttr ?? $"/{ast.Name}", isJson: true, attributes: attrs,
                streamingMode: streamingMode, isDeprecated: isDeprecated, deprecatedMessage: deprecatedMessage,
                replaceWith: replaceWith, sourceLine: line, sourceColumn: col)
        };
    }

    private (SchemaType eventType, IReadOnlyList<ClassDefinition> extraClasses) ConvertStreamFields(
        string streamName, IReadOnlyList<FieldDecl> fields)
    {
        if (fields.Count > 0)
        {
            var eventTypeName = $"{streamName}Event";
            var convertedFields = fields.Select(ConvertFieldDecl).ToList();
            var extraClass = new ClassDefinition(eventTypeName, convertedFields, []);
            return (new NamedType(eventTypeName), [extraClass]);
        }

        return (new NamedType("unit"), []);
    }

    private int ResolveEnumValue(AstNode? valueNode, int autoValue)
    {
        if (valueNode is null) return autoValue;

        if (valueNode is LiteralExpr lit && lit.LiteralKind == LiteralType.Number && lit.Value is not null)
            if (int.TryParse(lit.Value.ToString(), out var parsed))
                return parsed;

        return autoValue;
    }

    /// <summary>
    ///     从属性列表推断存储种类
    /// </summary>
    private static StorageKind InferStorageKindFromAttributes(IReadOnlyList<AttributeDefinition> attrs)
    {
        foreach (var attr in attrs)
            if (attr.Name is "kind" or "storage")
            {
                var value = attr.Arguments.FirstOrDefault().Value;
                if (value is "cache") return StorageKind.Cache;

                if (value is "storage" or "document" or "nosql") return StorageKind.Storage;

                if (value is "stream" or "eventsourcing" or "es" or "message_queue") return StorageKind.Stream;

                if (value is "database" or "relational" or "sql") return StorageKind.Database;
            }

        return StorageKind.Database;
    }

    private (int Line, int Column) GetLineCol(int offset)
    {
        if (string.IsNullOrEmpty(_sourceText) || offset < 0) return (0, 0);

        var line = 1;
        var column = 1;

        for (var i = 0; i < offset && i < _sourceText.Length; i++)
            if (_sourceText[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }

        return (line, column);
    }
}