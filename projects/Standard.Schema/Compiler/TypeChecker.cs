namespace Hermes.Compiler;

/// <summary>
///     Hermes Schema 类型检查器，负责语义分析、类型验证、循环引用检测和注解校验
/// </summary>
public sealed class TypeChecker
{
    private string? _sourceFile;
    private Dictionary<string, HashSet<string>> _typeFieldReferences = new(StringComparer.Ordinal);
    private HashSet<string> _typeNames = [];
    private HashSet<string> _usingNamespaces = [];

    /// <summary>
    ///     对 SchemaIR 执行完整的语义检查
    /// </summary>
    /// <param name="ir">待检查的 Schema IR</param>
    /// <param name="diagnostics">诊断收集器</param>
    /// <returns>是否有错误</returns>
    public bool Check(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        _sourceFile = ir.SourceFile;
        _typeNames = CollectTypeNames(ir);
        _usingNamespaces = CollectUsingNamespaces(ir);
        _typeFieldReferences = CollectTypeFieldReferences(ir);

        var hasErrors = false;

        hasErrors |= CheckFieldNameConflicts(ir, diagnostics);
        hasErrors |= CheckNamedTypeReferences(ir, diagnostics);
        hasErrors |= CheckCircularTypeReferences(ir, diagnostics);
        hasErrors |= CheckCircularUsings(ir, diagnostics);
        hasErrors |= CheckStorageModelReferences(ir, diagnostics);
        hasErrors |= CheckServiceEndpointReturnTypes(ir, diagnostics);
        hasErrors |= CheckAnnotationValidity(ir, diagnostics);
        hasErrors |= CheckInheritanceValidity(ir, diagnostics);
        hasErrors |= CheckDeprecatedEndpointValidity(ir, diagnostics);
        hasErrors |= CheckVersionAttributeValidity(ir, diagnostics);

        return !hasErrors;
    }

    #region 字段冲突检测

