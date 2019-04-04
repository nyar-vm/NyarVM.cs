using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[GenerateFactory]</c> 特性的接口自动生成工厂类，
///     并为标记了 <c>[Service]</c> 特性的类生成服务注册代码。
/// </summary>
[Generator]
public sealed class PureDIGenerator : IIncrementalGenerator
{
    private const string _service_attribute_full_name = "Sonic.Standard.DI.ServiceAttribute";
    private const string _inject_attribute_full_name = "Sonic.Standard.DI.InjectAttribute";
    private const string _generate_factory_attribute_full_name = "Sonic.Standard.DI.GenerateFactoryAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var factoryInterfaces = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _generate_factory_attribute_full_name,
                static (node, _) => node is InterfaceDeclarationSyntax,
                static (ctx, ct) => transform_factory(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        var serviceClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _service_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => transform_service(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(factoryInterfaces, generate_factory_source);
        context.RegisterSourceOutput(serviceClasses, generate_service_source);
    }

    /// <summary>
    ///     提取标记了 <c>[GenerateFactory]</c> 特性的接口信息。
    /// </summary>
    private static FactoryTypeInfo? transform_factory(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = context.TargetSymbol as INamedTypeSymbol;
        if (typeSymbol is null) return null;

        if (typeSymbol.TypeKind != TypeKind.Interface) return null;

        var methods = extract_interface_methods(typeSymbol);

        return new FactoryTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            methods);
    }

    /// <summary>
    ///     提取接口中所有方法的信息。
    /// </summary>
    private static List<FactoryMethodInfo> extract_interface_methods(INamedTypeSymbol typeSymbol)
    {
        var methods = new List<FactoryMethodInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.MethodKind != MethodKind.Ordinary) continue;

            var parameters = new List<FactoryParameterInfo>();

            foreach (var param in method.Parameters)
                parameters.Add(new FactoryParameterInfo(
                    param.Name,
                    param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    param.RefKind));

