using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator.Config;

/// <summary>
///     增量源代码生成器，扫描 <c>[Config]</c> 特性并自动生成强类型配置绑定代码。
///     生成内容包括：Builder 模式工厂、IConfigurable 实现、验证、Schema、深克隆、安全输出。
/// </summary>
[Generator]
public sealed class ConfigGenerator : IIncrementalGenerator
{
    private const string _config_attribute_full_name = "Sonic.Standard.Config.ConfigAttribute";
    private const string _property_attribute_full_name = "Sonic.Standard.Config.PropertyAttribute";
    private const string _append_attribute_full_name = "Sonic.Standard.Config.AppendAttribute";
    private const string _overwrite_attribute_full_name = "Sonic.Standard.Config.OverwriteAttribute";
    private const string _required_attribute_full_name = "Sonic.Standard.Config.RequiredAttribute";
    private const string _sensitive_attribute_full_name = "Sonic.Standard.Data.SensitiveAttribute";

    private const string _collection_not_empty_attribute_full_name =
        "Sonic.Standard.Config.CollectionNotEmptyAttribute";

    private const string _uri_attribute_full_name = "Sonic.Standard.Config.UriAttribute";
    private const string _enum_attribute_full_name = "Sonic.Standard.Config.EnumAttribute";
    private const string _range_attribute_full_name = "Sonic.Standard.Data.RangeAttribute";
    private const string _regex_attribute_full_name = "Sonic.Standard.Data.RegexAttribute";

    private static readonly SymbolDisplayFormat _fully_qualified_format =
        SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _config_attribute_full_name,
                static (node, _) =>
                    node is ClassDeclarationSyntax,
                static (ctx, ct) => transform(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(configTypes, generate_source);
    }

    #region 工厂方法生成

    /// <summary>
    ///     生成静态工厂方法 <c>From</c>、<c>FromJson</c>、<c>FromEnvironment</c>。
    /// </summary>
    private static void generate_factory_methods(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("#region 工厂方法");
        sb.append_line();

        sb.append_line("/// <summary>");
        sb.append_line($"/// 从配置提供程序创建 <see cref=\"{info.type_name}\"/> 的构建器。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"provider\">配置提供程序。</param>");
        sb.append_line($"/// <returns>用于构建 <see cref=\"{info.type_name}\"/> 实例的构建器。</returns>");
        sb.append_line("public static Builder From(global::Sonic.Standard.Config.IConfigProvider provider)");
        using (sb.block())
        {
            sb.append_line("return new Builder(provider);");
        }

        sb.append_line();

        sb.append_line("/// <summary>");
        sb.append_line("/// 从 JSON 文件创建构建器。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"path\">JSON 文件路径。</param>");
        sb.append_line("/// <returns>用于构建实例的构建器。</returns>");
        sb.append_line("public static Builder FromJson(string path)");
        using (sb.block())
        {
            sb.append_line("var provider = new global::Sonic.Standard.Config.JsonConfigProvider(path);");
            sb.append_line("return new Builder(provider);");
        }

        sb.append_line();

        sb.append_line("/// <summary>");
        sb.append_line("/// 从环境变量创建构建器。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"prefix\">环境变量前缀，默认为 <c>\"APP_\"</c>。</param>");
        sb.append_line("/// <returns>用于构建实例的构建器。</returns>");
        sb.append_line("public static Builder FromEnvironment(string prefix = \"APP_\")");
        using (sb.block())
        {
            sb.append_line("var provider = new global::Sonic.Standard.Config.EnvironmentConfigProvider(prefix);");
            sb.append_line("return new Builder(provider);");
        }

        sb.append_line();
        sb.append_line("#endregion");
        sb.append_line();
    }

    #endregion

    #region ResetToDefaults 生成

    /// <summary>
    ///     生成 <c>IConfigurable.ResetToDefaults()</c> 方法实现。
    /// </summary>
    private static void generate_reset_to_defaults_method(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("#region IConfigurable.ResetToDefaults");
        sb.append_line();

        sb.append_line("/// <summary>");
        sb.append_line("/// 将所有字段重置为编译时确定的默认值。");
        sb.append_line("/// </summary>");
        sb.append_line("public void ResetToDefaults()");
        using (sb.block())
        {
            foreach (var prop in info.properties)
                if (prop.default_value_expr != null)
                    sb.append_line($"{prop.name} = {prop.default_value_expr};");
                else if (prop.is_value_type)
                    sb.append_line($"{prop.name} = default({prop.fully_qualified_type});");
                else if (prop.list_element_type != null)
                    sb.append_line(
                        $"{prop.name} = new global::System.Collections.Generic.List<{prop.list_element_type}>();");
                else if (prop.dict_value_type != null)
                    sb.append_line(
                        $"{prop.name} = new global::System.Collections.Generic.Dictionary<string, {prop.dict_value_type}>();");
                else
                    sb.append_line($"{prop.name} = default({prop.fully_qualified_type});");
        }

        sb.append_line();
        sb.append_line("#endregion");
        sb.append_line();
    }