    /// <summary>
    ///     检测同一实体定义中的字段名冲突
    /// </summary>
    private bool CheckFieldNameConflicts(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var hasErrors = false;

        foreach (var cls in ir.Classes)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in cls.fields)
                if (!seen.Add(field.Name))
                {
                    diagnostics.AddError(
                        _sourceFile,
                        field.SourceLine,
                        field.SourceColumn,
                        "HER2005",
                        $"类 '{cls.Name}' 中存在重复字段名 '{field.Name}'");
                    hasErrors = true;
                }
        }

        foreach (var storage in ir.Storages)
        foreach (var model in storage.Models)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in model.fields)
                if (!seen.Add(field.Name))
                {
                    diagnostics.AddError(
                        _sourceFile,
                        field.SourceLine,
                        field.SourceColumn,
                        "HER2006",
                        $"存储模型 '{model.Name}' 中存在重复字段名 '{field.Name}'");
                    hasErrors = true;
                }
        }

        return hasErrors;
    }

    #endregion

    #region 循环类型引用检测

    /// <summary>
    ///     使用 DFS 检测类型字段引用图中是否存在循环引用
    /// </summary>
    private bool CheckCircularTypeReferences(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var whiteSet = new HashSet<string>(StringComparer.Ordinal); // 未访问
        var graySet = new HashSet<string>(StringComparer.Ordinal); // 正在访问（栈内）
        var blackSet = new HashSet<string>(StringComparer.Ordinal); // 已完成
        var path = new Stack<string>();

        foreach (var typeName in _typeFieldReferences.Keys) whiteSet.Add(typeName);

        while (whiteSet.Count > 0)
        {
            var start = whiteSet.First();
            if (!DFS(start, whiteSet, graySet, blackSet, path))
            {
                var cycle = string.Join(" → ", path.Reverse());
                diagnostics.AddError(
                    _sourceFile,
                    0, 0,
                    "HER2007",
                    $"检测到循环类型引用：{cycle}。请移除其中一个方向的引用以打破循环");
                return true;
            }
        }

        return false;

        bool DFS(string node, HashSet<string> white, HashSet<string> gray, HashSet<string> black,
            Stack<string> currentPath)
        {
            if (black.Contains(node)) return true;

            if (gray.Contains(node))
            {
                currentPath.Push(node);
                return false;
            }

            white.Remove(node);
            gray.Add(node);

            if (_typeFieldReferences.TryGetValue(node, out var refs))
                foreach (var neighbor in refs)
                    if (_typeFieldReferences.ContainsKey(neighbor))
                    {
                        currentPath.Push(node);
                        if (!DFS(neighbor, white, gray, black, currentPath)) return false;
                    }

            gray.Remove(node);
            black.Add(node);
            if (currentPath.Count > 0) currentPath.Pop();

            return true;
        }
    }

    #endregion

    #region Using 循环检测

    private bool CheckCircularUsings(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var hasErrors = false;

        if (string.IsNullOrEmpty(ir.Namespace)) return false;

        foreach (var usingEntry in ir.Usings)
            if (string.Equals(usingEntry.Namespace, ir.Namespace, StringComparison.Ordinal))
            {
                diagnostics.AddError(
                    _sourceFile,
                    usingEntry.SourceLine,
                    usingEntry.SourceColumn,
                    "HER2002",
                    $"循环 using 依赖：命名空间 '{ir.Namespace}' 引用了自身，请移除此 using 声明");

                hasErrors = true;
            }

        return hasErrors;
    }

    #endregion

    #region 存储模型引用验证

    private bool CheckStorageModelReferences(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var hasErrors = false;

        foreach (var storage in ir.Storages)
        foreach (var model in storage.Models)
            if (model.KeyType is NamedType namedKey)
                if (!IsKnownType(namedKey))
                {
                    diagnostics.AddError(
                        _sourceFile,
                        model.SourceLine,
                        model.SourceColumn,
                        "HER2003",
                        $"存储 '{storage.Name}' 的模型 '{model.Name}' 使用了未定义的键类型 '{namedKey.Name}'");

                    hasErrors = true;
                }

        return hasErrors;
    }

    #endregion

    #region 服务端点验证

    private bool CheckServiceEndpointReturnTypes(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var hasErrors = false;

        foreach (var service in ir.Services)
        foreach (var endpoint in service.Endpoints ?? [])
            if (endpoint.ReturnType is NamedType namedReturn)
                if (!IsKnownType(namedReturn))
                {
                    diagnostics.AddError(
                        _sourceFile,
                        endpoint.SourceLine,
                        endpoint.SourceColumn,
                        "HER2004",
                        $"服务 '{service.Name}' 的端点 '{endpoint.Name}' 使用了未定义的返回类型 '{namedReturn.Name}'");

                    hasErrors = true;
                }

        return hasErrors;
    }

    #endregion

    #region 继承验证

    /// <summary>
    ///     验证继承链的完整性——基类是否存在、是否存在循环继承
    /// </summary>
    private bool CheckInheritanceValidity(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var hasErrors = false;
        var classMap = new Dictionary<string, ClassDefinition>(StringComparer.Ordinal);

        foreach (var cls in ir.Classes) classMap[cls.Name] = cls;

        foreach (var cls in ir.Classes)
        {
            if (cls.Base is null) continue;

            if (!classMap.ContainsKey(cls.Base.Name) && !_typeNames.Contains(cls.Base.Name))
            {
                diagnostics.AddError(
                    _sourceFile,
                    cls.SourceLine,
                    cls.SourceColumn,
                    "HER2011",
                    $"类 '{cls.Name}' 继承的基类 '{cls.Base.Name}' 未定义，请确保基类已在当前文件或 using 的命名空间中声明");
                hasErrors = true;
            }
        }

        var whiteSet = new HashSet<string>(StringComparer.Ordinal);
        var graySet = new HashSet<string>(StringComparer.Ordinal);
        var blackSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var cls in ir.Classes)
            if (cls.Base is not null)
                whiteSet.Add(cls.Name);

        var path = new Stack<(string name, int line, int column)>();

        foreach (var start in whiteSet.ToList())
        {
            if (DFSInheritance(start, classMap, whiteSet, graySet, blackSet, path)) continue;

            var cycle = string.Join(" → ", path.Reverse().Select(p => p.name));
            var lastNode = path.Peek();
            diagnostics.AddError(
                _sourceFile,
                lastNode.line,
                lastNode.column,
                "HER2012",
                $"检测到循环继承：{cycle}。请移除了某个继承声明以打破循环");
            hasErrors = true;
            break;
        }

        return hasErrors;

        bool DFSInheritance(string node, Dictionary<string, ClassDefinition> map,
            HashSet<string> white, HashSet<string> gray, HashSet<string> black,
            Stack<(string name, int line, int column)> currentPath)
        {
            if (black.Contains(node)) return true;

            if (gray.Contains(node))
            {
                if (classMap.TryGetValue(node, out var cycleCls))
                    currentPath.Push((cycleCls.Name, cycleCls.SourceLine, cycleCls.SourceColumn));
                return false;
            }

            white.Remove(node);
            gray.Add(node);

            if (map.TryGetValue(node, out var cls))
            {
                currentPath.Push((cls.Name, cls.SourceLine, cls.SourceColumn));

                if (cls.Base is not null && map.ContainsKey(cls.Base.Name))
                    if (!DFSInheritance(cls.Base.Name, map, white, gray, black, currentPath))
                        return false;

                currentPath.Pop();
            }

            gray.Remove(node);
            black.Add(node);

            return true;
        }
    }

    #endregion

    #region 废弃端点验证

    /// <summary>
    ///     验证 @deprecated 注解的 replaceWith 参数指向同一服务中已存在的端点
    /// </summary>
    private bool CheckDeprecatedEndpointValidity(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var hasErrors = false;

        foreach (var service in ir.Services)
        {
            var endpointNames = new HashSet<string>(service.Endpoints.Select(e => e.Name), StringComparer.Ordinal);

            foreach (var endpoint in service.Endpoints)
            {
                if (!endpoint.IsDeprecated) continue;

                if (endpoint.ReplaceWith is not null && !endpointNames.Contains(endpoint.ReplaceWith))
                    diagnostics.AddWarning(
                        _sourceFile,
                        endpoint.SourceLine,
                        endpoint.SourceColumn,
                        "HER2013",
                        $"服务 '{service.Name}' 的废弃端点 '{endpoint.Name}' 指定了不存在的替代端点 '{endpoint.ReplaceWith}'");
            }

            foreach (var deprecatedName in service.DeprecatedEndpoints)
                if (!endpointNames.Contains(deprecatedName))
                    diagnostics.AddWarning(
                        _sourceFile,
                        service.SourceLine,
                        service.SourceColumn,
                        "HER2014",
                        $"服务 '{service.Name}' 的 @deprecatedEndpoints 列表中的端点 '{deprecatedName}' 不存在于当前端点列表中");
        }

        return hasErrors;
    }

    #endregion

    #region 版本属性验证

    /// <summary>
    ///     验证 @version 注解的格式是否为有效的语义版本号（如 1.0、2.1.3）
    /// </summary>
    private bool CheckVersionAttributeValidity(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var hasErrors = false;

        foreach (var service in ir.Services)
        {
            if (service.Version == "1.0") continue;

            var parts = service.Version.Split('.');
            if (parts.Length is < 1 or > 3)
            {
                diagnostics.AddError(
                    _sourceFile,
                    service.SourceLine,
                    service.SourceColumn,
                    "HER2015",
                    $"服务 '{service.Name}' 的版本号 '{service.Version}' 格式无效，应为 1-3 段数字（如 1.0、2.1.3）");
                hasErrors = true;
                continue;
            }

            foreach (var part in parts)
                if (!int.TryParse(part, out _))
                {
                    diagnostics.AddError(
                        _sourceFile,
                        service.SourceLine,
                        service.SourceColumn,
                        "HER2015",
                        $"服务 '{service.Name}' 的版本号 '{service.Version}' 格式无效，每段必须为整数");
                    hasErrors = true;
                    break;
                }
        }

        return hasErrors;
    }

    #endregion

    #region 类型判定工具

    private bool IsKnownType(NamedType named)
    {
        if (PrimitiveType.FromName(named.Name) is not null) return true;

        if (_typeNames.Contains(named.Name)) return true;

        if (named.Namespace is not null && _usingNamespaces.Contains(named.Namespace)) return true;

        return false;
    }

    #endregion

    #region 类型名称收集

    private HashSet<string> CollectTypeNames(SchemaIR ir)
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "datetime", "map", "set", "bytes", "decimal", "any", "object"
        };

        foreach (var c in ir.Classes) names.Add(c.Name);

        foreach (var e in ir.Enums) names.Add(e.Name);

        foreach (var f in ir.Flags) names.Add(f.Name);

        foreach (var u in ir.Unions) names.Add(u.Name);

        foreach (var storage in ir.Storages)
        {
            foreach (var model in storage.Models) names.Add(model.Name);

            foreach (var stream in storage.Streams) names.Add(stream.Name);

            foreach (var cache in storage.Caches) names.Add(cache.Name);
        }

        foreach (var service in ir.Services) names.Add(service.Name);

        return names;
    }

    private HashSet<string> CollectUsingNamespaces(SchemaIR ir)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var u in ir.Usings) names.Add(u.Namespace);

        return names;
    }

    /// <summary>
    ///     收集每个类型名到其字段引用的类型名的映射，用于循环引用检测
    /// </summary>
    private Dictionary<string, HashSet<string>> CollectTypeFieldReferences(SchemaIR ir)
    {
        var refs = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var cls in ir.Classes) refs[cls.Name] = CollectFieldTypeNames(cls.fields.Select(f => f.FieldType));

        foreach (var union in ir.Unions)
        {
            var typeNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var variant in union.Variants)
                if (variant.Payload is not null)
                    CollectTypeName(variant.Payload, typeNames);

            refs[union.Name] = typeNames;
        }

        foreach (var storage in ir.Storages)
        foreach (var model in storage.Models)
            refs[model.Name] = CollectFieldTypeNames(model.fields.Select(f => f.FieldType));

        return refs;
    }

    private HashSet<string> CollectFieldTypeNames(IEnumerable<SchemaType> types)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types) CollectTypeName(type, names);
        return names;
    }

    private void CollectTypeName(SchemaType type, HashSet<string> names)
    {
        if (type is NamedType named) names.Add(named.Name);
    }

    #endregion

    #region 命名类型引用验证

    private bool CheckNamedTypeReferences(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var hasErrors = false;

        foreach (var cls in ir.Classes)
        foreach (var field in cls.fields)
            hasErrors |= ValidateType(field.FieldType, diagnostics, $"类 '{cls.Name}' 字段 '{field.Name}'",
                field.SourceLine, field.SourceColumn);

        foreach (var union in ir.Unions)
        foreach (var variant in union.Variants)
            if (variant.Payload is not null)
                hasErrors |= ValidateType(variant.Payload, diagnostics, $"联合类型 '{union.Name}'", union.SourceLine,
                    union.SourceColumn);

        foreach (var storage in ir.Storages)
        {
            foreach (var model in storage.Models)
            {
                hasErrors |= ValidateType(model.KeyType, diagnostics, $"模型 '{model.Name}' 键类型", model.SourceLine,
                    model.SourceColumn);

                foreach (var field in model.fields)
                    hasErrors |= ValidateType(field.FieldType, diagnostics, $"模型 '{model.Name}' 字段 '{field.Name}'",
                        field.SourceLine, field.SourceColumn);
            }

            foreach (var stream in storage.Streams)
            {
                hasErrors |= ValidateType(stream.EventType, diagnostics, $"流 '{stream.Name}' 元素类型", stream.SourceLine,
                    stream.SourceColumn);

                if (stream.KeyType is not null)
                    hasErrors |= ValidateType(stream.KeyType, diagnostics, $"流 '{stream.Name}' 键类型", stream.SourceLine,
                        stream.SourceColumn);
            }

            foreach (var cache in storage.Caches)
            {
                hasErrors |= ValidateType(cache.KeyType, diagnostics, $"缓存 '{cache.Name}' 键类型", cache.SourceLine,
                    cache.SourceColumn);
                hasErrors |= ValidateType(cache.ValueType, diagnostics, $"缓存 '{cache.Name}' 值类型", cache.SourceLine,
                    cache.SourceColumn);
            }
        }

        foreach (var service in ir.Services)
        foreach (var endpoint in service.Endpoints ?? [])
        {
            foreach (var param in endpoint.Parameters)
                hasErrors |= ValidateType(param.ParameterType, diagnostics, $"端点 '{endpoint.Name}' 参数 '{param.Name}'",
                    param.SourceLine, param.SourceColumn);

            if (endpoint.ReturnType is not null)
                hasErrors |= ValidateType(endpoint.ReturnType, diagnostics, $"端点 '{endpoint.Name}' 返回类型",
                    endpoint.SourceLine, endpoint.SourceColumn);
        }

        foreach (var micro in ir.Micros)
        {
            foreach (var param in micro.Parameters ?? [])
                hasErrors |= ValidateType(param.ParameterType, diagnostics, $"微服务 '{micro.Name}' 参数 '{param.Name}'",
                    param.SourceLine, param.SourceColumn);

            if (micro.ReturnType is not null)
                hasErrors |= ValidateType(micro.ReturnType, diagnostics, $"微服务 '{micro.Name}' 返回类型", micro.SourceLine,
                    micro.SourceColumn);
        }

        return hasErrors;
    }

    private bool ValidateType(SchemaType type, SchemaDiagnosticSink diagnostics, string context, int sourceLine,
        int sourceColumn)
    {
        switch (type)
        {
            case NamedType named:
                return ValidateNamedType(named, diagnostics, context, sourceLine, sourceColumn);
            case ListType list:
                return ValidateType(list.ElementType, diagnostics, $"{context} 列表元素", sourceLine, sourceColumn);
            case OptionType option:
                return ValidateType(option.InnerType, diagnostics, $"{context} 可选值", sourceLine, sourceColumn);
            case ResultType result:
                var hasErr = ValidateType(result.OkType, diagnostics, $"{context} 成功分支", sourceLine, sourceColumn);
                if (result.ErrorType is not null)
                    hasErr |= ValidateType(result.ErrorType, diagnostics, $"{context} 错误分支", sourceLine, sourceColumn);
                return hasErr;
            case StreamType stream:
                return ValidateType(stream.InnerType, diagnostics, $"{context} 流元素", sourceLine, sourceColumn);
            case DictType dict:
                return ValidateType(dict.KeyType, diagnostics, $"{context} 字典键", sourceLine, sourceColumn) |
                       ValidateType(dict.ValueType, diagnostics, $"{context} 字典值", sourceLine, sourceColumn);
            case RecordType record:
                return ValidateType(record.KeyType, diagnostics, $"{context} 记录键", sourceLine, sourceColumn) |
                       ValidateType(record.ValueType, diagnostics, $"{context} 记录值", sourceLine, sourceColumn);
            case ReferenceType reference:
                return ValidateType(reference.ReferencedType, diagnostics, $"{context} 引用", sourceLine, sourceColumn);
            case ArrayType array:
                return ValidateType(array.ElementType, diagnostics, $"{context} 数组元素", sourceLine, sourceColumn);
            case PrimitiveType:
                return false;
            default:
                return false;
        }
    }

    private bool ValidateNamedType(NamedType named, SchemaDiagnosticSink diagnostics, string context, int sourceLine,
        int sourceColumn)
    {
        if (PrimitiveType.FromName(named.Name) is not null) return false;

        if (_typeNames.Contains(named.Name)) return false;

        if (named.Namespace is not null && _usingNamespaces.Contains(named.Namespace)) return false;

        diagnostics.AddError(
            _sourceFile,
            sourceLine,
            sourceColumn,
            "HER2001",
            $"未定义的类型 '{named.Name}'（使用位置：{context}，当前文件有 {_typeNames.Count} 个已知类型）");

        return true;
    }

    #endregion

    #region 注解校验

    /// <summary>
    ///     校验关键注解的参数格式和必填项
    /// </summary>
    private bool CheckAnnotationValidity(SchemaIR ir, SchemaDiagnosticSink diagnostics)
    {
        var hasErrors = false;

        foreach (var cls in ir.Classes)
        {
            if (cls.Attributes is not null)
                hasErrors |= CheckAnnotations(cls.Attributes, $"类 '{cls.Name}'", diagnostics, cls.SourceLine,
                    cls.SourceColumn);

            foreach (var field in cls.fields)
                if (field.Attributes is not null)
                    hasErrors |= CheckFieldAnnotations(field.Attributes, field.Name,
                        $"类 '{cls.Name}' 字段 '{field.Name}'", diagnostics, field.SourceLine, field.SourceColumn);
        }

        foreach (var storage in ir.Storages)
        {
            if (storage.Attributes is not null)
                hasErrors |= CheckAnnotations(storage.Attributes, $"存储 '{storage.Name}'", diagnostics,
                    storage.SourceLine, storage.SourceColumn);

            foreach (var model in storage.Models)
            {
                if (model.Attributes is not null)
                    hasErrors |= CheckAnnotations(model.Attributes, $"模型 '{model.Name}'", diagnostics, model.SourceLine,
                        model.SourceColumn);

                foreach (var field in model.fields)
                    if (field.Attributes is not null)
                        hasErrors |= CheckFieldAnnotations(field.Attributes, field.Name,
                            $"模型 '{model.Name}' 字段 '{field.Name}'", diagnostics, field.SourceLine, field.SourceColumn);
            }
        }

        return hasErrors;
    }

    private bool CheckAnnotations(IReadOnlyList<AttributeDefinition> attrs, string context,
        SchemaDiagnosticSink diagnostics, int sourceLine, int sourceColumn)
    {
        var hasErrors = false;

        foreach (var attr in attrs)
            if (string.IsNullOrWhiteSpace(attr.Name))
            {
                diagnostics.AddError(
                    _sourceFile,
                    sourceLine,
                    sourceColumn,
                    "HER2008",
                    $"注解名称不能为空（位置：{context}）");
                hasErrors = true;
            }

        return hasErrors;
    }

    private bool CheckFieldAnnotations(IReadOnlyList<AttributeDefinition> attrs, string fieldName, string context,
        SchemaDiagnosticSink diagnostics, int sourceLine, int sourceColumn)
    {
        var hasErrors = false;

        foreach (var attr in attrs)
        {
            if (string.IsNullOrWhiteSpace(attr.Name))
            {
                diagnostics.AddError(
                    _sourceFile,
                    sourceLine,
                    sourceColumn,
                    "HER2008",
                    $"字段 '{fieldName}' 的注解名称不能为空（位置：{context}）");
                hasErrors = true;
                continue;
            }

            switch (attr.Name)
            {
                case "unique":
                case "index":
                    break;

                case "ref":
                case "fk":
                    if (attr.Arguments.Count == 0)
                        diagnostics.AddWarning(
                            _sourceFile,
                            sourceLine,
                            sourceColumn,
                            "HER2009",
                            $"字段 '{fieldName}' 的外键注解未指定引用的目标表（位置：{context}）");
                    break;

                case "min":
                case "max":
                case "len":
                    if (attr.Arguments.Count == 0)
                        diagnostics.AddWarning(
                            _sourceFile,
                            sourceLine,
                            sourceColumn,
                            "HER2010",
                            $"字段 '{fieldName}' 的 '{attr.Name}' 注解缺少参数值（位置：{context}）");
                    break;
            }
        }

        return hasErrors;
    }

    #endregion
}