            methods.Add(new FactoryMethodInfo(
                method.Name,
                method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                parameters));
        }

        return methods;
    }

    /// <summary>
    ///     提取标记了 <c>[Service]</c> 特性的类信息。
    /// </summary>
    private static ServiceTypeInfo? transform_service(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = context.TargetSymbol as INamedTypeSymbol;
        if (typeSymbol is null) return null;

        var serviceAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_service_attribute_full_name}");

        if (serviceAttr is null) return null;

        string? lifetime = null;

        foreach (var named in serviceAttr.NamedArguments)
            if (named is { Key: "lifetime", Value.Value: string l })
                lifetime = l;

        var injectMembers = extract_inject_members(typeSymbol);

        var constructorDeps = extract_constructor_dependencies(typeSymbol);

        return new ServiceTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            lifetime ?? "Transient",
            constructorDeps,
            injectMembers);
    }

    /// <summary>
    ///     提取类型中标记了 <c>[Inject]</c> 特性的构造函数依赖。
    /// </summary>
    private static List<InjectParameterInfo> extract_constructor_dependencies(INamedTypeSymbol typeSymbol)
    {
        var deps = new List<InjectParameterInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.MethodKind != MethodKind.Constructor) continue;

            var hasInjectAttr = method.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_inject_attribute_full_name}");

            if (!hasInjectAttr) continue;

            foreach (var param in method.Parameters)
                deps.Add(new InjectParameterInfo(
                    param.Name,
                    param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            break;
        }

        return deps;
    }

    /// <summary>
    ///     提取类型中标记了 <c>[Inject]</c> 特性的属性和字段。
    /// </summary>
    private static List<InjectMemberInfo> extract_inject_members(INamedTypeSymbol typeSymbol)
    {
        var members = new List<InjectMemberInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsStatic) continue;

            var hasInjectAttr = member.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_inject_attribute_full_name}");

            if (!hasInjectAttr) continue;

            if (member is IPropertySymbol prop)
                members.Add(new InjectMemberInfo(
                    prop.Name,
                    prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    true));
            else if (member is IFieldSymbol field)
                members.Add(new InjectMemberInfo(
                    field.Name,
                    field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    false));
        }

        return members;
    }

    /// <summary>
    ///     生成工厂类的源代码。
    /// </summary>
    private static void generate_factory_source(SourceProductionContext context,
        ImmutableArray<FactoryTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_factory_class_source(info);
            var hintName = $"{info.type_name}Factory.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成服务注册的源代码。
    /// </summary>
    private static void generate_service_source(SourceProductionContext context,
        ImmutableArray<ServiceTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_service_class_source(info);
            var hintName = $"{info.type_name}.ServiceRegistration.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个接口的工厂类源代码。
    /// </summary>
    private static string generate_factory_class_source(FactoryTypeInfo info)
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

        var factoryClassName = $"{info.type_name}Factory";
        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{info.type_name}</c> 的自动生成工厂类。");
        sb.append_line("/// </summary>");
        sb.append_line($"public sealed class {factoryClassName} : {info.fully_qualified_name}");
        using (sb.block())
        {
            generate_factory_constructor_deps(sb, info);
            sb.append_line();
            generate_factory_methods(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成工厂类的构造函数依赖字段和构造函数。
    /// </summary>
    private static void generate_factory_constructor_deps(SourceTextBuilder sb, FactoryTypeInfo info)
    {
        var allDeps = new List<FactoryParameterInfo>();

        foreach (var method in info.methods)
        foreach (var param in method.parameters)
            if (!allDeps.Any(d => d.Type == param.Type))
                allDeps.Add(param);

        for (var i = 0; i < allDeps.Count; i++)
        {
            var dep = allDeps[i];
            sb.append_line("/// <summary>");
            sb.append_line($"/// 依赖 <c>{dep.Type}</c> 的实例。");
            sb.append_line("/// </summary>");
            sb.append_line($"private readonly {dep.Type} _{dep.name};");
            if (i < allDeps.Count - 1) sb.append_line();
        }

        if (allDeps.Count > 0)
        {
            sb.append_line();
            var paramList = string.Join(", ", allDeps.Select(d => $"{d.Type} {d.name}"));
            sb.append_line("/// <summary>");
            sb.append_line($"/// 初始化 <c>{info.type_name}Factory</c> 的新实例。");
            sb.append_line("/// </summary>");

            foreach (var dep in allDeps)
                sb.append_line($"/// <param name=\"{dep.name}\">依赖 <c>{dep.Type}</c> 的实例。</param>");

            sb.append_line($"public {info.type_name}Factory({paramList})");
            using (sb.block())
            {
                foreach (var dep in allDeps) sb.append_line($"_{dep.name} = {dep.name};");
            }
        }
    }

    /// <summary>
    ///     生成工厂类中实现接口的方法。
    /// </summary>
    private static void generate_factory_methods(SourceTextBuilder sb, FactoryTypeInfo info)
    {
        for (var i = 0; i < info.methods.Count; i++)
        {
            if (i > 0) sb.append_line();

            var method = info.methods[i];
            var paramList = string.Join(", ", method.parameters.Select(p =>
            {
                var prefix = p.ref_kind switch
                {
                    RefKind.Ref => "ref ",
                    RefKind.Out => "out ",
                    RefKind.In => "in ",
                    _ => ""
                };
                return $"{prefix}{p.Type} {p.name}";
            }));

            sb.append_line("/// <summary>");
            sb.append_line($"/// 实现 <c>{method.method_name}</c> 接口方法。");
            sb.append_line("/// </summary>");

            foreach (var p in method.parameters) sb.append_line($"/// <param name=\"{p.name}\">{p.name} 参数。</param>");

            sb.append_line($"public {method.return_type} {method.method_name}({paramList})");
            using (sb.block())
            {
                var argList = string.Join(", ", method.parameters.Select(p =>
                {
                    var prefix = p.ref_kind switch
                    {
                        RefKind.Ref => "ref ",
                        RefKind.Out => "out ",
                        RefKind.In => "in ",
                        _ => ""
                    };
                    return $"{prefix}{p.name}";
                }));

                sb.append_line($"return new {method.return_type}({argList});");
            }
        }
    }

    /// <summary>
    ///     生成单个服务类的注册代码源代码。
    /// </summary>
    private static string generate_service_class_source(ServiceTypeInfo info)
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
        sb.append_line($"/// <c>{info.type_name}</c> 的服务注册扩展。");
        sb.append_line("/// </summary>");
        sb.append_line($"public static class {info.type_name}ServiceExtensions");
        using (sb.block())
        {
            sb.append_line("/// <summary>");
            sb.append_line($"/// 将 <c>{info.type_name}</c> 注册到服务容器中。");
            sb.append_line("/// </summary>");
            sb.append_line("/// <param name=\"services\">服务容器。</param>");
            sb.append_line(
                $"public static void Register{info.type_name}(this global::Sonic.Standard.DI.IServiceContainer services)");
            using (sb.block())
            {
                var lifetime = info.lifetime;
                sb.append_line(
                    $"services.Register<{info.fully_qualified_name}>(global::Sonic.Standard.DI.ServiceLifetime.{lifetime});");

                foreach (var dep in info.constructor_deps)
                    sb.append_line(
                        $"services.Register<{dep.Type}>(global::Sonic.Standard.DI.ServiceLifetime.{lifetime});");
            }
        }

        return sb.ToString();
    }

    #region 数据模型

    /// <summary>
    ///     需要生成工厂类的接口信息。
    /// </summary>
    internal readonly struct FactoryTypeInfo
    {
        /// <summary>
        ///     接口名称。
        /// </summary>
        public readonly string type_name;

        /// <summary>
        ///     完全限定接口名称。
        /// </summary>
        public readonly string fully_qualified_name;

        /// <summary>
        ///     命名空间名称。
        /// </summary>
        public readonly string? namespace_name;

        /// <summary>
        ///     接口方法列表。
        /// </summary>
        public readonly List<FactoryMethodInfo> methods;

        /// <summary>
        ///     初始化 <see cref="FactoryTypeInfo" /> 的新实例。
        /// </summary>
        public FactoryTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            List<FactoryMethodInfo> methods)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            this.methods = methods;
        }
    }

    /// <summary>
    ///     工厂接口中的方法信息。
    /// </summary>
    internal readonly struct FactoryMethodInfo
    {
        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     返回类型完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     方法参数列表。
        /// </summary>
        public readonly List<FactoryParameterInfo> parameters;

        /// <summary>
        ///     初始化 <see cref="FactoryMethodInfo" /> 的新实例。
        /// </summary>
        public FactoryMethodInfo(
            string methodName,
            string returnType,
            List<FactoryParameterInfo> parameters)
        {
            method_name = methodName;
            return_type = returnType;
            this.parameters = parameters;
        }
    }

    /// <summary>
    ///     工厂方法参数信息。
    /// </summary>
    internal readonly struct FactoryParameterInfo
    {
        /// <summary>
        ///     参数名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     参数类型完全限定名称。
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     参数传递方式。
        /// </summary>
        public readonly RefKind ref_kind;

        /// <summary>
        ///     初始化 <see cref="FactoryParameterInfo" /> 的新实例。
        /// </summary>
        public FactoryParameterInfo(string name, string type, RefKind refKind)
        {
            this.name = name;
            Type = type;
            ref_kind = refKind;
        }
    }

    /// <summary>
    ///     需要生成服务注册的类信息。
    /// </summary>
    internal readonly struct ServiceTypeInfo
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
        ///     服务生命周期。
        /// </summary>
        public readonly string lifetime;

        /// <summary>
        ///     构造函数依赖列表。
        /// </summary>
        public readonly List<InjectParameterInfo> constructor_deps;

        /// <summary>
        ///     属性/字段注入列表。
        /// </summary>
        public readonly List<InjectMemberInfo> inject_members;

        /// <summary>
        ///     初始化 <see cref="ServiceTypeInfo" /> 的新实例。
        /// </summary>
        public ServiceTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            string lifetime,
            List<InjectParameterInfo> constructorDeps,
            List<InjectMemberInfo> injectMembers)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            this.lifetime = lifetime;
            constructor_deps = constructorDeps;
            inject_members = injectMembers;
        }
    }

    /// <summary>
    ///     构造函数注入参数信息。
    /// </summary>
    internal readonly struct InjectParameterInfo
    {
        /// <summary>
        ///     参数名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     参数类型完全限定名称。
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     初始化 <see cref="InjectParameterInfo" /> 的新实例。
        /// </summary>
        public InjectParameterInfo(string name, string type)
        {
            this.name = name;
            Type = type;
        }
    }

    /// <summary>
    ///     属性/字段注入成员信息。
    /// </summary>
    internal readonly struct InjectMemberInfo
    {
        /// <summary>
        ///     成员名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     成员类型完全限定名称。
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     是否为属性。
        /// </summary>
        public readonly bool is_property;

        /// <summary>
        ///     初始化 <see cref="InjectMemberInfo" /> 的新实例。
        /// </summary>
        public InjectMemberInfo(string name, string type, bool isProperty)
        {
            this.name = name;
            Type = type;
            is_property = isProperty;
        }
    }

    #endregion
}