    #endregion

    #region ToSafeString 生成

    /// <summary>
    ///     生成 <c>ToSafeString()</c> 方法，遮蔽 [Sensitive] 属性。
    /// </summary>
    private static void generate_to_safe_string_method(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("#region 安全日志");
        sb.append_line();

        sb.append_line("/// <summary>");
        sb.append_line("/// 生成安全文本表示，敏感字段将被遮蔽为 <c>\"***\"</c>。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <returns>不含敏感信息的文本表示。</returns>");
        sb.append_line("public string ToSafeString()");
        using (sb.block())
        {
            sb.append("return $\"");

            var parts = new List<string>();
            foreach (var prop in info.properties)
                if (prop.is_sensitive)
                    parts.Add($"{prop.name}=***");
                else
                    parts.Add($"{prop.name}={{{prop.name}}}");

            sb.append(string.Join(", ", parts));
            sb.append_line("\";");
        }

        sb.append_line();
        sb.append_line("#endregion");
        sb.append_line();
    }

    #endregion

    #region 语义变换

    /// <summary>
    ///     从 <c>[Config]</c> 特性的语法节点提取完整的类型信息。
    /// </summary>
    private static ConfigTypeInfo? transform(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var attr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(_fully_qualified_format) ==
            $"global::{_config_attribute_full_name}");

        if (attr is null) return null;

        var sectionName = extract_section_name(attr, typeSymbol.Name);
        var namingConvention = extract_naming_convention(attr);

        var properties = ImmutableArray.CreateBuilder<ConfigPropertyInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            if (member is not IPropertySymbol prop) continue;

            if (prop.IsStatic || prop.IsIndexer || prop.IsWriteOnly) continue;

            if (prop.DeclaredAccessibility != Accessibility.Public) continue;

            if (prop.SetMethod is null || prop.SetMethod.DeclaredAccessibility != Accessibility.Public) continue;

