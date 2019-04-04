using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Data(generate_meta = true)]</c> 特性的类型自动生成 <c>ISchema&lt;T&gt;</c> 实现，
///     提供类型元数据、字段描述和验证器的统一访问。
/// </summary>
[Generator]
public sealed class SchemaGenerator : IIncrementalGenerator
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
                static (ctx, ct) => transform_schema(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取需要生成模式代码的类型信息。
    /// </summary>
    private static SchemaTypeInfo? transform_schema(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var dataAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{DataGeneratorAttributeFacts.data_attribute_full_name}");

        var generateMeta = true;
        string? description = null;
        var version = 1u;

        if (dataAttr is not null)
            foreach (var named in dataAttr.NamedArguments)
            {
                if (named is { Key: "generate_meta", Value.Value: bool gm }) generateMeta = gm;

                if (named is { Key: "description", Value.Value: string desc }) description = desc;

                if (named is { Key: "version", Value.Value: uint v }) version = v;
            }

        if (!generateMeta) return null;

        var members = extract_schema_members(typeSymbol);

        return new SchemaTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            description,
            version,
            members);
    }

    /// <summary>
    ///     提取类型的所有公共成员的模式信息。
    /// </summary>
    private static List<SchemaMemberInfo> extract_schema_members(INamedTypeSymbol typeSymbol)
    {
        var members = new List<SchemaMemberInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsStatic) continue;

            if (member.DeclaredAccessibility != Accessibility.Public) continue;

            if (DataGeneratorAttributeFacts.has_any_attribute(member.GetAttributes(), DataGeneratorAttributeFacts.ignore_attribute_full_names))
                continue;

            ITypeSymbol memberType;
            NullableAnnotation nullableAnnotation;
            ImmutableArray<AttributeData> attributes;

            if (member is IPropertySymbol prop)
            {
                if (prop.IsWriteOnly) continue;

                memberType = prop.Type;
                nullableAnnotation = prop.NullableAnnotation;
                attributes = prop.GetAttributes();
            }
            else if (member is IFieldSymbol field)
            {
                memberType = field.Type;
                nullableAnnotation = field.NullableAnnotation;
                attributes = field.GetAttributes();
            }
            else
            {
                continue;
            }

            var isRequired = is_type_required(memberType, nullableAnnotation);
            var alias = DataGeneratorAttributeFacts.get_explicit_binding_name(attributes);
            object? defaultValue = null;

            var fieldAttr = DataGeneratorAttributeFacts.get_first_attribute(attributes, DataGeneratorAttributeFacts.field_attribute_full_names);

            if (fieldAttr is not null)
            {
                foreach (var named in fieldAttr.NamedArguments)
                {
                    if (named.Key == "default_value") defaultValue = named.Value.Value;

                    if (named is { Key: "required", Value.Value: bool req }) isRequired = req;
                }
            }

            members.Add(new SchemaMemberInfo(member.Name, memberType, isRequired, alias, defaultValue));
        }

        return members;
    }
    /// <summary>
    ///     判断类型是否为必填。
    /// </summary>
    private static bool is_type_required(ITypeSymbol type, NullableAnnotation nullableAnnotation)
    {
        if (type.IsReferenceType) return nullableAnnotation == NullableAnnotation.NotAnnotated;

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return false;

        return true;
    }

    /// <summary>
    ///     生成所有类型的模式源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<SchemaTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_schema_source(info);
            var hintName = $"{info.type_name}.Schema.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的模式源代码。
    /// </summary>
    private static string generate_schema_source(SchemaTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{info.type_name}</c> 的数据契约模式实现。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public sealed class {info.type_name}Schema : global::Sonic.Standard.Data.Contract.ISchema<{info.fully_qualified_name}>");
        using (sb.block())
        {
            generate_meta_property(sb, info);
            sb.append_line();
            generate_validator_property(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>meta</c> 属性，包含类型名称和字段元数据。
    /// </summary>
    private static void generate_meta_property(SourceTextBuilder sb, SchemaTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 获取类型级别的元数据。");
        sb.append_line("/// </summary>");
        sb.append_line(
            "public global::Sonic.Standard.Data.Contract.TypeMeta meta { get; } = new global::Sonic.Standard.Data.Contract.TypeMeta(");
        sb.indent();
        sb.append_line($"\"{info.type_name}\",");
        sb.append_line(
            "global::System.Collections.Immutable.ImmutableArray.Create<global::Sonic.Standard.Data.Contract.TypeMeta.FieldMeta>(");
        sb.indent();

        for (var i = 0; i < info.members.Count; i++)
        {
            var member = info.members[i];
            var trailingComma = i < info.members.Count - 1 ? "," : "";
            var fqType = member.type_symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            sb.append_line(
                $"new global::Sonic.Standard.Data.Contract.TypeMeta.FieldMeta(\"{member.alias ?? member.name}\", typeof({fqType}).Name, {member.is_required.ToString().ToLowerInvariant()}){trailingComma}");
        }

        sb.outdent();
        sb.append_line("));");
        sb.outdent();
    }

    /// <summary>
    ///     生成 <c>validator</c> 属性。
    /// </summary>
    private static void generate_validator_property(SourceTextBuilder sb, SchemaTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 获取类型对应的验证器。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public global::Sonic.Standard.Data.Contract.IValidator<{info.fully_qualified_name}> validator => new {info.type_name}Validator();");
    }

    /// <summary>
    ///     格式化默认值表达式用于模式生成。
    /// </summary>
    private static string format_default_for_schema(object? defaultValue)
    {
        if (defaultValue is null) return "null";

        if (defaultValue is string s) return $"\"{StringEscapeHelper.escape_for_string(s)}\"";

        if (defaultValue is bool b) return b ? "true" : "false";

        if (defaultValue is int i) return i.ToString();

        if (defaultValue is long l) return l + "L";

        if (defaultValue is double d) return d.ToString("R") + "d";

        if (defaultValue is float f) return f.ToString("R") + "f";

        return $"(object){defaultValue}";
    }

    #region 数据模型

    /// <summary>
    ///     需要生成模式的类型信息。
    /// </summary>
    internal readonly struct SchemaTypeInfo
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
        ///     类型描述。
        /// </summary>
        public readonly string? description;

        /// <summary>
        ///     版本号。
        /// </summary>
        public readonly uint version;

        /// <summary>
        ///     成员列表。
        /// </summary>
        public readonly List<SchemaMemberInfo> members;

        /// <summary>
        ///     初始化 <see cref="SchemaTypeInfo" /> 的新实例。
        /// </summary>
        public SchemaTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            bool isStruct,
            string? description,
            uint version,
            List<SchemaMemberInfo> members)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            is_struct = isStruct;
            this.description = description;
            this.version = version;
            this.members = members;
        }
    }

    /// <summary>
    ///     模式成员信息。
    /// </summary>
    internal readonly struct SchemaMemberInfo
    {
        /// <summary>
        ///     成员名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     类型符号。
        /// </summary>
        public readonly ITypeSymbol type_symbol;

        /// <summary>
        ///     是否为必填字段。
        /// </summary>
        public readonly bool is_required;

        /// <summary>
        ///     字段别名。
        /// </summary>
        public readonly string? alias;

        /// <summary>
        ///     默认值。
        /// </summary>
        public readonly object? default_value;

        /// <summary>
        ///     初始化 <see cref="SchemaMemberInfo" /> 的新实例。
        /// </summary>
        public SchemaMemberInfo(
            string name,
            ITypeSymbol typeSymbol,
            bool isRequired,
            string? alias,
            object? defaultValue)
        {
            this.name = name;
            type_symbol = typeSymbol;
            is_required = isRequired;
            this.alias = alias;
            default_value = defaultValue;
        }
    }

    #endregion
}
