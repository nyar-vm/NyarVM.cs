using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Data]</c> 特性的类型自动生成 <c>restore</c> 方法，
///     从 <c>IDeserializer</c> 的反序列化调用序列还原类型实例。
/// </summary>
[Generator]
public sealed class RestorerGenerator : IIncrementalGenerator
{
    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DataGeneratorAttributeFacts.data_attribute_full_name,
                static (node, _) => node is TypeDeclarationSyntax typeDecl &&
                                    typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_restorer(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取需要生成还原代码的类型信息。
    /// </summary>
    private static RestorerTypeInfo? transform_restorer(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var dataAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{DataGeneratorAttributeFacts.data_attribute_full_name}");

        var mode = RestorerProjectMode.map;

        if (dataAttr is not null)
            foreach (var named in dataAttr.NamedArguments)
                if (named is { Key: "mode", Value.Value: int modeVal })
                    mode = (RestorerProjectMode)modeVal;

        var members = extract_members(typeSymbol);

        if (members.Count == 0) return null;

        var hasParameterlessConstructor = typeSymbol.Constructors.Any(c =>
            c.Parameters.Length == 0 && c.DeclaredAccessibility != Accessibility.Private);

        return new RestorerTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            hasParameterlessConstructor,
            mode,
            members);
    }

    /// <summary>
    ///     提取类型的所有公共可写属性和字段信息。
    /// </summary>
    private static List<RestorerMemberInfo> extract_members(INamedTypeSymbol typeSymbol)
    {
        var members = new List<RestorerMemberInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsStatic) continue;

            if (member.DeclaredAccessibility != Accessibility.Public) continue;

            if (DataGeneratorAttributeFacts.has_any_attribute(member.GetAttributes(), DataGeneratorAttributeFacts.ignore_attribute_full_names))
                continue;

