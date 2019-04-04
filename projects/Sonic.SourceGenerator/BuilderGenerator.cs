using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[GenerateBuilder]</c> 特性的类型自动生成 Builder 类，
///     为每个标记了 <c>[With]</c> 特性的成员生成 <c>WithXxx</c> 方法，
///     并生成 <c>Build</c> 方法创建目标类型实例。
/// </summary>
[Generator]
public sealed class BuilderGenerator : IIncrementalGenerator
{
    private const string _generate_builder_attribute_full_name = "Sonic.Standard.DI.Builder.GenerateBuilderAttribute";
    private const string _with_attribute_full_name = "Sonic.Standard.DI.Builder.WithAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _generate_builder_attribute_full_name,
                static (node, _) => node is TypeDeclarationSyntax typeDecl &&
                                    typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_builder(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取需要生成 Builder 的类型信息。
    /// </summary>
    private static BuilderTypeInfo? transform_builder(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var withMembers = extract_with_members(typeSymbol);

        if (withMembers.Count == 0) return null;

        return new BuilderTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            withMembers);
    }

    /// <summary>
    ///     提取类型中所有标记了 <c>[With]</c> 特性的成员信息。
    /// </summary>
    private static List<WithMemberInfo> extract_with_members(INamedTypeSymbol typeSymbol)
    {
        var members = new List<WithMemberInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsStatic) continue;

            if (member.DeclaredAccessibility != Accessibility.Public) continue;

