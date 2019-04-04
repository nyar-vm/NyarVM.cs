using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Sonic.Data.Generator.Config;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Data]</c> 特性的类型自动生成 <c>IValidator&lt;T&gt;</c> 实现，
///     根据字段上的验证特性生成校验逻辑。
/// </summary>
[Generator]
public sealed class ValidatorGenerator : IIncrementalGenerator
{
    private const string _string_length_attribute_full_name = "Sonic.Standard.Data.StringLengthAttribute";
    private const string _range_attribute_full_name = nameof(ConfigPropertyInfo);
    private const string _regex_attribute_full_name = "Sonic.Standard.Data.RegexAttribute";
    private const string _enum_check_attribute_full_name = "Sonic.Standard.Data.EnumCheckAttribute";
    private const string _required_when_attribute_full_name = "Sonic.Standard.Data.RequiredWhenAttribute";
    private const string _deprecated_attribute_full_name = "Sonic.Standard.Marker.DeprecatedAttribute";

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
                                    typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword
                                    ),
                static (ctx, ct) => transform_validator(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取需要生成验证器代码的类型信息。
    /// </summary>
    private static ValidatorTypeInfo? transform_validator(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var members = extract_validator_members(typeSymbol);

        if (members.Count == 0) return null;

        return new ValidatorTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            members
        );
    }

    /// <summary>
    ///     提取类型的所有需要验证的公共成员信息。
    /// </summary>
    private static List<ValidatorMemberInfo> extract_validator_members(INamedTypeSymbol typeSymbol)
    {
        var members = new List<ValidatorMemberInfo>();

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
            var validationRules = extract_validation_rules(member.Name, memberType, attributes);

            var fieldAttr = DataGeneratorAttributeFacts.get_first_attribute(attributes, DataGeneratorAttributeFacts.field_attribute_full_names);

            if (fieldAttr is not null)
                foreach (var named in fieldAttr.NamedArguments)
                    if (named is { Key: "required", Value.Value: bool req })
                        isRequired = req;

            members.Add(new ValidatorMemberInfo(member.Name, memberType, isRequired, validationRules));
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
    ///     从成员的特性中提取验证规则。
    /// </summary>
    private static List<ValidationRuleInfo> extract_validation_rules(
        string memberName,
        ITypeSymbol memberType,
        ImmutableArray<AttributeData> attributes)
    {
        var rules = new List<ValidationRuleInfo>();

        foreach (var attr in attributes)
        {
            var attrFullName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (attrFullName == $"global::{_string_length_attribute_full_name}")
            {
                var min = 0;
                var max = 0;
                string? errorMessage = null;

                if (attr.ConstructorArguments.Length >= 2)
                {
                    min = attr.ConstructorArguments[0].Value is int mn ? mn : 0;
                    max = attr.ConstructorArguments[1].Value is int mx ? mx : 0;
                }

                foreach (var named in attr.NamedArguments)
                    if (named is { Key: "error_message", Value.Value: string em })
                        errorMessage = em;

                rules.Add(new ValidationRuleInfo(ValidationRuleKind.string_length, memberName, memberType)
                {
                    int_param_1 = min,
                    int_param_2 = max,
                    string_param = errorMessage
                });
            }
            else if (attrFullName == $"global::{_range_attribute_full_name}")
            {
                var min = 0.0;
                var max = 0.0;
                string? errorMessage = null;

                if (attr.ConstructorArguments.Length >= 2)
                {
                    min = attr.ConstructorArguments[0].Value is double mn ? mn : 0;
                    max = attr.ConstructorArguments[1].Value is double mx ? mx : 0;
                }

                foreach (var named in attr.NamedArguments)
                    if (named is { Key: "error_message", Value.Value: string em })
                        errorMessage = em;

                rules.Add(new ValidationRuleInfo(ValidationRuleKind.range, memberName, memberType)
                {
                    double_param_1 = min,
                    double_param_2 = max,
                    string_param = errorMessage
                });
            }
            else if (attrFullName == $"global::{_regex_attribute_full_name}")
            {
                var pattern = "";
                string? errorMessage = null;

                if (attr.ConstructorArguments.Length >= 1) pattern = attr.ConstructorArguments[0].Value as string ?? "";

                foreach (var named in attr.NamedArguments)
                    if (named is { Key: "error_message", Value.Value: string em })
                        errorMessage = em;

                rules.Add(new ValidationRuleInfo(ValidationRuleKind.regex, memberName, memberType)
                {
                    string_param = pattern,
                    string_param_2 = errorMessage
                });
            }
            else if (attrFullName == $"global::{_enum_check_attribute_full_name}")
            {
                string? errorMessage = null;

                foreach (var named in attr.NamedArguments)
                    if (named is { Key: "error_message", Value.Value: string em })
                        errorMessage = em;

                rules.Add(new ValidationRuleInfo(ValidationRuleKind.enum_check, memberName, memberType)
                {
                    string_param = errorMessage
                });
            }
            else if (attrFullName == $"global::{_required_when_attribute_full_name}")
            {
                var dependentField = "";
                var expectedValue = "";

                if (attr.ConstructorArguments.Length >= 2)
                {
                    dependentField = attr.ConstructorArguments[0].Value as string ?? "";
                    expectedValue = attr.ConstructorArguments[1].Value as string ?? "";
                }

                rules.Add(new ValidationRuleInfo(ValidationRuleKind.required_when, memberName, memberType)
                {
                    string_param = dependentField,
                    string_param_2 = expectedValue
                });
            }
            else if (attrFullName == $"global::{_deprecated_attribute_full_name}")
            {
                string? message = null;

                foreach (var named in attr.NamedArguments)
                    if (named is { Key: "message", Value.Value: string m })
                        message = m;

                rules.Add(new ValidationRuleInfo(ValidationRuleKind.deprecated, memberName, memberType)
                {
                    string_param = message
                });
            }
        }

        return rules;
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
    ///     判断类型是否为字符串类型。
    /// </summary>
    private static bool is_string_type(ITypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_String;
    }

    /// <summary>
    ///     判断类型是否为数值类型。
    /// </summary>
    private static bool is_numeric_type(ITypeSymbol type)
    {
        return type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_UInt64
            or SpecialType.System_Double or SpecialType.System_Single or SpecialType.System_Decimal;
    }

    /// <summary>
    ///     判断类型是否为枚举类型。
    /// </summary>
    private static bool is_enum_type(ITypeSymbol type)
    {
        return type.TypeKind == TypeKind.Enum;
    }

    /// <summary>
    ///     获取类型的完全限定名。
    /// </summary>
    private static string get_type_full_name(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    ///     生成所有类型的验证器源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<ValidatorTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_validator_source(info);
            var hintName = $"{info.type_name}.Validator.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的验证器源代码。
    /// </summary>
    private static string generate_validator_source(ValidatorTypeInfo info)
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
        sb.append_line($"/// <c>{info.type_name}</c> 的验证器实现。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public sealed class {info.type_name}Validator : global::Sonic.Standard.Data.Contract.IValidator<{info.fully_qualified_name}>");
        using (sb.block())
        {
            generate_validate_method(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>validate</c> 方法体。
    /// </summary>
    private static void generate_validate_method(SourceTextBuilder sb, ValidatorTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 对 <c>{info.type_name}</c> 实例执行验证。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"target\">待验证的目标对象。</param>");
        sb.append_line("/// <returns>包含错误和警告的验证结果。</returns>");
        sb.append_line(
            $"public global::Sonic.Standard.Data.Contract.ValidationResult validate({info.fully_qualified_name} target)");
        using (sb.block())
        {
            sb.append_line("var __result = new global::Sonic.Standard.Data.Contract.ValidationResult();");
            sb.append_line();

            foreach (var member in info.members) generate_member_validation(sb, member);

            sb.append_line();
            sb.append_line("return __result;");
        }
    }

    /// <summary>
    ///     生成单个成员的验证代码。
    /// </summary>
    private static void generate_member_validation(SourceTextBuilder sb, ValidatorMemberInfo member)
    {
        var name = member.name;
        var type = member.type_symbol;
        var nullable = is_nullable_type(type);

        if (nullable)
        {
            sb.append_line($"if (target.{name} is not null)");
            using (sb.block())
            {
                generate_nullable_member_rules(sb, member);
            }

            if (member.is_required)
            {
                sb.append_line("else");
                using (sb.block())
                {
                    sb.append_line($"__result.add_error(\"{name}\", \"字段 {name} 为必填字段\", \"required\");");
                }
            }
        }
        else
        {
            if (member.is_required && type.IsReferenceType)
            {
                sb.append_line($"if (target.{name} is null)");
                using (sb.block())
                {
                    sb.append_line($"__result.add_error(\"{name}\", \"字段 {name} 为必填字段\", \"required\");");
                }

                sb.append_line("else");
                using (sb.block())
                {
                    generate_non_nullable_member_rules(sb, member);
                }
            }
            else
            {
                generate_non_nullable_member_rules(sb, member);
            }
        }

        sb.append_line();
    }

    /// <summary>
    ///     生成可空成员的非空值验证规则。
    /// </summary>
    private static void generate_nullable_member_rules(SourceTextBuilder sb, ValidatorMemberInfo member)
    {
        foreach (var rule in member.rules) generate_rule_check(sb, member, rule);
    }

    /// <summary>
    ///     生成非空成员的验证规则。
    /// </summary>
    private static void generate_non_nullable_member_rules(SourceTextBuilder sb, ValidatorMemberInfo member)
    {
        foreach (var rule in member.rules) generate_rule_check(sb, member, rule);
    }

    /// <summary>
    ///     生成单条验证规则的检查代码。
    /// </summary>
    private static void generate_rule_check(SourceTextBuilder sb, ValidatorMemberInfo member, ValidationRuleInfo rule)
    {
        var name = member.name;

        switch (rule.kind)
        {
            case ValidationRuleKind.string_length:
                if (is_string_type(member.type_symbol))
                {
                    var min = rule.int_param_1;
                    var max = rule.int_param_2;
                    var errorMsg = rule.string_param ?? $"字段 {name} 的长度必须在 {min} 和 {max} 之间";

                    sb.append_line($"if (target.{name}.Length < {min} || target.{name}.Length > {max})");
                    using (sb.block())
                    {
                        sb.append_line($"__result.add_error(\"{name}\", \"{errorMsg}\", \"string_length\");");
                    }
                }

                break;

            case ValidationRuleKind.range:
                if (is_numeric_type(member.type_symbol))
                {
                    var min = rule.double_param_1;
                    var max = rule.double_param_2;
                    var errorMsg = rule.string_param ?? $"字段 {name} 的值必须在 {min} 和 {max} 之间";

                    sb.append_line(
                        $"if ((double)target.{name} < {min:R} || (double)target.{name} > {max:R})");
                    using (sb.block())
                    {
                        sb.append_line($"__result.add_error(\"{name}\", \"{errorMsg}\", \"range\");");
                    }
                }

                break;

            case ValidationRuleKind.regex:
                if (is_string_type(member.type_symbol))
                {
                    var pattern = rule.string_param ?? "";
                    var errorMsg = rule.string_param_2 ?? $"字段 {name} 不匹配正则表达式 {pattern}";

                    sb.append_line(
                        $"if (!global::System.Text.RegularExpressions.Regex.IsMatch(target.{name}, \"{StringEscapeHelper.escape_for_string(pattern)}\"))");
                    using (sb.block())
                    {
                        sb.append_line(
                            $"__result.add_error(\"{name}\", \"{StringEscapeHelper.escape_for_string(errorMsg)}\", \"regex\");");
                    }
                }

                break;

            case ValidationRuleKind.enum_check:
                if (is_enum_type(member.type_symbol))
                {
                    var enumType = get_type_full_name(member.type_symbol);
                    var errorMsg = rule.string_param ?? $"字段 {name} 的值不是有效的枚举值";

                    sb.append_line($"if (!global::System.Enum.IsDefined(typeof({enumType}), target.{name}))");
                    using (sb.block())
                    {
                        sb.append_line($"__result.add_error(\"{name}\", \"{errorMsg}\", \"enum_check\");");
                    }
                }

                break;

            case ValidationRuleKind.required_when:
            {
                var dependentField = rule.string_param ?? "";
                var expectedValue = rule.string_param_2 ?? "";
                var nullable = is_nullable_type(member.type_symbol);

                sb.append_line(
                    $"if (target.{dependentField}?.ToString() == \"{StringEscapeHelper.escape_for_string(expectedValue)}\")");
                using (sb.block())
                {
                    if (nullable)
                    {
                        sb.append_line($"if (target.{name} is null)");
                        using (sb.block())
                        {
                            sb.append_line(
                                $"__result.add_error(\"{name}\", \"当 {dependentField} 为 {expectedValue} 时字段 {name} 为必填\", \"required_when\");");
                        }
                    }
                    else if (member.type_symbol.IsReferenceType)
                    {
                        sb.append_line($"if (target.{name} is null)");
                        using (sb.block())
                        {
                            sb.append_line(
                                $"__result.add_error(\"{name}\", \"当 {dependentField} 为 {expectedValue} 时字段 {name} 为必填\", \"required_when\");");
                        }
                    }
                }

                break;
            }

            case ValidationRuleKind.deprecated:
            {
                var message = rule.string_param ?? $"字段 {name} 已弃用";

                sb.append_line($"if (target.{name} is not null)");
                using (sb.block())
                {
                    sb.append_line(
                        $"__result.add_warning(\"{name}\", \"{StringEscapeHelper.escape_for_string(message)}\", \"deprecated\");");
                }

                break;
            }
        }
    }

    #region 数据模型

    /// <summary>
    ///     需要生成验证器的目标类型信息。
    /// </summary>
    internal readonly struct ValidatorTypeInfo
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
        ///     验证成员列表。
        /// </summary>
        public readonly List<ValidatorMemberInfo> members;

        /// <summary>
        ///     初始化 <see cref="ValidatorTypeInfo" /> 的新实例。
        /// </summary>
        public ValidatorTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            bool isStruct,
            List<ValidatorMemberInfo> members)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            is_struct = isStruct;
            this.members = members;
        }
    }

    /// <summary>
    ///     验证成员信息。
    /// </summary>
    internal readonly struct ValidatorMemberInfo
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
        ///     验证规则列表。
        /// </summary>
        public readonly List<ValidationRuleInfo> rules;

        /// <summary>
        ///     初始化 <see cref="ValidatorMemberInfo" /> 的新实例。
        /// </summary>
        public ValidatorMemberInfo(
            string name,
            ITypeSymbol typeSymbol,
            bool isRequired,
            List<ValidationRuleInfo> rules)
        {
            this.name = name;
            type_symbol = typeSymbol;
            is_required = isRequired;
            this.rules = rules;
        }
    }

    /// <summary>
    ///     验证规则信息。
    /// </summary>
    internal sealed class ValidationRuleInfo
    {
        /// <summary>
        ///     规则类型。
        /// </summary>
        public readonly ValidationRuleKind kind;

        /// <summary>
        ///     成员名称。
        /// </summary>
        public readonly string member_name;

        /// <summary>
        ///     成员类型。
        /// </summary>
        public readonly ITypeSymbol member_type;

        /// <summary>
        ///     浮点数参数 1。
        /// </summary>
        public double double_param_1;

        /// <summary>
        ///     浮点数参数 2。
        /// </summary>
        public double double_param_2;

        /// <summary>
        ///     整数参数 1。
        /// </summary>
        public int int_param_1;

        /// <summary>
        ///     整数参数 2。
        /// </summary>
        public int int_param_2;

        /// <summary>
        ///     字符串参数。
        /// </summary>
        public string? string_param;

        /// <summary>
        ///     字符串参数 2。
        /// </summary>
        public string? string_param_2;

        /// <summary>
        ///     初始化 <see cref="ValidationRuleInfo" /> 的新实例。
        /// </summary>
        public ValidationRuleInfo(ValidationRuleKind kind, string memberName, ITypeSymbol memberType)
        {
            this.kind = kind;
            member_name = memberName;
            member_type = memberType;
        }
    }

    /// <summary>
    ///     验证规则类型枚举。
    /// </summary>
    internal enum ValidationRuleKind
    {
        /// <summary>
        ///     字符串长度验证。
        /// </summary>
        string_length,

        /// <summary>
        ///     数值范围验证。
        /// </summary>
        range,

        /// <summary>
        ///     正则表达式验证。
        /// </summary>
        regex,

        /// <summary>
        ///     枚举值验证。
        /// </summary>
        enum_check,

        /// <summary>
        ///     条件必填验证。
        /// </summary>
        required_when,

        /// <summary>
        ///     弃用警告。
        /// </summary>
        deprecated
    }

    #endregion
}
