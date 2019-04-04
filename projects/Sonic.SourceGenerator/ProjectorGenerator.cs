using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Data]</c> 特性的类型自动生成 <c>project</c> 方法，
///     将类型实例投影到 <c>ISerializer</c> 的序列化调用序列。
/// </summary>
[Generator]
public sealed class ProjectorGenerator : IIncrementalGenerator
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
                static (ctx, ct) => transform_projector(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取需要生成投影代码的类型信息。
    /// </summary>
    private static ProjectorTypeInfo? transform_projector(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var dataAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{DataGeneratorAttributeFacts.data_attribute_full_name}");

        var mode = ProjectMode.map;
        var renameAll = RenameStyle.none;

        if (dataAttr is not null)
            foreach (var named in dataAttr.NamedArguments)
            {
                if (named is { Key: "mode", Value.Value: int modeVal }) mode = (ProjectMode)modeVal;

                if (named is { Key: "rename_all", Value.Value: int renameVal }) renameAll = (RenameStyle)renameVal;
            }

        var members = extract_members(typeSymbol);

        if (members.Count == 0) return null;

        return new ProjectorTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            mode,
            renameAll,
            members);
    }

    /// <summary>
    ///     提取类型的所有公共可读属性和字段信息。
    /// </summary>
    private static List<MemberInfo> extract_members(INamedTypeSymbol typeSymbol)
    {
        var members = new List<MemberInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsStatic) continue;

            if (member.DeclaredAccessibility != Accessibility.Public) continue;

            if (DataGeneratorAttributeFacts.has_any_attribute(member.GetAttributes(), DataGeneratorAttributeFacts.ignore_attribute_full_names))
                continue;

            if (member is IPropertySymbol prop)
            {
                if (prop.IsWriteOnly) continue;

                var info = build_member_info(prop.Name, prop.Type, prop.GetAttributes(), prop.NullableAnnotation);
                members.Add(info);
            }
            else if (member is IFieldSymbol field)
            {
                var info = build_member_info(field.Name, field.Type, field.GetAttributes(), field.NullableAnnotation);
                members.Add(info);
            }
        }

        return members;
    }

    /// <summary>
    ///     从成员符号构建成员信息。
    /// </summary>
    private static MemberInfo build_member_info(
        string name,
        ITypeSymbol type,
        ImmutableArray<AttributeData> attributes,
        NullableAnnotation nullableAnnotation)
    {
        var category = categorize_type(type);
        var isRequired = is_type_required(type, nullableAnnotation);
        var fieldName = DataGeneratorAttributeFacts.get_explicit_binding_name(attributes);
        var order = -1;
        var skipWhenNull = false;
        var skipWhenDefault = false;

        var fieldAttr = DataGeneratorAttributeFacts.get_first_attribute(attributes, DataGeneratorAttributeFacts.field_attribute_full_names);
        if (fieldAttr is not null)
        {
            foreach (var named in fieldAttr.NamedArguments)
            {
                if (named is { Key: "order", Value.Value: int o }) order = o;

                if (named is { Key: "skip_when_null", Value.Value: bool sn }) skipWhenNull = sn;

                if (named is { Key: "skip_when_default", Value.Value: bool sd }) skipWhenDefault = sd;

                if (named is { Key: "required", Value.Value: bool req }) isRequired = req;
            }
        }

        return new MemberInfo(name, category, type, isRequired, fieldName, order, skipWhenNull, skipWhenDefault);
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
    ///     将类型符号分类为序列化类别。
    /// </summary>
    private static MemberCategory categorize_type(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String) return MemberCategory.@string;

        if (type.SpecialType == SpecialType.System_Int32) return MemberCategory.int32;

        if (type.SpecialType == SpecialType.System_Int64) return MemberCategory.int64;

        if (type.SpecialType == SpecialType.System_UInt64) return MemberCategory.uint64;

        if (type.SpecialType == SpecialType.System_Boolean) return MemberCategory.boolean;

        if (type.SpecialType == SpecialType.System_Double) return MemberCategory.float64;

        if (type.SpecialType == SpecialType.System_Single) return MemberCategory.float32;

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var fullName = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName == "global::Sonic.Standard.Sonic.Core.Option<T>") return MemberCategory.option;

            if (fullName == "global::System.Collections.Generic.Dictionary<TKey, TValue>"
                && namedType.TypeArguments[0].SpecialType == SpecialType.System_String)
                return MemberCategory.dictionary_string;

            if (fullName == "global::System.Collections.Generic.List<T>") return MemberCategory.list;
        }

        if (type is IArrayTypeSymbol) return MemberCategory.list;

        if (implements_i_list(type)) return MemberCategory.list;

        return MemberCategory.complex;
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
    ///     根据重命名风格转换字段名称。
    /// </summary>
    private static string apply_rename_style(string name, RenameStyle style)
    {
        return style switch
        {
            RenameStyle.camel_case => camel_case(name),
            RenameStyle.snake_case => snake_case(name),
            RenameStyle.kebab_case => kebab_case(name),
            RenameStyle.upper_camel_case => upper_camel_case(name),
            RenameStyle.lower_case => name.ToLowerInvariant(),
            RenameStyle.upper_case => name.ToUpperInvariant(),
            _ => name
        };
    }

    /// <summary>
    ///     将 PascalCase 转换为 camelCase。
    /// </summary>
    private static string camel_case(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var chars = name.ToCharArray();
        var i = 0;

        while (i < chars.Length && char.IsUpper(chars[i]))
            i++;

        if (i == 0) return name;

        if (i > 1) i--;

        chars[0] = char.ToLowerInvariant(chars[0]);

        return new string(chars, 0, chars.Length);
    }

    /// <summary>
    ///     将 PascalCase 转换为 snake_case。
    /// </summary>
    private static string snake_case(string name)
    {
        var sb = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     将 PascalCase 转换为 kebab-case。
    /// </summary>
    private static string kebab_case(string name)
    {
        var sb = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('-');

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     将名称转换为 UpperCamelCase（PascalCase）。
    /// </summary>
    private static string upper_camel_case(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        if (char.IsUpper(name[0])) return name;

        var chars = name.ToCharArray();
        chars[0] = char.ToUpperInvariant(chars[0]);

        return new string(chars);
    }

    /// <summary>
    ///     获取成员的序列化名称。
    /// </summary>
    private static string get_serialization_name(MemberInfo member, RenameStyle renameAll)
    {
        var name = member.field_name ?? member.name;

        if (member.field_name is null) name = apply_rename_style(name, renameAll);

        return name;
    }

    /// <summary>
    ///     生成所有类型的投影源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<ProjectorTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_projector_source(info);
            var hintName = $"{info.type_name}.Projector.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的投影源代码。
    /// </summary>
    private static string generate_projector_source(ProjectorTypeInfo info)
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
            generate_project_method(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>project</c> 方法体，将类型实例投影到序列化器。
    /// </summary>
    private static void generate_project_method(SourceTextBuilder sb, ProjectorTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 将当前 <c>{info.type_name}</c> 实例投影到指定的序列化器。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"value\">要投影的值。</param>");
        sb.append_line("/// <param name=\"serializer\">目标序列化器。</param>");
        sb.append_line(
            $"public void project(in {info.fully_qualified_name} value, global::Sonic.Standard.DataProcess.Serialize.ISerializer serializer)");
        using (sb.block())
        {
            if (info.mode == ProjectMode.tuple)
                generate_tuple_project_body(sb, info);
            else
                generate_map_project_body(sb, info);
        }
    }

    /// <summary>
    ///     生成 Map 模式的投影方法体。
    /// </summary>
    private static void generate_map_project_body(SourceTextBuilder sb, ProjectorTypeInfo info)
    {
        var orderedMembers = info.members.OrderBy(m => m.order >= 0 ? m.order : int.MaxValue).ThenBy(m => m.name)
            .ToList();

        sb.append_line($"using var __obj = serializer.serialize_map({orderedMembers.Count});");
        sb.append_line();

        foreach (var member in orderedMembers) generate_member_write(sb, member, info.rename_all);

        sb.append_line();
        sb.append_line("__obj.end();");
    }

    /// <summary>
    ///     生成 Tuple 模式的投影方法体。
    /// </summary>
    private static void generate_tuple_project_body(SourceTextBuilder sb, ProjectorTypeInfo info)
    {
        var orderedMembers = info.members.OrderBy(m => m.order >= 0 ? m.order : int.MaxValue).ThenBy(m => m.name)
            .ToList();

        sb.append_line($"using var __tuple = serializer.serialize_sequence({orderedMembers.Count});");
        sb.append_line();

        foreach (var member in orderedMembers) generate_tuple_member_write(sb, member);

        sb.append_line();
        sb.append_line("__tuple.end();");
    }

    /// <summary>
    ///     生成单个成员的 Map 模式写入代码。
    /// </summary>
    private static void generate_member_write(SourceTextBuilder sb, MemberInfo member, RenameStyle renameAll)
    {
        var name = member.name;
        var serialName = get_serialization_name(member, renameAll);

        if (member.skip_when_null && is_nullable_type(member.type_symbol))
        {
            sb.append_line($"if (value.{name} is not null)");
            using (sb.block())
            {
                emit_field_write(sb, member, serialName);
            }

            sb.append_line();
            return;
        }

        if (member.skip_when_default && !is_nullable_type(member.type_symbol))
        {
            var defaultExpr = get_default_expression(member.type_symbol);
            sb.append_line($"if (!object.Equals(value.{name}, {defaultExpr}))");
            using (sb.block())
            {
                emit_field_write(sb, member, serialName);
            }

            sb.append_line();
            return;
        }

        emit_field_write(sb, member, serialName);
        sb.append_line();
    }

    /// <summary>
    ///     发射字段名和值的写入代码。
    /// </summary>
    private static void emit_field_write(SourceTextBuilder sb, MemberInfo member, string serialName)
    {
        sb.append_line($"__obj.write_field_name(\"{serialName}\");");
        emit_typed_value(sb, member.category, member.type_symbol, $"value.{member.name}");
    }

    /// <summary>
    ///     生成单个成员的 Tuple 模式写入代码。
    /// </summary>
    private static void generate_tuple_member_write(SourceTextBuilder sb, MemberInfo member)
    {
        emit_typed_value(sb, member.category, member.type_symbol, $"value.{member.name}");
    }

    /// <summary>
    ///     根据类型类别写入对应的值表达式。
    /// </summary>
    private static void emit_typed_value(SourceTextBuilder sb, MemberCategory category, ITypeSymbol type,
        string valueExpr)
    {
        switch (category)
        {
            case MemberCategory.@string:
                sb.append_line(
                    $"serializer.serialize_utf8(global::Sonic.Standard.Text.Utf8Text.from_string({valueExpr}));");
                break;

            case MemberCategory.int32:
                sb.append_line($"serializer.serialize_i32({valueExpr});");
                break;

            case MemberCategory.int64:
                sb.append_line($"serializer.serialize_i64({valueExpr});");
                break;

            case MemberCategory.uint64:
                sb.append_line($"serializer.serialize_u64({valueExpr});");
                break;

            case MemberCategory.boolean:
                sb.append_line($"serializer.serialize_bool({valueExpr});");
                break;

            case MemberCategory.float32:
                sb.append_line($"serializer.serialize_f32({valueExpr});");
                break;

            case MemberCategory.float64:
                sb.append_line($"serializer.serialize_f64({valueExpr});");
                break;

            case MemberCategory.option:
                generate_option_write(sb, valueExpr, type);
                break;

            case MemberCategory.list:
                generate_list_write(sb, valueExpr, type);
                break;

            case MemberCategory.dictionary_string:
                generate_dictionary_write(sb, valueExpr, type);
                break;

            case MemberCategory.complex:
                var typeFullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.append_line($"{typeFullName}.project({valueExpr}, serializer);");
                break;
        }
    }

    /// <summary>
    ///     生成 <c>Option&lt;T&gt;</c> 的条件写入代码。
    /// </summary>
    private static void generate_option_write(SourceTextBuilder sb, string valueExpr, ITypeSymbol type)
    {
        var namedType = (INamedTypeSymbol)type;
        sb.append_line($"if ({valueExpr}.IsSome)");
        using (sb.block())
        {
            var innerType = namedType.TypeArguments[0];
            var innerCategory = categorize_type(innerType);
            emit_typed_value(sb, innerCategory, innerType, $"{valueExpr}.Value");
        }

        sb.append_line("else");
        using (sb.block())
        {
            sb.append_line("serializer.serialize_null();");
        }
    }

    /// <summary>
    ///     生成 <c>List&lt;T&gt;</c> 或可遍历集合的数组写入代码。
    /// </summary>
    private static void generate_list_write(SourceTextBuilder sb, string valueExpr, ITypeSymbol type)
    {
        ITypeSymbol elementType;

        if (type is IArrayTypeSymbol arrayType)
            elementType = arrayType.ElementType;
        else if (type is INamedTypeSymbol { IsGenericType: true } namedType)
            elementType = namedType.TypeArguments[0];
        else
            elementType = type;

        var elementCategory = categorize_type(elementType);

        sb.append_line("using var __arr = serializer.serialize_sequence();");
        sb.append_line($"foreach (var __item in {valueExpr})");
        using (sb.block())
        {
            emit_typed_value(sb, elementCategory, elementType, "__item");
        }

        sb.append_line("__arr.end();");
    }

    /// <summary>
    ///     生成 <c>Dictionary&lt;string, T&gt;</c> 的对象写入代码。
    /// </summary>
    private static void generate_dictionary_write(SourceTextBuilder sb, string valueExpr, ITypeSymbol type)
    {
        var namedType = (INamedTypeSymbol)type;
        var valueType = namedType.TypeArguments[1];
        var valueCategory = categorize_type(valueType);

        sb.append_line("using var __dict = serializer.serialize_map();");
        sb.append_line($"foreach (var __kvp in {valueExpr})");
        using (sb.block())
        {
            sb.append_line("__dict.write_field_name(__kvp.Key);");
            emit_typed_value(sb, valueCategory, valueType, "__kvp.Value");
        }

        sb.append_line("__dict.end();");
    }

    /// <summary>
    ///     判断类型是否为可空类型。
    /// </summary>
    private static bool is_nullable_type(ITypeSymbol type)
    {
        if (type.IsReferenceType) return true;

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return true;

        return false;
    }

    /// <summary>
    ///     获取类型的默认值表达式。
    /// </summary>
    private static string get_default_expression(ITypeSymbol type)
    {
        if (type.IsReferenceType) return "null";

        if (type.SpecialType == SpecialType.System_Int32) return "0";

        if (type.SpecialType == SpecialType.System_Int64) return "0L";

        if (type.SpecialType == SpecialType.System_UInt64) return "0UL";

        if (type.SpecialType == SpecialType.System_Double) return "0.0";

        if (type.SpecialType == SpecialType.System_Single) return "0.0f";

        if (type.SpecialType == SpecialType.System_Boolean) return "false";

        return $"default({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
    }

    #region 数据模型

    /// <summary>
    ///     需要生成投影方法的目标类型信息。
    /// </summary>
    internal readonly struct ProjectorTypeInfo
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
        ///     投影模式。
        /// </summary>
        public readonly ProjectMode mode;

        /// <summary>
        ///     字段重命名风格。
        /// </summary>
        public readonly RenameStyle rename_all;

        /// <summary>
        ///     成员列表。
        /// </summary>
        public readonly List<MemberInfo> members;

        /// <summary>
        ///     初始化 <see cref="ProjectorTypeInfo" /> 的新实例。
        /// </summary>
        public ProjectorTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            bool isStruct,
            ProjectMode mode,
            RenameStyle renameAll,
            List<MemberInfo> members)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            is_struct = isStruct;
            this.mode = mode;
            rename_all = renameAll;
            this.members = members;
        }
    }

    /// <summary>
    ///     成员信息，包含属性或字段的序列化元数据。
    /// </summary>
    internal readonly struct MemberInfo
    {
        /// <summary>
        ///     成员名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     序列化类别。
        /// </summary>
        public readonly MemberCategory category;

        /// <summary>
        ///     类型符号。
        /// </summary>
        public readonly ITypeSymbol type_symbol;

        /// <summary>
        ///     是否为必填字段。
        /// </summary>
        public readonly bool is_required;

        /// <summary>
        ///     自定义序列化名称，为 null 时使用成员名称。
        /// </summary>
        public readonly string? field_name;

        /// <summary>
        ///     位置顺序，-1 表示未指定。
        /// </summary>
        public readonly int order;

        /// <summary>
        ///     当值为 null 时是否跳过序列化。
        /// </summary>
        public readonly bool skip_when_null;

        /// <summary>
        ///     当值等于类型默认值时是否跳过序列化。
        /// </summary>
        public readonly bool skip_when_default;

        /// <summary>
        ///     初始化 <see cref="MemberInfo" /> 的新实例。
        /// </summary>
        public MemberInfo(
            string name,
            MemberCategory category,
            ITypeSymbol typeSymbol,
            bool isRequired,
            string? fieldName,
            int order,
            bool skipWhenNull,
            bool skipWhenDefault)
        {
            this.name = name;
            this.category = category;
            type_symbol = typeSymbol;
            is_required = isRequired;
            field_name = fieldName;
            this.order = order;
            skip_when_null = skipWhenNull;
            skip_when_default = skipWhenDefault;
        }
    }

    /// <summary>
    ///     成员序列化类别枚举。
    /// </summary>
    internal enum MemberCategory
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
    ///     数据投影模式。
    /// </summary>
    internal enum ProjectMode
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

    /// <summary>
    ///     字段重命名风格。
    /// </summary>
    internal enum RenameStyle
    {
        /// <summary>
        ///     不重命名。
        /// </summary>
        none = 0,

        /// <summary>
        ///     camelCase。
        /// </summary>
        camel_case = 1,

        /// <summary>
        ///     snake_case。
        /// </summary>
        snake_case = 2,

        /// <summary>
        ///     kebab-case。
        /// </summary>
        kebab_case = 3,

        /// <summary>
        ///     UpperCamelCase。
        /// </summary>
        upper_camel_case = 4,

        /// <summary>
        ///     全小写。
        /// </summary>
        lower_case = 5,

        /// <summary>
        ///     全大写。
        /// </summary>
        upper_case = 6
    }

    #endregion
}