            var propInfo = extract_property_info(prop, namingConvention);
            properties.Add(propInfo);
        }

        if (properties.Count == 0) return null;

        return new ConfigTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(_fully_qualified_format),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            sectionName,
            namingConvention,
            properties.ToImmutable());
    }

    /// <summary>
    ///     从 <c>[Config]</c> 特性提取 section，未指定时默认为类名 camelCase。
    /// </summary>
    private static string extract_section_name(AttributeData attr, string typeName)
    {
        foreach (var namedArg in attr.NamedArguments)
            if (namedArg is { Key: "section", Value.Value: string s })
                return s;

        return NamingHelper.to_section_name(typeName);
    }

    /// <summary>
    ///     从 <c>[Config]</c> 特性提取 naming，默认为 CamelCase。
    /// </summary>
    private static NamingConvention extract_naming_convention(AttributeData attr)
    {
        foreach (var namedArg in attr.NamedArguments)
            if (namedArg is { Key: "naming", Value.Value: int val })
                return (NamingConvention)val;

        return NamingConvention.camel_case;
    }

    /// <summary>
    ///     从属性符号提取完整的配置属性信息。
    /// </summary>
    private static ConfigPropertyInfo extract_property_info(
        IPropertySymbol prop, NamingConvention convention)
    {
        var fqType = prop.Type.ToDisplayString(_fully_qualified_format);
        var isValueType = prop.Type.IsValueType;
        var isNullable = is_nullable(prop.Type);

        var configKey = extract_config_key(prop, convention);
        var mergeStrategy = infer_merge_strategy(prop, prop.Type);
        var isSensitive = has_attribute(prop, _sensitive_attribute_full_name);
        var isRequired = has_attribute(prop, _required_attribute_full_name);
        var defaultExpr = extract_default_value(prop);
        var validations = extract_validations(prop);

        if (isRequired && !validations.Any(v => v.kind == ValidationKind.required))
        {
            var builder = ImmutableArray.CreateBuilder<ValidationInfo>();
            builder.Add(new ValidationInfo(ValidationKind.required));
            builder.AddRange(validations);
            validations = builder.ToImmutable();
        }

        var isConfigType = has_attribute(prop.Type, _config_attribute_full_name);
        var listElementType = extract_list_element_type(prop.Type);
        var dictValueType = extract_dict_value_type(prop.Type);

        return new ConfigPropertyInfo(
            prop.Name,
            fqType,
            isValueType,
            isNullable,
            configKey,
            mergeStrategy,
            isSensitive,
            isRequired,
            defaultExpr,
            validations,
            isConfigType,
            listElementType,
            dictValueType);
    }

    /// <summary>
    ///     提取属性的配置键名，优先使用 [Property] 覆盖，否则按命名约定转换。
    /// </summary>
    private static string extract_config_key(IPropertySymbol prop, NamingConvention convention)
    {
        foreach (var attr in prop.GetAttributes())
            if (attr.AttributeClass?.ToDisplayString(_fully_qualified_format) ==
                $"global::{_property_attribute_full_name}")
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string name)
                    return name;

        return NamingHelper.apply_convention(prop.Name, convention);
    }

    /// <summary>
    ///     推断属性的合并策略：简单类型→Overwrite，List→Overwrite，Dictionary→ShallowMerge，嵌套 IConfigurable→DeepMerge。
    ///     可被 [Append]/[Overwrite] 覆盖。
    /// </summary>
    private static CollectionMergeStrategy infer_merge_strategy(IPropertySymbol prop, ITypeSymbol type)
    {
        if (has_attribute(prop, _append_attribute_full_name)) return CollectionMergeStrategy.append;

        if (has_attribute(prop, _overwrite_attribute_full_name)) return CollectionMergeStrategy.overwrite;

        if (is_dictionary_type(type)) return CollectionMergeStrategy.shallow_merge;

        if (has_attribute(type, _config_attribute_full_name)) return CollectionMergeStrategy.deep_merge;

        return CollectionMergeStrategy.overwrite;
    }

    /// <summary>
    ///     提取属性初始化器的默认值表达式。
    /// </summary>
    private static string? extract_default_value(IPropertySymbol prop)
    {
        foreach (var decl in prop.DeclaringSyntaxReferences)
        {
            var syntax = decl.GetSyntax();

            if (syntax is PropertyDeclarationSyntax { Initializer: not null } pds)
                return pds.Initializer.Value.ToString();
        }

        return null;
    }

    /// <summary>
    ///     提取属性上的所有验证特性信息。
    /// </summary>
    private static ImmutableArray<ValidationInfo> extract_validations(IPropertySymbol prop)
    {
        var builder = ImmutableArray.CreateBuilder<ValidationInfo>();

        foreach (var attr in prop.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString(_fully_qualified_format);

            if (attrName == $"global::{_required_attribute_full_name}")
            {
                builder.Add(new ValidationInfo(ValidationKind.required));
            }
            else if (attrName == $"global::{_range_attribute_full_name}")
            {
                if (attr.ConstructorArguments.Length >= 2)
                {
                    var min = attr.ConstructorArguments[0].Value is double d1 ? d1 : 0.0;
                    var max = attr.ConstructorArguments[1].Value is double d2 ? d2 : 0.0;
                    builder.Add(new ValidationInfo(min, max));
                }
            }
            else if (attrName == $"global::{_regex_attribute_full_name}")
            {
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is string pattern)
                    builder.Add(new ValidationInfo(pattern));
            }
            else if (attrName == $"global::{_collection_not_empty_attribute_full_name}")
            {
                builder.Add(new ValidationInfo(ValidationKind.collection_not_empty));
            }
            else if (attrName == $"global::{_uri_attribute_full_name}")
            {
                builder.Add(new ValidationInfo(ValidationKind.uri));
            }
            else if (attrName == $"global::{_enum_attribute_full_name}")
            {
                builder.Add(new ValidationInfo(ValidationKind.@enum));
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    ///     当类型为 <c>List&lt;T&gt;</c> 时，提取 T 的完全限定名。
    /// </summary>
    private static string? extract_list_element_type(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var def = named.OriginalDefinition;
            var defName = def.ToDisplayString(_fully_qualified_format);

            if (defName == "global::System.Collections.Generic.List<T>")
                return named.TypeArguments[0].ToDisplayString(_fully_qualified_format);
        }

        return null;
    }

    /// <summary>
    ///     当类型为 <c>Dictionary&lt;string,T&gt;</c> 时，提取 T 的完全限定名。
    /// </summary>
    private static string? extract_dict_value_type(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var def = named.OriginalDefinition;
            var defName = def.ToDisplayString(_fully_qualified_format);

            if (defName == "global::System.Collections.Generic.Dictionary<TKey, TValue>")
                return named.TypeArguments[1].ToDisplayString(_fully_qualified_format);
        }

        return null;
    }

    /// <summary>
    ///     检查符号是否包含指定全名特性。
    /// </summary>
    private static bool has_attribute(ISymbol symbol, string attributeFullName)
    {
        foreach (var attr in symbol.GetAttributes())
            if (attr.AttributeClass?.ToDisplayString(_fully_qualified_format) ==
                $"global::{attributeFullName}")
                return true;

        return false;
    }

    /// <summary>
    ///     判断类型是否为可空引用类型。
    /// </summary>
    private static bool is_nullable(ITypeSymbol type)
    {
        return type.NullableAnnotation == NullableAnnotation.Annotated;
    }

    /// <summary>
    ///     判断类型是否为 <c>Dictionary&lt;,&gt;</c>。
    /// </summary>
    private static bool is_dictionary_type(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var defName = named.OriginalDefinition.ToDisplayString(_fully_qualified_format);
            return defName == "global::System.Collections.Generic.Dictionary<TKey, TValue>";
        }

        return false;
    }

    #endregion

    #region 源代码输出

    /// <summary>
    ///     生成所有配置类型的源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context,
        ImmutableArray<ConfigTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_config_source(info);
            var hintName =
                $"{info.fully_qualified_name.Replace("global::", "").Replace('<', '_').Replace('>', '_')}.Config.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个配置类型的完整 C# 源代码。
    /// </summary>
    private static string generate_config_source(ConfigTypeInfo info)
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

        sb.append_line($"partial class {info.type_name}");
        using (sb.block())
        {
            generate_factory_methods(sb, info);
            generate_builder_class(sb, info);
            generate_validate_method(sb, info);
            generate_get_schema_method(sb, info);
            generate_clone_method(sb, info);
            generate_reset_to_defaults_method(sb, info);
            generate_to_safe_string_method(sb, info);
        }

        return sb.ToString();
    }

    #endregion

    #region Builder 类生成

    /// <summary>
    ///     生成嵌套 Builder 类，包含 Merge、WithoutValidation、Build 方法。
    /// </summary>
    private static void generate_builder_class(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("#region Builder");
        sb.append_line();

        sb.append_line("/// <summary>");
        sb.append_line($"/// <see cref=\"{info.type_name}\"/> 的构建器，支持多源合并与可选验证。");
        sb.append_line("/// </summary>");
        sb.append_line("public sealed class Builder");
        using (sb.block())
        {
            generate_builder_fields(sb, info);
            generate_builder_constructor(sb, info);
            generate_builder_merge(sb);
            generate_builder_without_validation(sb);
            generate_builder_build(sb, info);
            generate_builder_apply_source(sb, info);
        }

        sb.append_line();
        sb.append_line("#endregion");
        sb.append_line();
    }

    /// <summary>
    ///     生成 Builder 的私有字段。
    /// </summary>
    private static void generate_builder_fields(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line(
            "private readonly global::System.Collections.Generic.List<global::Sonic.Standard.Config.IConfigProvider> _sources = new();");
        sb.append_line("private bool _validate = true;");
        sb.append_line();
    }

    /// <summary>
    ///     生成 Builder 的构造函数。
    /// </summary>
    private static void generate_builder_constructor(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 使用初始配置提供程序初始化构建器。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"provider\">初始配置提供程序。</param>");
        sb.append_line("public Builder(global::Sonic.Standard.Config.IConfigProvider provider)");
        using (sb.block())
        {
            sb.append_line("_sources.Add(provider);");
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成 Builder.Merge 方法。
    /// </summary>
    private static void generate_builder_merge(SourceTextBuilder sb)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 合并额外的配置提供程序，后添加的源优先级更高。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"providers\">要合并的配置提供程序。</param>");
        sb.append_line("/// <returns>当前构建器实例。</returns>");
        sb.append_line("public Builder Merge(params global::Sonic.Standard.Config.IConfigProvider[] providers)");
        using (sb.block())
        {
            sb.append_line("foreach (var p in providers)");
            using (sb.block())
            {
                sb.append_line("_sources.Add(p);");
            }

            sb.append_line("return this;");
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成 Builder.WithoutValidation 方法。
    /// </summary>
    private static void generate_builder_without_validation(SourceTextBuilder sb)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 禁用构建时验证。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <returns>当前构建器实例。</returns>");
        sb.append_line("public Builder WithoutValidation()");
        using (sb.block())
        {
            sb.append_line("_validate = false;");
            sb.append_line("return this;");
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成 Builder.Build 方法。
    /// </summary>
    private static void generate_builder_build(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 构建 <see cref=\"{info.type_name}\"/> 实例，依次应用所有配置源。");
        sb.append_line("/// </summary>");
        sb.append_line($"/// <returns>填充完毕的 <see cref=\"{info.type_name}\"/> 实例。</returns>");
        sb.append_line($"public {info.type_name} Build()");
        using (sb.block())
        {
            sb.append_line($"var target = new {info.type_name}();");
            sb.append_line("target.ResetToDefaults();");
            sb.append_line();
            sb.append_line("foreach (var source in _sources)");
            using (sb.block())
            {
                sb.append_line("var node = source.Load();");
                sb.append_line("ApplySource(target, node);");
            }

            sb.append_line();
            sb.append_line("if (_validate)");
            using (sb.block())
            {
                sb.append_line("var errors = target.Validate();");
                sb.append_line("if (errors.Count > 0)");
                using (sb.block())
                {
                    sb.append_line("throw new global::System.InvalidOperationException(");
                    sb.append_line("    $\"配置验证失败：{string.Join(\"; \", errors)}\");");
                }
            }

            sb.append_line();
            sb.append_line("return target;");
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成 Builder.ApplySource 方法，按属性类型生成不同的配置值应用逻辑。
    /// </summary>
    private static void generate_builder_apply_source(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 将配置节点树中的值应用到目标实例的对应属性。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"target\">目标配置实例。</param>");
        sb.append_line("/// <param name=\"source\">配置节点树。</param>");
        sb.append_line(
            $"private static void ApplySource({info.type_name} target, global::Sonic.Standard.Config.ConfigNode source)");
        using (sb.block())
        {
            sb.append_line(
                "if (source == null || source.type != global::Sonic.Standard.Config.ConfigNode.NodeType.Object)");
            using (sb.block())
            {
                sb.append_line("return;");
            }

            sb.append_line();

            foreach (var prop in info.properties) generate_apply_property(sb, prop);
        }

        sb.append_line();
    }

    /// <summary>
    ///     为单个属性生成 ApplySource 中的赋值逻辑。
    /// </summary>
    private static void generate_apply_property(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        sb.append_line($"var _{prop.name}Node = source.get_field(\"{prop.config_key}\");");

        if (prop.is_config_type)
            generate_apply_nested_config(sb, prop);
        else if (prop.list_element_type != null)
            generate_apply_list(sb, prop);
        else if (prop.dict_value_type != null)
            generate_apply_dictionary(sb, prop);
        else
            generate_apply_simple(sb, prop);

        sb.append_line();
    }

    /// <summary>
    ///     生成简单类型属性的配置值应用逻辑。
    /// </summary>
    private static void generate_apply_simple(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        var conversion = get_scalar_conversion(prop, $"_{prop.name}Node");
        sb.append_line($"if (_{prop.name}Node != null)");
        using (sb.block())
        {
            if (prop.is_nullable || !prop.is_value_type)
                sb.append_line($"target.{prop.name} = {conversion};");
            else
                sb.append_line(
                    $"target.{prop.name} = _{prop.name}Node.type == global::Sonic.Standard.Config.ConfigNode.NodeType.Scalar ? {conversion} : target.{prop.name};");
        }
    }

    /// <summary>
    ///     生成 List&lt;T&gt; 类型属性的配置值应用逻辑。
    /// </summary>
    private static void generate_apply_list(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        var elemType = prop.list_element_type!;
        var elemConversion = get_element_conversion(elemType, "_item");

        sb.append_line(
            $"if (_{prop.name}Node != null && _{prop.name}Node.type == global::Sonic.Standard.Config.ConfigNode.NodeType.Array)");

        if (prop.merge_strategy == CollectionMergeStrategy.append)
            using (sb.block())
            {
                sb.append_line($"foreach (var _item in _{prop.name}Node.EnumerateArray())");
                using (sb.block())
                {
                    sb.append_line($"target.{prop.name}.Add({elemConversion});");
                }
            }
        else
            using (sb.block())
            {
                sb.append_line(
                    $"target.{prop.name} = new global::System.Collections.Generic.List<{elemType}>(_{prop.name}Node.EnumerateArray().Select(_item => {elemConversion}));");
            }
    }

    /// <summary>
    ///     生成 Dictionary&lt;string,T&gt; 类型属性的配置值应用逻辑。
    /// </summary>
    private static void generate_apply_dictionary(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        var valType = prop.dict_value_type!;
        var valConversion = get_element_conversion(valType, "_fieldValue");

        sb.append_line(
            $"if (_{prop.name}Node != null && _{prop.name}Node.type == global::Sonic.Standard.Config.ConfigNode.NodeType.Object)");

        if (prop.merge_strategy == CollectionMergeStrategy.shallow_merge)
            using (sb.block())
            {
                sb.append_line($"foreach (var _kvp in _{prop.name}Node.EnumerateFields())");
                using (sb.block())
                {
                    sb.append_line("var _fieldValue = _kvp.Value;");
                    sb.append_line($"target.{prop.name}[_kvp.Key] = {valConversion};");
                }
            }
        else
            using (sb.block())
            {
                sb.append_line(
                    $"target.{prop.name} = new global::System.Collections.Generic.Dictionary<string, {valType}>();");
                sb.append_line($"foreach (var _kvp in _{prop.name}Node.EnumerateFields())");
                using (sb.block())
                {
                    sb.append_line("var _fieldValue = _kvp.Value;");
                    sb.append_line($"target.{prop.name}[_kvp.Key] = {valConversion};");
                }
            }
    }

    /// <summary>
    ///     生成嵌套 [Config] 类型属性的配置值应用逻辑。
    /// </summary>
    private static void generate_apply_nested_config(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        sb.append_line($"if (_{prop.name}Node != null)");

        if (prop.merge_strategy == CollectionMergeStrategy.deep_merge)
            using (sb.block())
            {
                sb.append_line($"if (target.{prop.name} != null)");
                using (sb.block())
                {
                    sb.append_line($"ApplySource(target.{prop.name}, _{prop.name}Node);");
                }

                sb.append_line("else");
                using (sb.block())
                {
                    sb.append_line($"target.{prop.name} = new {prop.fully_qualified_type}();");
                    sb.append_line($"ApplySource(target.{prop.name}, _{prop.name}Node);");
                }
            }
        else
            using (sb.block())
            {
                sb.append_line($"target.{prop.name} = new {prop.fully_qualified_type}();");
                sb.append_line($"ApplySource(target.{prop.name}, _{prop.name}Node);");
            }
    }

    /// <summary>
    ///     获取从 ConfigNode 到标量类型的转换表达式。
    /// </summary>
    private static string get_scalar_conversion(ConfigPropertyInfo prop, string nodeVar)
    {
        var type = prop.fully_qualified_type;
        var unwrapped = type.Replace("?", "");

        if (unwrapped == "string") return $"{nodeVar}.AsString() ?? string.Empty";

        if (unwrapped == "int") return $"{nodeVar}.AsInt32()";

        if (unwrapped == "long") return $"{nodeVar}.AsInt64()";

        if (unwrapped == "bool") return $"{nodeVar}.AsBoolean()";

        if (unwrapped == "double") return $"{nodeVar}.AsDouble()";

        if (unwrapped == "float") return $"{nodeVar}.AsFloat()";

        if (unwrapped == "decimal") return $"{nodeVar}.AsDecimal()";

        if (unwrapped == "global::System.TimeSpan")
            return $"global::System.TimeSpan.Parse({nodeVar}.AsString() ?? \"00:00:00\")";

        if (unwrapped == "global::System.DateTime")
            return
                $"global::System.DateTime.Parse({nodeVar}.AsString() ?? global::System.DateTime.MinValue.ToString())";

        if (unwrapped == "global::System.Uri")
            return $"new global::System.Uri({nodeVar}.AsString() ?? \"about:blank\")";

        return
            $"({prop.fully_qualified_type})global::System.Enum.Parse(typeof({unwrapped}), {nodeVar}.AsString() ?? string.Empty, true)";
    }

    /// <summary>
    ///     获取从 ConfigNode 数组元素到目标元素类型的转换表达式。
    /// </summary>
    private static string get_element_conversion(string elementType, string itemVar)
    {
        if (elementType == "string") return $"{itemVar}.AsString() ?? string.Empty";

        if (elementType == "int") return $"{itemVar}.AsInt32()";

        if (elementType == "long") return $"{itemVar}.AsInt64()";

        if (elementType == "bool") return $"{itemVar}.AsBoolean()";

        if (elementType == "double") return $"{itemVar}.AsDouble()";

        if (elementType == "float") return $"{itemVar}.AsFloat()";

        if (elementType == "decimal") return $"{itemVar}.AsDecimal()";

        return
            $"({elementType})global::System.Enum.Parse(typeof({elementType}), {itemVar}.AsString() ?? string.Empty, true)";
    }

    #endregion

    #region Validate 生成

    /// <summary>
    ///     生成 <c>IConfigurable.Validate()</c> 方法实现。
    /// </summary>
    private static void generate_validate_method(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("#region IConfigurable.Validate");
        sb.append_line();

        sb.append_line("/// <summary>");
        sb.append_line("/// 验证当前配置，返回所有错误消息。空集合表示验证通过。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <returns>验证错误列表，空集合表示合法。</returns>");
        sb.append_line("public global::System.Collections.Generic.IReadOnlyList<string> Validate()");
        using (sb.block())
        {
            sb.append_line("var _errors = new global::System.Collections.Generic.List<string>();");
            sb.append_line();

            foreach (var prop in info.properties) generate_validate_property(sb, prop);

            sb.append_line("return _errors;");
        }

        sb.append_line();
        sb.append_line("#endregion");
        sb.append_line();
    }

    /// <summary>
    ///     为单个属性生成验证逻辑。
    /// </summary>
    private static void generate_validate_property(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        foreach (var v in prop.validations)
            switch (v.kind)
            {
                case ValidationKind.required:
                {
                    generate_validate_required(sb, prop);
                    break;
                }
                case ValidationKind.range:
                {
                    generate_validate_range(sb, prop, v.range_min, v.range_max);
                    break;
                }
                case ValidationKind.regex:
                {
                    generate_validate_regex(sb, prop, v.regex_pattern ?? "");
                    break;
                }
                case ValidationKind.collection_not_empty:
                {
                    generate_validate_collection_not_empty(sb, prop);
                    break;
                }
                case ValidationKind.uri:
                {
                    generate_validate_uri(sb, prop);
                    break;
                }
                case ValidationKind.@enum:
                {
                    generate_validate_enum(sb, prop);
                    break;
                }
            }

        if (prop.is_config_type)
        {
            sb.append_line($"if ({prop.name} is global::Sonic.Standard.Config.IConfigurable _nested{prop.name})");
            using (sb.block())
            {
                sb.append_line($"_errors.AddRange(_nested{prop.name}.Validate());");
            }

            sb.append_line();
        }
    }

    /// <summary>
    ///     生成 [Required] 验证逻辑。
    /// </summary>
    private static void generate_validate_required(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        if (prop is { is_value_type: true, is_nullable: false })
        {
            sb.append_line($"if ({prop.name} == default({prop.fully_qualified_type}))");
            using (sb.block())
            {
                sb.append_line($"_errors.Add(\"属性 {prop.name} 是必填项，但不能为默认值\");");
            }
        }
        else
        {
            sb.append_line($"if ({prop.name} is null || ({prop.name} is string s && string.IsNullOrEmpty(s)))");
            using (sb.block())
            {
                sb.append_line($"_errors.Add(\"属性 {prop.name} 是必填项，但不能为空\");");
            }
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成 [Range] 验证逻辑。
    /// </summary>
    private static void generate_validate_range(SourceTextBuilder sb, ConfigPropertyInfo prop,
        double min, double max)
    {
        sb.append_line(
            $"if ((double){prop.name} < {min.ToString(CultureInfo.InvariantCulture)} || (double){prop.name} > {max.ToString(CultureInfo.InvariantCulture)})");
        using (sb.block())
        {
            sb.append_line(
                $"_errors.Add($\"属性 {prop.name} 的值 {{{prop.name}}} 不在范围 [{min.ToString(CultureInfo.InvariantCulture)}, {max.ToString(CultureInfo.InvariantCulture)}] 内\");");
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成 [Regex] 验证逻辑。
    /// </summary>
    private static void generate_validate_regex(SourceTextBuilder sb, ConfigPropertyInfo prop,
        string pattern)
    {
        var escapedPattern = pattern.Replace("\"", "\"\"");
        sb.append_line(
            $"if (!global::System.Text.RegularExpressions.Regex.IsMatch({prop.name} ?? string.Empty, \"{escapedPattern}\"))");
        using (sb.block())
        {
            sb.append_line($"_errors.Add($\"属性 {prop.name} 的值不匹配正则表达式 {escapedPattern}\");");
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成 [CollectionNotEmpty] 验证逻辑。
    /// </summary>
    private static void generate_validate_collection_not_empty(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        sb.append_line($"if ({prop.name} == null || {prop.name}.Count == 0)");
        using (sb.block())
        {
            sb.append_line($"_errors.Add(\"属性 {prop.name} 的集合不能为空\");");
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成 [Uri] 验证逻辑。
    /// </summary>
    private static void generate_validate_uri(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        sb.append_line(
            $"if (!global::System.Uri.IsWellFormedUriString({prop.name}?.ToString() ?? string.Empty, global::System.UriKind.Absolute))");
        using (sb.block())
        {
            sb.append_line($"_errors.Add($\"属性 {prop.name} 的值 {{{prop.name}}} 不是有效的 URI\");");
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成 [Enum] 验证逻辑。
    /// </summary>
    private static void generate_validate_enum(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        var unwrapped = prop.fully_qualified_type.Replace("?", "");
        sb.append_line($"if (!global::System.Enum.IsDefined(typeof({unwrapped}), {prop.name}))");
        using (sb.block())
        {
            sb.append_line($"_errors.Add($\"属性 {prop.name} 的值 {{{prop.name}}} 不在枚举 {unwrapped} 的定义范围内\");");
        }

        sb.append_line();
    }

    #endregion

    #region GetSchema 生成

    /// <summary>
    ///     生成 <c>IConfigurable.GetSchema()</c> 方法实现，返回编译期生成的 JSON Schema 常量字符串。
    /// </summary>
    private static void generate_get_schema_method(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("#region IConfigurable.GetSchema");
        sb.append_line();

        var schemaJson = build_json_schema(info);
        var escapedSchema = schemaJson.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")
            .Replace("\r", "");

        sb.append_line("/// <summary>");
        sb.append_line("/// 返回该配置对应的 JSON Schema 字符串。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <returns>JSON Schema 字符串。</returns>");
        sb.append_line("public string GetSchema()");
        using (sb.block())
        {
            sb.append_line($"return \"{escapedSchema}\";");
        }

        sb.append_line();
        sb.append_line("#endregion");
        sb.append_line();
    }

    /// <summary>
    ///     根据属性类型和特性构建 JSON Schema 字符串。
    /// </summary>
    private static string build_json_schema(ConfigTypeInfo info)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"$schema\":\"http://json-schema.org/draft-07/schema#\",");
        sb.Append("\"type\":\"object\",");
        sb.Append($"\"title\":\"{info.type_name}\",");

        if (!string.IsNullOrEmpty(info.section_name)) sb.Append($"\"description\":\"配置节: {info.section_name}\",");

        sb.Append("\"properties\":{");

        var props = new List<string>();
        foreach (var prop in info.properties) props.Add(build_property_schema(prop));

        sb.Append(string.Join(",", props));
        sb.Append("}");

        var requiredProps = info.properties
            .Where(p => p.is_required)
            .Select(p => $"\"{p.config_key}\"")
            .ToList();

        if (requiredProps.Count > 0) sb.Append($",\"required\":[{string.Join(",", requiredProps)}]");

        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>
    ///     为单个属性构建 JSON Schema 属性片段。
    /// </summary>
    private static string build_property_schema(ConfigPropertyInfo prop)
    {
        var sb = new StringBuilder();
        sb.Append($"\"{prop.config_key}\":{{");

        if (prop.list_element_type != null)
        {
            sb.Append("\"type\":\"array\",");
            sb.Append($"\"items\":{{\"type\":\"{map_type_to_json_schema(prop.list_element_type)}\"}}");
        }
        else if (prop.dict_value_type != null)
        {
            sb.Append("\"type\":\"object\",");
            sb.Append($"\"additionalProperties\":{{\"type\":\"{map_type_to_json_schema(prop.dict_value_type)}\"}}");
        }
        else if (prop.is_config_type)
        {
            sb.Append("\"type\":\"object\",");
            sb.Append("\"description\":\"嵌套配置对象\"");
        }
        else
        {
            var jsonType = map_type_to_json_schema(prop.fully_qualified_type.Replace("?", ""));
            sb.Append($"\"type\":\"{jsonType}\"");
        }

        if (prop.is_sensitive) sb.Append(",\"writeOnly\":true");

        if (prop.default_value_expr != null)
        {
            var defaultStr = try_format_default_for_schema(prop.default_value_expr);
            if (defaultStr != null) sb.Append($",\"default\":{defaultStr}");
        }

        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>
    ///     将 C# 类型映射到 JSON Schema 类型字符串。
    /// </summary>
    private static string map_type_to_json_schema(string fqType)
    {
        if (fqType == "string") return "string";

        if (fqType is "int" or "long") return "integer";

        if (fqType is "double" or "float" or "decimal") return "number";

        if (fqType == "bool") return "boolean";

        return "string";
    }

    /// <summary>
    ///     尝试将默认值表达式格式化为 JSON Schema 的 default 值。
    /// </summary>
    private static string? try_format_default_for_schema(string expr)
    {
        if (expr.StartsWith("\"")) return expr;

        if (expr is "true" or "false") return expr;

        if (int.TryParse(expr, out _)) return expr;

        if (double.TryParse(expr, NumberStyles.Float,
                CultureInfo.InvariantCulture, out _))
            return expr;

        if (expr == "null") return "null";

        return null;
    }

    #endregion

    #region Clone 生成

    /// <summary>
    ///     生成 <c>IConfigurable.Clone()</c> 方法实现，深克隆所有属性。
    /// </summary>
    private static void generate_clone_method(SourceTextBuilder sb, ConfigTypeInfo info)
    {
        sb.append_line("#region IConfigurable.Clone");
        sb.append_line();

        sb.append_line("/// <summary>");
        sb.append_line("/// 创建当前配置的深克隆副本。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <returns>深克隆的配置对象。</returns>");
        sb.append_line("public object Clone()");
        using (sb.block())
        {
            sb.append_line($"var _clone = new {info.type_name}();");
            sb.append_line();

            foreach (var prop in info.properties) generate_clone_property(sb, prop);

            sb.append_line("return _clone;");
        }

        sb.append_line();
        sb.append_line("#endregion");
        sb.append_line();
    }

    /// <summary>
    ///     为单个属性生成深克隆赋值逻辑。
    /// </summary>
    private static void generate_clone_property(SourceTextBuilder sb, ConfigPropertyInfo prop)
    {
        if (prop.is_config_type)
            sb.append_line(
                $"_clone.{prop.name} = {prop.name} != null ? ({prop.fully_qualified_type})(({prop.fully_qualified_type}){prop.name}).Clone() : default;");
        else if (prop.list_element_type != null)
            sb.append_line(
                $"_clone.{prop.name} = {prop.name} != null ? new global::System.Collections.Generic.List<{prop.list_element_type}>({prop.name}) : new global::System.Collections.Generic.List<{prop.list_element_type}>();");
        else if (prop.dict_value_type != null)
            sb.append_line(
                $"_clone.{prop.name} = {prop.name} != null ? new global::System.Collections.Generic.Dictionary<string, {prop.dict_value_type}>({prop.name}) : new global::System.Collections.Generic.Dictionary<string, {prop.dict_value_type}>();");
        else
            sb.append_line($"_clone.{prop.name} = {prop.name};");
    }

    #endregion
}