            var withAttr = member.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_with_attribute_full_name}");

            if (withAttr is null) continue;

            string memberName;
            ITypeSymbol memberType;
            bool isRequired;

            if (member is IPropertySymbol prop)
            {
                memberName = prop.Name;
                memberType = prop.Type;
                isRequired = prop.IsRequired;
            }
            else if (member is IFieldSymbol field)
            {
                memberName = field.Name;
                memberType = field.Type;
                isRequired = field.IsRequired;
            }
            else
            {
                continue;
            }

            string? withName = null;

            foreach (var named in withAttr.NamedArguments)
                if (named is { Key: "name", Value.Value: string n })
                    withName = n;

            var defaultExpr = get_default_expression(memberType);

            members.Add(new WithMemberInfo(
                memberName,
                memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                isRequired,
                withName,
                defaultExpr));
        }

        return members;
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

        if (type.SpecialType == SpecialType.System_String) return "string.Empty";

        if (type.SpecialType == SpecialType.System_Char) return "'\\0'";

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return "null";

        return $"default({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
    }

    /// <summary>
    ///     生成所有类型的 Builder 源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<BuilderTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_builder_source(info);
            var hintName = $"{info.type_name}.Builder.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的 Builder 类源代码。
    /// </summary>
    private static string generate_builder_source(BuilderTypeInfo info)
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

        var builderClassName = $"{info.type_name}Builder";
        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{info.type_name}</c> 的构建器类，提供流式 API 设置各成员值并构建实例。");
        sb.append_line("/// </summary>");
        sb.append_line($"public sealed class {builderClassName}");
        using (sb.block())
        {
            generate_builder_fields(sb, info);
            sb.append_line();
            generate_builder_constructor(sb, info);
            sb.append_line();
            generate_with_methods(sb, info);
            sb.append_line();
            generate_build_method(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 Builder 类的私有字段。
    /// </summary>
    private static void generate_builder_fields(SourceTextBuilder sb, BuilderTypeInfo info)
    {
        for (var i = 0; i < info.members.Count; i++)
        {
            if (i > 0) sb.append_line();

            var member = info.members[i];
            sb.append_line("/// <summary>");
            sb.append_line($"/// <c>{member.member_name}</c> 的后备字段。");
            sb.append_line("/// </summary>");
            sb.append_line($"private {member.fully_qualified_type} _{member.member_name};");
        }
    }

    /// <summary>
    ///     生成 Builder 类的构造函数。
    /// </summary>
    private static void generate_builder_constructor(SourceTextBuilder sb, BuilderTypeInfo info)
    {
        var builderClassName = $"{info.type_name}Builder";
        sb.append_line("/// <summary>");
        sb.append_line($"/// 初始化 <c>{builderClassName}</c> 的新实例，所有字段初始化为默认值。");
        sb.append_line("/// </summary>");
        sb.append_line($"public {builderClassName}()");
        using (sb.block())
        {
            foreach (var member in info.members)
                sb.append_line($"_{member.member_name} = {member.default_expression};");
        }
    }

    /// <summary>
    ///     生成 Builder 类的 <c>WithXxx</c> 方法。
    /// </summary>
    private static void generate_with_methods(SourceTextBuilder sb, BuilderTypeInfo info)
    {
        for (var i = 0; i < info.members.Count; i++)
        {
            if (i > 0) sb.append_line();

            var member = info.members[i];
            var methodName = member.with_name ?? $"With{member.member_name}";

            sb.append_line("/// <summary>");
            sb.append_line($"/// 设置 <c>{member.member_name}</c> 的值。");
            sb.append_line("/// </summary>");
            sb.append_line($"/// <param name=\"value\"><c>{member.member_name}</c> 的新值。</param>");
            sb.append_line("/// <returns>当前构建器实例，支持链式调用。</returns>");
            sb.append_line($"public {info.type_name}Builder {methodName}({member.fully_qualified_type} value)");
            using (sb.block())
            {
                sb.append_line($"_{member.member_name} = value;");
                sb.append_line("return this;");
            }
        }
    }

    /// <summary>
    ///     生成 Builder 类的 <c>Build</c> 方法。
    /// </summary>
    private static void generate_build_method(SourceTextBuilder sb, BuilderTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 使用当前设置的字段值构建 <c>{info.type_name}</c> 实例。");
        sb.append_line("/// </summary>");
        sb.append_line($"/// <returns>构建的 <c>{info.type_name}</c> 实例。</returns>");
        sb.append_line($"public {info.fully_qualified_name} Build()");
        using (sb.block())
        {
            sb.append_line($"return new {info.fully_qualified_name}");
            using (sb.block(";"))
            {
                foreach (var member in info.members) sb.append_line($"{member.member_name} = _{member.member_name},");
            }
        }
    }

    #region 数据模型

    /// <summary>
    ///     需要生成 Builder 的目标类型信息。
    /// </summary>
    internal readonly struct BuilderTypeInfo
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
        ///     标记了 <c>[With]</c> 特性的成员列表。
        /// </summary>
        public readonly List<WithMemberInfo> members;

        /// <summary>
        ///     初始化 <see cref="BuilderTypeInfo" /> 的新实例。
        /// </summary>
        public BuilderTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            bool isStruct,
            List<WithMemberInfo> members)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            is_struct = isStruct;
            this.members = members;
        }
    }

    /// <summary>
    ///     标记了 <c>[With]</c> 特性的成员信息。
    /// </summary>
    internal readonly struct WithMemberInfo
    {
        /// <summary>
        ///     成员名称。
        /// </summary>
        public readonly string member_name;

        /// <summary>
        ///     成员类型的完全限定名称。
        /// </summary>
        public readonly string fully_qualified_type;

        /// <summary>
        ///     是否为必填成员。
        /// </summary>
        public readonly bool is_required;

        /// <summary>
        ///     自定义 <c>With</c> 方法名称，为 <c>null</c> 时使用 <c>With{MemberName}</c>。
        /// </summary>
        public readonly string? with_name;

        /// <summary>
        ///     默认值表达式。
        /// </summary>
        public readonly string default_expression;

        /// <summary>
        ///     初始化 <see cref="WithMemberInfo" /> 的新实例。
        /// </summary>
        public WithMemberInfo(
            string memberName,
            string fullyQualifiedType,
            bool isRequired,
            string? withName,
            string defaultExpression)
        {
            member_name = memberName;
            fully_qualified_type = fullyQualifiedType;
            is_required = isRequired;
            with_name = withName;
            default_expression = defaultExpression;
        }
    }

    #endregion
}