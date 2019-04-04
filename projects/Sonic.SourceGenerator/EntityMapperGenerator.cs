using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Data]</c> 且配置了 <c>storage</c> 参数的类型自动生成 <c>IEntityMapper&lt;T&gt;</c> 实现，
///     提供实体类型与存储表之间的映射关系。
/// </summary>
[Generator]
public sealed class EntityMapperGenerator : IIncrementalGenerator
{
    private const string _table_attribute_full_name = "Sonic.Standard.Infra.TableAttribute";
    private const string _column_attribute_full_name = "Sonic.Standard.Data.ColumnAttribute";
    private const string _primary_key_attribute_full_name = "Sonic.Standard.Data.KeyAttribute";
    private const string _unique_attribute_full_name = "Sonic.Standard.Data.UniqueAttribute";
    private const string _index_attribute_full_name = "Sonic.Standard.Data.IndexAttribute";
    private const string _version_attribute_full_name = "Sonic.Standard.Data.VersionAttribute";
    private const string _concurrency_check_attribute_full_name = "Sonic.Standard.Data.ConcurrencyCheckAttribute";

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
                static (ctx, ct) => transform_entity_mapper(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取需要生成实体映射器的类型信息。
    /// </summary>
    private static EntityMapperTypeInfo? transform_entity_mapper(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var tableAttr = typeSymbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_table_attribute_full_name}");

        string? tableName = null;

        if (tableAttr is not null)
            foreach (var named in tableAttr.NamedArguments)
                if (named is { Key: "name", Value.Value: string n })
                    tableName = n;

        var hasStorage = false;

        var dataAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{DataGeneratorAttributeFacts.data_attribute_full_name}");

        if (dataAttr is not null)
            foreach (var named in dataAttr.NamedArguments)
                if (named is { Key: "storage", Value.Value: not null })
                    hasStorage = true;

        if (!hasStorage && tableAttr is null) return null;

        tableName ??= typeSymbol.Name;
        var columns = extract_columns(typeSymbol);

        if (columns.Count == 0) return null;

        return new EntityMapperTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            tableName,
            columns);
    }

    /// <summary>
    ///     提取类型的所有列映射信息。
    /// </summary>
    private static List<ColumnInfo> extract_columns(INamedTypeSymbol typeSymbol)
    {
        var columns = new List<ColumnInfo>();

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

            var isNullable = is_type_nullable(memberType, nullableAnnotation);
            var isPrimaryKey = attributes.Any(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_primary_key_attribute_full_name}");
            var isUnique = attributes.Any(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_unique_attribute_full_name}");
            var isIndexed = attributes.Any(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_index_attribute_full_name}");
            var isVersion = attributes.Any(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_version_attribute_full_name}" ||
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_concurrency_check_attribute_full_name}");

            string? columnName = null;
            var storageType = infer_storage_type(memberType);

            var columnAttr = attributes.FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_column_attribute_full_name}");

            if (columnAttr is not null)
                foreach (var named in columnAttr.NamedArguments)
                {
                    if (named is { Key: "name", Value.Value: string cn }) columnName = cn;

                    if (named is { Key: "storage_type", Value.Value: int st }) storageType = (MappedStorageType)st;
                }

            object? defaultValue = null;

            var fieldAttr = DataGeneratorAttributeFacts.get_first_attribute(attributes, DataGeneratorAttributeFacts.field_attribute_full_names);

            if (fieldAttr is not null)
                foreach (var named in fieldAttr.NamedArguments)
                    if (named.Key == "default_value")
                        defaultValue = named.Value.Value;

            columns.Add(new ColumnInfo(
                member.Name,
                columnName ?? member.Name,
                storageType,
                isNullable,
                isPrimaryKey,
                isUnique,
                isIndexed,
                isVersion,
                defaultValue));
        }

        return columns;
    }
    /// <summary>
    ///     判断类型是否为可空。
    /// </summary>
    private static bool is_type_nullable(ITypeSymbol type, NullableAnnotation nullableAnnotation)
    {
        if (type.IsReferenceType) return nullableAnnotation != NullableAnnotation.NotAnnotated;

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return true;

        return false;
    }

    /// <summary>
    ///     根据成员类型推断存储类型。
    /// </summary>
    private static MappedStorageType infer_storage_type(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String) return MappedStorageType.@string;

        if (type.SpecialType == SpecialType.System_Int32) return MappedStorageType.int32;

        if (type.SpecialType == SpecialType.System_Int64) return MappedStorageType.int64;

        if (type.SpecialType == SpecialType.System_Double) return MappedStorageType.float64;

        if (type.SpecialType == SpecialType.System_Single) return MappedStorageType.float64;

        if (type.SpecialType == SpecialType.System_Boolean) return MappedStorageType.boolean;

        if (type.SpecialType == SpecialType.System_DateTime) return MappedStorageType.date_time;

        return MappedStorageType.@string;
    }

    /// <summary>
    ///     生成所有类型的实体映射器源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<EntityMapperTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_entity_mapper_source(info);
            var hintName = $"{info.type_name}.EntityMapper.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的实体映射器源代码。
    /// </summary>
    private static string generate_entity_mapper_source(EntityMapperTypeInfo info)
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
        sb.append_line($"/// <c>{info.type_name}</c> 的实体映射器实现。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public sealed class {info.type_name}EntityMapper : global::Sonic.Standard.Data.Storage.IEntityMapper<{info.fully_qualified_name}>");
        using (sb.block())
        {
            generate_table_name_property(sb, info);
            sb.append_line();
            generate_columns_property(sb, info);
            sb.append_line();
            generate_primary_keys_property(sb, info);
            sb.append_line();
            generate_concurrency_field_property(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>table_name</c> 属性。
    /// </summary>
    private static void generate_table_name_property(SourceTextBuilder sb, EntityMapperTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 获取目标存储表的名称。");
        sb.append_line("/// </summary>");
        sb.append_line($"public string table_name => \"{info.table_name}\";");
    }

    /// <summary>
    ///     生成 <c>columns</c> 属性。
    /// </summary>
    private static void generate_columns_property(SourceTextBuilder sb, EntityMapperTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 获取所有列的元数据列表。");
        sb.append_line("/// </summary>");
        sb.append_line(
            "public global::System.Collections.Generic.IReadOnlyList<global::Sonic.Standard.Data.Storage.ColumnMeta> columns => new global::System.Collections.Generic.List<global::Sonic.Standard.Data.Storage.ColumnMeta>");
        using (sb.block())
        {
            for (var i = 0; i < info.columns.Count; i++)
            {
                var col = info.columns[i];
                var storageTypeStr = $"global::Sonic.Standard.Data.Storage.StorageType.{col.storage_type}";
                var defaultArg = format_default_for_column(col.default_value);
                var trailingComma = i < info.columns.Count - 1 ? "," : ",";

                sb.append_line("new global::Sonic.Standard.Data.Storage.ColumnMeta(");
                sb.indent();
                sb.append_line($"\"{col.member_name}\",");
                sb.append_line($"\"{col.column_name}\",");
                sb.append_line($"{storageTypeStr},");
                sb.append_line($"{col.is_nullable.ToString().ToLowerInvariant()},");
                sb.append_line($"{col.is_unique.ToString().ToLowerInvariant()},");
                sb.append_line($"{col.is_indexed.ToString().ToLowerInvariant()},");
                sb.append_line($"{defaultArg}),");
                sb.outdent();
            }
        }

        sb.append_line(";");
    }

    /// <summary>
    ///     生成 <c>primary_keys</c> 属性。
    /// </summary>
    private static void generate_primary_keys_property(SourceTextBuilder sb, EntityMapperTypeInfo info)
    {
        var primaryKeyIndices = new List<int>();

        for (var i = 0; i < info.columns.Count; i++)
            if (info.columns[i].is_primary_key)
                primaryKeyIndices.Add(i);

        sb.append_line("/// <summary>");
        sb.append_line("/// 获取主键列的索引列表。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public global::System.Collections.Generic.IReadOnlyList<int> primary_keys => new int[] {{ {string.Join(", ", primaryKeyIndices)} }};");
    }

    /// <summary>
    ///     生成 <c>concurrency_field</c> 属性。
    /// </summary>
    private static void generate_concurrency_field_property(SourceTextBuilder sb, EntityMapperTypeInfo info)
    {
        var concurrencyIndex = -1;

        for (var i = 0; i < info.columns.Count; i++)
            if (info.columns[i].is_version)
            {
                concurrencyIndex = i;
                break;
            }

        sb.append_line("/// <summary>");
        sb.append_line("/// 获取并发检查字段的列索引。");
        sb.append_line("/// </summary>");
        sb.append_line($"public int? concurrency_field => {concurrencyIndex} >= 0 ? {concurrencyIndex} : null;");
    }

    /// <summary>
    ///     格式化默认值表达式用于列元数据。
    /// </summary>
    private static string format_default_for_column(object? defaultValue)
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
    ///     需要生成实体映射器的类型信息。
    /// </summary>
    internal readonly struct EntityMapperTypeInfo
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
        ///     表名称。
        /// </summary>
        public readonly string table_name;

        /// <summary>
        ///     列信息列表。
        /// </summary>
        public readonly List<ColumnInfo> columns;

        /// <summary>
        ///     初始化 <see cref="EntityMapperTypeInfo" /> 的新实例。
        /// </summary>
        public EntityMapperTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            bool isStruct,
            string tableName,
            List<ColumnInfo> columns)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            is_struct = isStruct;
            table_name = tableName;
            this.columns = columns;
        }
    }

    /// <summary>
    ///     列映射信息。
    /// </summary>
    internal readonly struct ColumnInfo
    {
        /// <summary>
        ///     成员名称。
        /// </summary>
        public readonly string member_name;

        /// <summary>
        ///     列名称。
        /// </summary>
        public readonly string column_name;

        /// <summary>
        ///     存储类型。
        /// </summary>
        public readonly MappedStorageType storage_type;

        /// <summary>
        ///     是否允许为空。
        /// </summary>
        public readonly bool is_nullable;

        /// <summary>
        ///     是否为主键。
        /// </summary>
        public readonly bool is_primary_key;

        /// <summary>
        ///     是否具有唯一约束。
        /// </summary>
        public readonly bool is_unique;

        /// <summary>
        ///     是否已建立索引。
        /// </summary>
        public readonly bool is_indexed;

        /// <summary>
        ///     是否为版本/并发检查字段。
        /// </summary>
        public readonly bool is_version;

        /// <summary>
        ///     默认值。
        /// </summary>
        public readonly object? default_value;

        /// <summary>
        ///     初始化 <see cref="ColumnInfo" /> 的新实例。
        /// </summary>
        public ColumnInfo(
            string memberName,
            string columnName,
            MappedStorageType storageType,
            bool isNullable,
            bool isPrimaryKey,
            bool isUnique,
            bool isIndexed,
            bool isVersion,
            object? defaultValue)
        {
            member_name = memberName;
            column_name = columnName;
            storage_type = storageType;
            is_nullable = isNullable;
            is_primary_key = isPrimaryKey;
            is_unique = isUnique;
            is_indexed = isIndexed;
            is_version = isVersion;
            default_value = defaultValue;
        }
    }

    /// <summary>
    ///     映射存储类型枚举，与 <c>Sonic.Standard.Data.Storage.StorageType</c> 对应。
    /// </summary>
    internal enum MappedStorageType
    {
        /// <summary>
        ///     32 位有符号整数。
        /// </summary>
        int32 = 0,

        /// <summary>
        ///     64 位有符号整数。
        /// </summary>
        int64 = 1,

        /// <summary>
        ///     字符串。
        /// </summary>
        @string = 2,

        /// <summary>
        ///     64 位双精度浮点数。
        /// </summary>
        float64 = 3,

        /// <summary>
        ///     布尔值。
        /// </summary>
        boolean = 4,

        /// <summary>
        ///     二进制大对象。
        /// </summary>
        blob = 5,

        /// <summary>
        ///     日期时间。
        /// </summary>
        date_time = 6,

        /// <summary>
        ///     全局唯一标识符。
        /// </summary>
        guid = 7
    }

    #endregion
}
