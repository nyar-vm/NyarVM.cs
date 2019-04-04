using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Authorize]</c> 或 <c>[RequirePermission]</c> 特性的方法自动生成授权检查包装。
/// </summary>
[Generator]
public sealed class AuthorizationGenerator : IIncrementalGenerator
{
    private const string _authorize_attribute_full_name = "Sonic.Standard.Security.Authorization.AuthorizeAttribute";

    private const string _require_permission_attribute_full_name =
        "Sonic.Standard.Security.Authorization.RequirePermissionAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var authorizeTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _authorize_attribute_full_name,
                static (node, _) => node is TypeDeclarationSyntax typeDecl &&
                                    typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_authorize(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        var permissionTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _require_permission_attribute_full_name,
                static (node, _) => node is TypeDeclarationSyntax typeDecl &&
                                    typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_require_permission(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        var authorizeCollected = authorizeTypes.Collect();
        var permissionCollected = permissionTypes.Collect();

        var allTypes = authorizeCollected.Combine(permissionCollected)
            .Select(static (pair, _) =>
            {
                var list = new List<AuthorizationTypeInfo>();
                foreach (var info in pair.Left) list.Add(info);

                foreach (var info in pair.Right) list.Add(info);

                return merge_type_infos([.. list]);
            });

        context.RegisterSourceOutput(allTypes, generate_source);
    }

    /// <summary>
    ///     合并同一类型的授权信息。
    /// </summary>
    private static ImmutableArray<AuthorizationTypeInfo> merge_type_infos(ImmutableArray<AuthorizationTypeInfo> infos)
    {
        var dict = new Dictionary<string, AuthorizationTypeInfo>();

        foreach (var info in infos)
            if (dict.TryGetValue(info.fully_qualified_name, out var existing))
            {
                var mergedMethods = new List<AuthorizationMethodInfo>(existing.methods);

                foreach (var method in info.methods)
                {
                    var idx = mergedMethods.FindIndex(m => m.method_name == method.method_name);

                    if (idx >= 0)
                    {
                        var existingMethod = mergedMethods[idx];
                        var mergedRoles = existingMethod.roles ?? method.roles;
                        var mergedPermission = existingMethod.permission ?? method.permission;

                        mergedMethods[idx] = new AuthorizationMethodInfo(
                            method.method_name,
                            method.return_type,
                            method.is_void,
                            method.parameters,
                            mergedRoles,
                            mergedPermission);
                    }
                    else
                    {
                        mergedMethods.Add(method);
                    }
                }

                dict[info.fully_qualified_name] = new AuthorizationTypeInfo(
                    info.type_name,
                    info.fully_qualified_name,
                    info.namespace_name,
                    info.is_struct,
                    mergedMethods);
            }
            else
            {
                dict[info.fully_qualified_name] = info;
            }

        return [.. dict.Values];
    }

    /// <summary>
    ///     提取标记了 <c>[Authorize]</c> 的类型和方法信息。
    /// </summary>
    private static AuthorizationTypeInfo? transform_authorize(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var classRoles = extract_class_roles(typeSymbol);

        var methods = extract_authorize_methods(typeSymbol, classRoles);

        if (methods.Count == 0 && classRoles is null) return null;

        return new AuthorizationTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            methods);
    }