            if (member is IPropertySymbol prop)
            {
                if (prop.IsWriteOnly)
                {
                    var info = build_restorer_member_info(prop.Name, prop.Type, prop.GetAttributes(),
                        prop.NullableAnnotation);
                    members.Add(info);
                    continue;
                }

                if (prop is { IsReadOnly: true, SetMethod: null }) continue;

                {
                    var info = build_restorer_member_info(prop.Name, prop.Type, prop.GetAttributes(),
                        prop.NullableAnnotation);
                    members.Add(info);
                }
            }
            else if (member is IFieldSymbol field)
            {
                if (field.IsReadOnly) continue;

                var info = build_restorer_member_info(field.Name, field.Type, field.GetAttributes(),
                    field.NullableAnnotation);
                members.Add(info);
            }
        }

        return members;
    }

    /// <summary>
    ///     从成员符号构建还原成员信息。
    /// </summary>
    private static RestorerMemberInfo build_restorer_member_info(
        string name,
        ITypeSymbol type,
        ImmutableArray<AttributeData> attributes,
        NullableAnnotation nullableAnnotation)
    {
        var category = categorize_type(type);
        var isRequired = is_type_required(type, nullableAnnotation);
        var bindingNames = DataGeneratorAttributeFacts.get_binding_names(attributes, name);
        var order = -1;
        object? defaultValue = null;

        var fieldAttr = DataGeneratorAttributeFacts.get_first_attribute(attributes, DataGeneratorAttributeFacts.field_attribute_full_names);
        if (fieldAttr is not null)
        {
            foreach (var named in fieldAttr.NamedArguments)
            {
                if (named is { Key: "order", Value.Value: int o }) order = o;

                if (named.Key == "default_value") defaultValue = named.Value.Value;

                if (named is { Key: "required", Value.Value: bool req }) isRequired = req;
            }
        }

        return new RestorerMemberInfo(name, category, type, isRequired, bindingNames, order, defaultValue);
    }
    /// <summary>
    ///     判断类型是否为必填（不可为 null）。
    /// </summary>
    private static bool is_type_required(ITypeSymbol type, NullableAnnotation nullableAnnotation)
    {
        if (type.IsReferenceType) return nullableAnnotation == NullableAnnotation.NotAnnotated;

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return false;

        return true;
    }

    /// <summary>
    ///     将类型符号分类为反序列化类别。
    /// </summary>
    private static RestorerMemberCategory categorize_type(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String) return RestorerMemberCategory.@string;

        if (type.SpecialType == SpecialType.System_Int32) return RestorerMemberCategory.int32;

        if (type.SpecialType == SpecialType.System_Int64) return RestorerMemberCategory.int64;

        if (type.SpecialType == SpecialType.System_UInt64) return RestorerMemberCategory.uint64;

        if (type.SpecialType == SpecialType.System_Boolean) return RestorerMemberCategory.boolean;

        if (type.SpecialType == SpecialType.System_Double) return RestorerMemberCategory.float64;

        if (type.SpecialType == SpecialType.System_Single) return RestorerMemberCategory.float32;

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var fullName = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName == "global::Sonic.Standard.Sonic.Core.Option<T>") return RestorerMemberCategory.option;

            if (fullName == "global::System.Collections.Generic.Dictionary<TKey, TValue>"
                && namedType.TypeArguments[0].SpecialType == SpecialType.System_String)
                return RestorerMemberCategory.dictionary_string;

            if (fullName == "global::System.Collections.Generic.List<T>") return RestorerMemberCategory.list;
        }

        if (type is IArrayTypeSymbol) return RestorerMemberCategory.list;

        if (implements_i_list(type)) return RestorerMemberCategory.list;

        return RestorerMemberCategory.complex;
    }

    /// <summary>
    ///     检查类型是否实现了 <c>IList&lt;T&gt;</c> 接口。
    /// </summary>
    private static bool implements_i_list(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
            foreach (var iface in namedType.AllInterfaces)
                if (iface.IsGenericType
                    && iface.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                    "global::System.Collections.Generic.IList<T>")
                    return true;

        return false;
    }

    /// <summary>
    ///     获取类型的完全限定名。
    /// </summary>
    private static string get_type_full_name(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    ///     解析集合的元素类型。
    /// </summary>
    private static ITypeSymbol resolve_element_type(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType) return arrayType.ElementType;

        if (type is INamedTypeSymbol { IsGenericType: true } namedType) return namedType.TypeArguments[0];

        return type;
    }

    /// <summary>
    ///     生成所有类型的还原源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<RestorerTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_restorer_source(info);
            var hintName = $"{info.type_name}.Restorer.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的还原源代码。
    /// </summary>
    private static string generate_restorer_source(RestorerTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System.Buffers;");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        var typeKeyword = info.is_struct ? "partial struct" : "partial class";
        sb.append_line($"{typeKeyword} {info.type_name}");
        using (sb.block())
        {
            generate_restore_method(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>restore</c> 方法，从反序列化器还原类型实例。
    /// </summary>
    private static void generate_restore_method(SourceTextBuilder sb, RestorerTypeInfo info)
    {
        var typeName = info.type_name;

        sb.append_line("/// <summary>");
        sb.append_line($"/// 从反序列化器还原 <c>{typeName}</c> 实例。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"deserializer\">源反序列化器。</param>");
        sb.append_line($"/// <returns>还原后的 <c>{typeName}</c> 实例。</returns>");
        sb.append_line(
            $"public static {typeName} restore(global::Sonic.Standard.DataProcess.Deserialize.IDeserializer deserializer)");
        using (sb.block())
        {
            if (info.mode == RestorerProjectMode.tuple)
                generate_tuple_restore_body(sb, info);
            else
                generate_map_restore_body(sb, info);
        }
    }

    /// <summary>
    ///     生成 Map 模式的还原方法体。
    /// </summary>
    private static void generate_map_restore_body(SourceTextBuilder sb, RestorerTypeInfo info)
    {
        var typeName = info.type_name;
        var instanceName = info.is_struct ? "obj" : "var obj = new " + typeName + "()";
        sb.append_line($"{instanceName};");
        sb.append_line();
        sb.append_line("using var __obj = deserializer.deserialize_map();");
        sb.append_line();
        sb.append_line("while (true)");
        using (sb.block())
        {
            sb.append_line("var __field_result = __obj.read_field_name();");
            sb.append_line("if (__field_result.is_err) break;");
            sb.append_line("var __fieldName = __field_result.unwrap();");
            sb.append_line();
            sb.append_line("switch (__fieldName)");
            using (sb.block())
            {
                foreach (var member in info.members) generate_member_read(sb, member);

                sb.append_line("default:");
                sb.append_line("    break;");
            }
        }

        sb.append_line();
        sb.append_line("__obj.end();");
        sb.append_line();

        generate_default_assignments(sb, info);

        sb.append_line("return obj;");
    }

    /// <summary>
    ///     生成 Tuple 模式的还原方法体。
    /// </summary>
    private static void generate_tuple_restore_body(SourceTextBuilder sb, RestorerTypeInfo info)
    {
        var typeName = info.type_name;
        var instanceName = info.is_struct ? "obj" : "var obj = new " + typeName + "()";
        sb.append_line($"{instanceName};");
        sb.append_line();
        sb.append_line($"using var __tuple = deserializer.deserialize_sequence({info.members.Count});");
        sb.append_line();

        var orderedMembers = info.members.OrderBy(m => m.order >= 0 ? m.order : int.MaxValue).ThenBy(m => m.name)
            .ToList();

        foreach (var member in orderedMembers)
        {
            sb.append_line("if (!__tuple.try_read_element(deserializer)) goto __done;");
            generate_value_assign(sb, member, "obj");
            sb.append_line();
        }

        sb.append_line("__done:");
        sb.append_line("__tuple.end();");
        sb.append_line();

        generate_default_assignments(sb, info);

        sb.append_line("return obj;");
    }

    /// <summary>
    ///     生成默认值赋值代码，为缺失的必填字段赋予默认值。
    /// </summary>
    private static void generate_default_assignments(SourceTextBuilder sb, RestorerTypeInfo info)
    {
        foreach (var member in info.members)
            if (member.default_value is not null)
            {
                var defaultExpr = format_default_value(member);
                sb.append_line($"if (obj.{member.name} == null) obj.{member.name} = {defaultExpr};");
            }
    }

    /// <summary>
    ///     格式化默认值表达式。
    /// </summary>
    private static string format_default_value(RestorerMemberInfo member)
    {
        if (member.default_value is string s) return $"\"{s}\"";

        if (member.default_value is bool b) return b ? "true" : "false";

        if (member.default_value is int i) return i.ToString();

        if (member.default_value is long l) return l + "L";

        if (member.default_value is double d) return d.ToString("R") + "d";

        if (member.default_value is float f) return f.ToString("R") + "f";

        if (member.default_value is null) return "null";

        return member.default_value.ToString() ?? "null";
    }

    /// <summary>
    ///     生成单个成员的读取与赋值代码（Map 模式 switch 分支）。
    /// </summary>
    private static void generate_member_read(SourceTextBuilder sb, RestorerMemberInfo member)
    {
        foreach (var bindingName in member.binding_names)
        {
            sb.append_line($"case \"{bindingName}\":");
        }

        generate_value_assign(sb, member, "obj");
        sb.append_line("    break;");
        sb.append_line();
    }

    /// <summary>
    ///     根据成员类别生成值的读取与赋值代码。
    /// </summary>
    private static void generate_value_assign(SourceTextBuilder sb, RestorerMemberInfo member, string targetName)
    {
        var name = member.name;

        switch (member.category)
        {
            case RestorerMemberCategory.@string:
                sb.append_line($"{targetName}.{name} = deserializer.deserialize_utf8().to_string();");
                break;

            case RestorerMemberCategory.int32:
                sb.append_line($"{targetName}.{name} = deserializer.deserialize_i32();");
                break;

            case RestorerMemberCategory.int64:
                sb.append_line($"{targetName}.{name} = deserializer.deserialize_i64();");
                break;

            case RestorerMemberCategory.uint64:
                sb.append_line($"{targetName}.{name} = deserializer.deserialize_u64();");
                break;

            case RestorerMemberCategory.boolean:
                sb.append_line($"{targetName}.{name} = deserializer.deserialize_bool();");
                break;

            case RestorerMemberCategory.float32:
                sb.append_line($"{targetName}.{name} = deserializer.deserialize_f32();");
                break;

            case RestorerMemberCategory.float64:
                sb.append_line($"{targetName}.{name} = deserializer.deserialize_f64();");
                break;

            case RestorerMemberCategory.option:
            {
                var innerType = ((INamedTypeSymbol)member.type_symbol).TypeArguments[0];
                var innerTypeFullName = get_type_full_name(innerType);
                sb.append_line("if (deserializer.try_read_null())");
                using (sb.block())
                {
                    sb.append_line(
                        $"{targetName}.{name} = global::Sonic.Standard.Sonic.Core.Option<{innerTypeFullName}>.None;");
                }

                sb.append_line("else");
                using (sb.block())
                {
                    read_typed_value(sb, innerType, $"var __{name}_value", out var valueVar);
                    sb.append_line(
                        $"{targetName}.{name} = global::Sonic.Standard.Sonic.Core.Option<{innerTypeFullName}>.Some({valueVar});");
                }

                break;
            }

            case RestorerMemberCategory.list:
            {
                var elementType = resolve_element_type(member.type_symbol);
                var elementTypeFullName = get_type_full_name(elementType);
                sb.append_line(
                    $"{targetName}.{name} = new global::System.Collections.Generic.List<{elementTypeFullName}>();");
                sb.append_line("using var __arr = deserializer.deserialize_sequence();");
                sb.append_line("while (__arr.try_read_element(deserializer))");
                using (sb.block())
                {
                    read_typed_value(sb, elementType, $"var __{name}_item", out var itemVar);
                    sb.append_line($"{targetName}.{name}.Add({itemVar});");
                }

                sb.append_line("__arr.end();");
                break;
            }

            case RestorerMemberCategory.dictionary_string:
            {
                var valueType = ((INamedTypeSymbol)member.type_symbol).TypeArguments[1];
                var valueTypeFullName = get_type_full_name(valueType);
                sb.append_line(
                    $"{targetName}.{name} = new global::System.Collections.Generic.Dictionary<string, {valueTypeFullName}>();");
                sb.append_line("using var __dict = deserializer.deserialize_map();");
                sb.append_line("while (true)");
                using (sb.block())
                {
                    sb.append_line("var __key_result = __dict.read_field_name();");
                    sb.append_line("if (__key_result.is_err) break;");
                    sb.append_line("var __dictKey = __key_result.unwrap();");
                    read_typed_value(sb, valueType, $"var __{name}_value", out var valueVar);
                    sb.append_line($"{targetName}.{name}[__dictKey] = {valueVar};");
                }

                sb.append_line("__dict.end();");
                break;
            }

            case RestorerMemberCategory.complex:
                var complexTypeFullName = get_type_full_name(member.type_symbol);
                sb.append_line($"{targetName}.{name} = {complexTypeFullName}.restore(deserializer);");
                break;
        }
    }

    /// <summary>
    ///     根据类型符号读取对应类型的值。
    /// </summary>
    private static void read_typed_value(SourceTextBuilder sb, ITypeSymbol type, string varDecl, out string varName)
    {
        var dotIndex = varDecl.IndexOf(' ');
        varName = dotIndex >= 0 ? varDecl.Substring(dotIndex + 1) : varDecl;

        if (type.SpecialType == SpecialType.System_String)
        {
            sb.append_line($"{varDecl} = deserializer.deserialize_utf8().to_string();");
        }
        else if (type.SpecialType == SpecialType.System_Int32)
        {
            sb.append_line($"{varDecl} = deserializer.deserialize_i32();");
        }
        else if (type.SpecialType == SpecialType.System_Int64)
        {
            sb.append_line($"{varDecl} = deserializer.deserialize_i64();");
        }
        else if (type.SpecialType == SpecialType.System_UInt64)
        {
            sb.append_line($"{varDecl} = deserializer.deserialize_u64();");
        }
        else if (type.SpecialType == SpecialType.System_Boolean)
        {
            sb.append_line($"{varDecl} = deserializer.deserialize_bool();");
        }
        else if (type.SpecialType == SpecialType.System_Double)
        {
            sb.append_line($"{varDecl} = deserializer.deserialize_f64();");
        }
        else if (type.SpecialType == SpecialType.System_Single)
        {
            sb.append_line($"{varDecl} = deserializer.deserialize_f32();");
        }
        else if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var fullName = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName == "global::Sonic.Standard.Sonic.Core.Option<T>")
            {
                var innerType = namedType.TypeArguments[0];
                var innerTypeFullName = get_type_full_name(innerType);
                sb.append_line("if (deserializer.try_read_null())");
                using (sb.block())
                {
                    sb.append_line($"{varDecl} = global::Sonic.Standard.Sonic.Core.Option<{innerTypeFullName}>.None;");
                }

                sb.append_line("else");
                using (sb.block())
                {
                    read_typed_value(sb, innerType, $"var __{varName}_inner", out var innerVar);
                    sb.append_line(
                        $"{varName} = global::Sonic.Standard.Sonic.Core.Option<{innerTypeFullName}>.Some(__{varName}_inner);");
                }
            }
            else
            {
                var typeFullName = get_type_full_name(type);
                sb.append_line($"{varDecl} = {typeFullName}.restore(deserializer);");
            }
        }
        else
        {
            var typeFullName = get_type_full_name(type);
            sb.append_line($"{varDecl} = {typeFullName}.restore(deserializer);");
        }
    }

    #region 数据模型

    /// <summary>
    ///     需要生成还原方法的目标类型信息。
    /// </summary>
    internal readonly struct RestorerTypeInfo
    {
        /// <summary>
        ///     类型名称。
        /// </summary>
        public readonly string type_name;

        /// <summary>
        ///     完全限定类型名称。
        /// </summary>
        public readonly string fully_qualified_name;

        /// <summary>
        ///     命名空间名称。
        /// </summary>
        public readonly string? namespace_name;

        /// <summary>
        ///     是否为结构体。
        /// </summary>
        public readonly bool is_struct;

        /// <summary>
        ///     是否具有无参构造函数。
        /// </summary>
        public readonly bool has_parameterless_constructor;

        /// <summary>
        ///     投影模式。
        /// </summary>
        public readonly RestorerProjectMode mode;

        /// <summary>
        ///     成员列表。
        /// </summary>
        public readonly List<RestorerMemberInfo> members;

        /// <summary>
        ///     初始化 <see cref="RestorerTypeInfo" /> 的新实例。
        /// </summary>
        public RestorerTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            bool isStruct,
            bool hasParameterlessConstructor,
            RestorerProjectMode mode,
            List<RestorerMemberInfo> members)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            is_struct = isStruct;
            has_parameterless_constructor = hasParameterlessConstructor;
            this.mode = mode;
            this.members = members;
        }
    }

    /// <summary>
    ///     还原成员信息。
    /// </summary>
    internal readonly struct RestorerMemberInfo
    {
        /// <summary>
        ///     成员名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     反序列化类别。
        /// </summary>
        public readonly RestorerMemberCategory category;

        /// <summary>
        ///     类型符号。
        /// </summary>
        public readonly ITypeSymbol type_symbol;

        /// <summary>
        ///     是否为必填字段。
        /// </summary>
        public readonly bool is_required;

        /// <summary>
        ///     可接受的绑定名称列表，首项为主绑定名，其余为别名。
        /// </summary>
        public readonly ImmutableArray<string> binding_names;

        /// <summary>
        ///     位置顺序，-1 表示未指定。
        /// </summary>
        public readonly int order;

        /// <summary>
        ///     默认值，为 null 表示无默认值。
        /// </summary>
        public readonly object? default_value;

        /// <summary>
        ///     初始化 <see cref="RestorerMemberInfo" /> 的新实例。
        /// </summary>
        public RestorerMemberInfo(
            string name,
            RestorerMemberCategory category,
            ITypeSymbol typeSymbol,
            bool isRequired,
            ImmutableArray<string> bindingNames,
            int order,
            object? defaultValue)
        {
            this.name = name;
            this.category = category;
            type_symbol = typeSymbol;
            is_required = isRequired;
            binding_names = bindingNames;
            this.order = order;
            default_value = defaultValue;
        }
    }

    /// <summary>
    ///     成员反序列化类别枚举。
    /// </summary>
    internal enum RestorerMemberCategory
    {
        /// <summary>
        ///     字符串。
        /// </summary>
        @string,

        /// <summary>
        ///     32 位有符号整数。
        /// </summary>
        int32,

        /// <summary>
        ///     64 位有符号整数。
        /// </summary>
        int64,

        /// <summary>
        ///     64 位无符号整数。
        /// </summary>
        uint64,

        /// <summary>
        ///     布尔值。
        /// </summary>
        boolean,

        /// <summary>
        ///     32 位浮点数。
        /// </summary>
        float32,

        /// <summary>
        ///     64 位浮点数。
        /// </summary>
        float64,

        /// <summary>
        ///     Option 类型。
        /// </summary>
        option,

        /// <summary>
        ///     列表/数组。
        /// </summary>
        list,

        /// <summary>
        ///     字符串键字典。
        /// </summary>
        dictionary_string,

        /// <summary>
        ///     复杂类型。
        /// </summary>
        complex
    }

    /// <summary>
    ///     还原投影模式。
    /// </summary>
    internal enum RestorerProjectMode
    {
        /// <summary>
        ///     映射模式。
        /// </summary>
        map = 0,

        /// <summary>
        ///     元组模式。
        /// </summary>
        tuple = 1
    }

    #endregion
}
