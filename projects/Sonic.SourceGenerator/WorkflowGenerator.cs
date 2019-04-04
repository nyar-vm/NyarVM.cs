using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Workflow]</c> 特性的类自动生成 <c>ISaga&lt;T&gt;</c> 实现，
///     按依赖顺序编排 <c>[Step]</c> 方法，并支持 <c>[Compensation]</c> 回滚。
/// </summary>
[Generator]
public sealed class WorkflowGenerator : IIncrementalGenerator
{
    private const string _workflow_attribute_full_name = "Sonic.Standard.Flow.Workflow.WorkflowAttribute";
    private const string _step_attribute_full_name = "Sonic.Standard.Flow.Workflow.StepAttribute";
    private const string _compensation_attribute_full_name = "Sonic.Standard.Flow.Workflow.CompensationAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _workflow_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_workflow(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取工作流类型信息。
    /// </summary>
    private static WorkflowTypeInfo? transform_workflow(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var workflowAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_workflow_attribute_full_name}");

        string? workflowName = null;

        if (workflowAttr is not null)
            foreach (var named in workflowAttr.NamedArguments)
                if (named is { Key: "name", Value.Value: string n })
                    workflowName = n;

        var steps = new List<StepInfo>();
        var compensations = new List<CompensationInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.IsStatic) continue;

            if (method.DeclaredAccessibility != Accessibility.Public) continue;

            var stepAttr = method.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_step_attribute_full_name}");

            if (stepAttr is not null)
            {
                string? dependsOn = null;

                foreach (var named in stepAttr.NamedArguments)
                    if (named is { Key: "depends_on", Value.Value: string d })
                        dependsOn = d;

                steps.Add(new StepInfo(
                    method.Name,
                    dependsOn,
                    method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    method.Parameters.Length > 0));
            }

            var compAttr = method.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_compensation_attribute_full_name}");

            if (compAttr is not null)
            {
                string? forStep = null;

                foreach (var named in compAttr.NamedArguments)
                    if (named is { Key: "for_step", Value.Value: string fs })
                        forStep = fs;

                compensations.Add(new CompensationInfo(method.Name, forStep, method.Parameters.Length > 0));
            }
        }

        if (steps.Count == 0) return null;

        var orderedSteps = topological_sort(steps);

        return new WorkflowTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            workflowName ?? typeSymbol.Name,
            orderedSteps,
            compensations);
    }

    /// <summary>
    ///     对步骤进行拓扑排序，确保依赖步骤先执行。
    /// </summary>
    private static List<StepInfo> topological_sort(List<StepInfo> steps)
    {
        var nameToStep = new Dictionary<string, StepInfo>();

        foreach (var step in steps) nameToStep[step.method_name] = step;

        var visited = new HashSet<string>();
        var result = new List<StepInfo>();

        foreach (var step in steps) visit(step, nameToStep, visited, result);

        return result;
    }

    /// <summary>
    ///     深度优先遍历步骤依赖图。
    /// </summary>
    private static void visit(StepInfo step, Dictionary<string, StepInfo> nameToStep, HashSet<string> visited,
        List<StepInfo> result)
    {
        if (!visited.Add(step.method_name)) return;

        if (step.depends_on is not null && nameToStep.TryGetValue(step.depends_on, out var dep))
            visit(dep, nameToStep, visited, result);

        result.Add(step);
    }

    /// <summary>
    ///     生成所有工作流类型的源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<WorkflowTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_workflow_source(info);
            var hintName = $"{info.type_name}.Workflow.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个工作流类型的源代码。
    /// </summary>
    private static string generate_workflow_source(WorkflowTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System;");
        sb.append_line("using System.Threading.Tasks;");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{info.type_name}</c> 的工作流 Saga 实现。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public partial class {info.type_name} : global::Sonic.Standard.Flow.Workflow.ISaga<{info.fully_qualified_name}>");
        using (sb.block())
        {
            generate_state_property(sb, info);
            sb.append_line();
            generate_persist_method(sb, info);
            sb.append_line();
            generate_execute_method(sb, info);
            sb.append_line();
            generate_compensate_method(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>state</c> 属性。
    /// </summary>
    private static void generate_state_property(SourceTextBuilder sb, WorkflowTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 当前 saga 状态。");
        sb.append_line("/// </summary>");
        sb.append_line($"public {info.fully_qualified_name} state => this;");
    }

    /// <summary>
    ///     生成 <c>persist</c> 方法。
    /// </summary>
    private static void generate_persist_method(SourceTextBuilder sb, WorkflowTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 持久化当前 saga 状态。");
        sb.append_line("/// </summary>");
        sb.append_line("public void persist()");
        using (sb.block())
        {
        }
    }

    /// <summary>
    ///     生成 <c>execute</c> 方法，按拓扑顺序执行步骤。
    /// </summary>
    private static void generate_execute_method(SourceTextBuilder sb, WorkflowTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 按依赖顺序执行 <c>{info.workflow_name}</c> 工作流的所有步骤。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"context\">工作流上下文。</param>");
        sb.append_line(
            $"public async global::System.Threading.Tasks.Task execute({info.fully_qualified_name} context)");
        using (sb.block())
        {
            sb.append_line("var __completed = new global::System.Collections.Generic.List<string>();");
            sb.append_line();
            sb.append_line("try");
            using (sb.block())
            {
                foreach (var step in info.steps)
                {
                    generate_step_invocation(sb, step);
                    sb.append_line($"__completed.Add(\"{step.method_name}\");");
                    sb.append_line();
                }
            }

            sb.append_line("catch");
            using (sb.block())
            {
                sb.append_line("await compensate(context, __completed);");
                sb.append_line("throw;");
            }
        }
    }

    /// <summary>
    ///     生成单个步骤的调用代码。
    /// </summary>
    private static void generate_step_invocation(SourceTextBuilder sb, StepInfo step)
    {
        var isAsync = step.return_type.Contains("Task");

        if (step.has_parameter)
        {
            if (isAsync)
                sb.append_line($"await {step.method_name}(context);");
            else
                sb.append_line($"{step.method_name}(context);");
        }
        else
        {
            if (isAsync)
                sb.append_line($"await {step.method_name}();");
            else
                sb.append_line($"{step.method_name}();");
        }
    }

    /// <summary>
    ///     生成 <c>compensate</c> 方法，按完成步骤的逆序执行补偿操作。
    /// </summary>
    private static void generate_compensate_method(SourceTextBuilder sb, WorkflowTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 按逆序执行已完成步骤的补偿操作。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"context\">工作流上下文。</param>");
        sb.append_line(
            $"private async global::System.Threading.Tasks.Task compensate({info.fully_qualified_name} context, global::System.Collections.Generic.List<string> completedSteps)");
        using (sb.block())
        {
            if (info.compensations.Count == 0)
            {
                sb.append_line("await global::System.Threading.Tasks.Task.CompletedTask;");
                return;
            }

            sb.append_line("for (var __i = completedSteps.Count - 1; __i >= 0; __i--)");
            using (sb.block())
            {
                sb.append_line("var __stepName = completedSteps[__i];");
                sb.append_line();

                var first = true;
                foreach (var comp in info.compensations)
                {
                    var keyword = first ? "if" : "else if";
                    first = false;

                    if (comp.for_step is not null)
                        sb.append_line($"{keyword} (__stepName == \"{comp.for_step}\")");
                    else
                        sb.append_line($"{keyword} (__stepName == \"{comp.method_name}\")");

                    using (sb.block())
                    {
                        if (comp.has_parameter)
                            sb.append_line($"await {comp.method_name}(context);");
                        else
                            sb.append_line($"await {comp.method_name}();");
                    }
                }
            }
        }
    }

    #region 数据模型

    /// <summary>
    ///     工作流类型信息。
    /// </summary>
    internal readonly struct WorkflowTypeInfo
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
        ///     工作流名称。
        /// </summary>
        public readonly string workflow_name;

        /// <summary>
        ///     按拓扑顺序排列的步骤列表。
        /// </summary>
        public readonly List<StepInfo> steps;

        /// <summary>
        ///     补偿操作列表。
        /// </summary>
        public readonly List<CompensationInfo> compensations;

        /// <summary>
        ///     初始化 <see cref="WorkflowTypeInfo" /> 的新实例。
        /// </summary>
        public WorkflowTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            string workflowName,
            List<StepInfo> steps,
            List<CompensationInfo> compensations)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            workflow_name = workflowName;
            this.steps = steps;
            this.compensations = compensations;
        }
    }

    /// <summary>
    ///     工作流步骤信息。
    /// </summary>
    internal readonly struct StepInfo
    {
        /// <summary>
        ///     步骤方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     依赖的步骤名称。
        /// </summary>
        public readonly string? depends_on;

        /// <summary>
        ///     返回类型的完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     是否有参数。
        /// </summary>
        public readonly bool has_parameter;

        /// <summary>
        ///     初始化 <see cref="StepInfo" /> 的新实例。
        /// </summary>
        public StepInfo(string methodName, string? dependsOn, string returnType, bool hasParameter)
        {
            method_name = methodName;
            depends_on = dependsOn;
            return_type = returnType;
            has_parameter = hasParameter;
        }
    }

    /// <summary>
    ///     补偿操作信息。
    /// </summary>
    internal readonly struct CompensationInfo
    {
        /// <summary>
        ///     补偿方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     对应的步骤名称。
        /// </summary>
        public readonly string? for_step;

        /// <summary>
        ///     是否有参数。
        /// </summary>
        public readonly bool has_parameter;

        /// <summary>
        ///     初始化 <see cref="CompensationInfo" /> 的新实例。
        /// </summary>
        public CompensationInfo(string methodName, string? forStep, bool hasParameter)
        {
            method_name = methodName;
            for_step = forStep;
            has_parameter = hasParameter;
        }
    }

    #endregion
}