    /// <summary>
    ///     提取标记了 <c>[RequirePermission]</c> 的类型和方法信息。
    /// </summary>
    private static AuthorizationTypeInfo? transform_require_permission(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var classPermission = extract_class_permission(typeSymbol);

        var methods = extract_permission_methods(typeSymbol, classPermission);

        if (methods.Count == 0 && classPermission is null) return null;

        return new AuthorizationTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            methods);
    }

    /// <summary>
    ///     提取类级别 <c>[Authorize]</c> 特性的角色列表。
    /// </summary>
    private static string[]? extract_class_roles(INamedTypeSymbol typeSymbol)
    {
        var classAttr = typeSymbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_authorize_attribute_full_name}");

        if (classAttr is null) return null;

        if (classAttr.ConstructorArguments.Length > 0 &&
            classAttr.ConstructorArguments[0].Values.Length > 0)
            return
            [
                .. classAttr.ConstructorArguments[0].Values
                    .Where(v => v.Value is string)
                    .Select(v => (string)v.Value!)
            ];

        return [];
    }

    /// <summary>
    ///     提取类级别 <c>[RequirePermission]</c> 特性的权限。
    /// </summary>
    private static string? extract_class_permission(INamedTypeSymbol typeSymbol)
    {
        var classAttr = typeSymbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_require_permission_attribute_full_name}");

        if (classAttr is null) return null;

        foreach (var named in classAttr.NamedArguments)
            if (named is { Key: "permission", Value.Value: string p })
                return p;

        return null;
    }

    /// <summary>
    ///     提取标记了 <c>[Authorize]</c> 的方法信息。
    /// </summary>
    private static List<AuthorizationMethodInfo> extract_authorize_methods(INamedTypeSymbol typeSymbol,
        string[]? classRoles)
    {
        var methods = new List<AuthorizationMethodInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.IsStatic) continue;

            if (method.DeclaredAccessibility != Accessibility.Public) continue;

            var methodAttr = method.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_authorize_attribute_full_name}");

            if (methodAttr is null && classRoles is null) continue;

            var roles = classRoles;

            if (methodAttr is not null)
            {
                if (methodAttr.ConstructorArguments.Length > 0 &&
                    methodAttr.ConstructorArguments[0].Values.Length > 0)
                    roles =
                    [
                        .. methodAttr.ConstructorArguments[0].Values
                            .Where(v => v.Value is string)
                            .Select(v => (string)v.Value!)
                    ];
                else
                    roles = [];
            }

            var parameters = method.Parameters
                .Select(p => new AuthParameterInfo(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.RefKind))
                .ToList();

            methods.Add(new AuthorizationMethodInfo(
                method.Name,
                method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                method.ReturnsVoid,
                parameters,
                roles,
                null));
        }

        return methods;
    }

    /// <summary>
    ///     提取标记了 <c>[RequirePermission]</c> 的方法信息。
    /// </summary>
    private static List<AuthorizationMethodInfo> extract_permission_methods(INamedTypeSymbol typeSymbol,
        string? classPermission)
    {
        var methods = new List<AuthorizationMethodInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.IsStatic) continue;

            if (method.DeclaredAccessibility != Accessibility.Public) continue;

            var methodAttr = method.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_require_permission_attribute_full_name}");

            if (methodAttr is null && classPermission is null) continue;

            var permission = classPermission;

            if (methodAttr is not null)
                foreach (var named in methodAttr.NamedArguments)
                    if (named is { Key: "permission", Value.Value: string p })
                        permission = p;

            var parameters = method.Parameters
                .Select(p => new AuthParameterInfo(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.RefKind))
                .ToList();

            methods.Add(new AuthorizationMethodInfo(
                method.Name,
                method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                method.ReturnsVoid,
                parameters,
                null,
                permission));
        }

        return methods;
    }

    /// <summary>
    ///     生成所有类型的授权检查包装源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context,
        ImmutableArray<AuthorizationTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_authorization_source(info);
            var hintName = $"{info.type_name}.Authorization.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的授权检查包装源代码。
    /// </summary>
    private static string generate_authorization_source(AuthorizationTypeInfo info)
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

        var typeKeyword = info.is_struct ? "partial struct" : "partial class";
        sb.append_line($"{typeKeyword} {info.type_name}");
        using (sb.block())
        {
            foreach (var method in info.methods)
            {
                generate_authorization_wrapper(sb, info, method);
                sb.append_line();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     为单个方法生成授权检查包装方法。
    /// </summary>
    private static void generate_authorization_wrapper(SourceTextBuilder sb, AuthorizationTypeInfo info,
        AuthorizationMethodInfo method)
    {
        var paramList = string.Join(", ", method.parameters.Select(p => format_parameter(p)));
        var argList = string.Join(", ", method.parameters.Select(p => format_argument(p)));

        sb.append_line("/// <summary>");
        sb.append_line($"/// 带授权检查的 <c>{method.method_name}</c> 包装方法。");
        sb.append_line("/// </summary>");

        foreach (var p in method.parameters) sb.append_line($"/// <param name=\"{p.name}\">{p.name} 参数。</param>");

        var returnType = method.is_void ? "void" : method.return_type;
        sb.append_line($"public {returnType} {method.method_name}_authorized({paramList})");
        using (sb.block())
        {
            if (method.roles is not null && method.roles.Length > 0)
            {
                var rolesArray = string.Join(", ", method.roles.Select(r => $"\"{r}\""));
                sb.append_line($"var __roles = new global::System.String[] {{ {rolesArray} }};");
                sb.append_line(
                    "var __principal = global::Sonic.Standard.Security.Authentication.AuthContext.current_principal();");
                sb.append_line("if (__principal is null)");
                using (sb.block())
                {
                    sb.append_line("throw new global::System.UnauthorizedAccessException(\"授权失败：未提供有效的身份凭据\");");
                }

                sb.append_line();
                sb.append_line("foreach (var __role in __roles)");
                using (sb.block())
                {
                    sb.append_line("if (!__principal.is_in_role(__role))");
                    using (sb.block())
                    {
                        sb.append_line(
                            "throw new global::System.UnauthorizedAccessException($\"授权失败：需要角色 {__role}\");");
                    }
                }

                sb.append_line();
            }

            if (method.permission is not null)
            {
                sb.append_line(
                    $"var __hasPermission = global::Sonic.Standard.Security.Authorization.AuthorizationContext.check_permission(\"{method.permission}\");");
                sb.append_line("if (!__hasPermission)");
                using (sb.block())
                {
                    sb.append_line(
                        $"throw new global::System.UnauthorizedAccessException(\"授权失败：需要权限 {method.permission}\");");
                }

                sb.append_line();
            }

            if (method.is_void)
                sb.append_line($"{method.method_name}({argList});");
            else
                sb.append_line($"return {method.method_name}({argList});");
        }
    }

    /// <summary>
    ///     格式化参数声明。
    /// </summary>
    private static string format_parameter(AuthParameterInfo param)
    {
        var prefix = param.ref_kind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => ""
        };

        return $"{prefix}{param.Type} {param.name}";
    }

    /// <summary>
    ///     格式化参数调用。
    /// </summary>
    private static string format_argument(AuthParameterInfo param)
    {
        var prefix = param.ref_kind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => ""
        };

        return $"{prefix}{param.name}";
    }

    #region 数据模型

    /// <summary>
    ///     包含授权方法的类型信息。
    /// </summary>
    internal readonly struct AuthorizationTypeInfo
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
        ///     需要授权检查的方法列表。
        /// </summary>
        public readonly List<AuthorizationMethodInfo> methods;

        /// <summary>
        ///     初始化 <see cref="AuthorizationTypeInfo" /> 的新实例。
        /// </summary>
        public AuthorizationTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            bool isStruct,
            List<AuthorizationMethodInfo> methods)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            is_struct = isStruct;
            this.methods = methods;
        }
    }

    /// <summary>
    ///     需要授权检查的方法信息。
    /// </summary>
    internal readonly struct AuthorizationMethodInfo
    {
        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     返回类型的完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     是否为 void 返回类型。
        /// </summary>
        public readonly bool is_void;

        /// <summary>
        ///     参数列表。
        /// </summary>
        public readonly List<AuthParameterInfo> parameters;

        /// <summary>
        ///     允许的角色列表，为 null 表示无角色要求。
        /// </summary>
        public readonly string[]? roles;

        /// <summary>
        ///     所需权限，为 null 表示无权限要求。
        /// </summary>
        public readonly string? permission;

        /// <summary>
        ///     初始化 <see cref="AuthorizationMethodInfo" /> 的新实例。
        /// </summary>
        public AuthorizationMethodInfo(
            string methodName,
            string returnType,
            bool isVoid,
            List<AuthParameterInfo> parameters,
            string[]? roles,
            string? permission)
        {
            method_name = methodName;
            return_type = returnType;
            is_void = isVoid;
            this.parameters = parameters;
            this.roles = roles;
            this.permission = permission;
        }
    }

    /// <summary>
    ///     方法参数信息。
    /// </summary>
    internal readonly struct AuthParameterInfo
    {
        /// <summary>
        ///     参数名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     参数类型的完全限定名称。
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     参数传递方式。
        /// </summary>
        public readonly RefKind ref_kind;

        /// <summary>
        ///     初始化 <see cref="AuthParameterInfo" /> 的新实例。
        /// </summary>
        public AuthParameterInfo(string name, string type, RefKind refKind)
        {
            this.name = name;
            Type = type;
            ref_kind = refKind;
        }
    }

    #endregion
}