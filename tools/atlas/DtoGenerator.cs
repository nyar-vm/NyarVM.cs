namespace Atlas.CLI;

/// <summary>
///     DTO 生成器——包装 CSharpGenerator，以 dto 为目标生成 DTO record 类
/// </summary>
public sealed class DtoGenerator : IIncrementalGenerator
{
    private readonly CSharpGenerator _inner = new();

    /// <summary>
    ///     生成器名称
    /// </summary>
    public string name => "dto";

    /// <summary>
    ///     支持的目标类型
    /// </summary>
    public string[] supported_targets => _inner.SupportedTargets;

    /// <summary>
    ///     生成 DTO 代码
    /// </summary>
    public GeneratorResult generate(GeneratorContext context)
    {
        var overriddenContext = new GeneratorContext
        {
            Schema = context.Schema,
            SchemaPath = context.SchemaPath,
            OutputPath = context.OutputPath,
            Options = override_target(context.Options),
            Diagnostics = context.Diagnostics
        };

        return _inner.Generate(overriddenContext);
    }

    /// <summary>
    ///     增量生成 DTO 代码
    /// </summary>
    public GeneratorResult generate_incremental(IncrementalGeneratorContext context)
    {
        var overriddenContext = new IncrementalGeneratorContext
        {
            Schema = context.Schema,
            PreviousSchema = context.PreviousSchema,
            Diff = context.Diff,
            ChangedTypeNames = context.ChangedTypeNames,
            RemovedTypeNames = context.RemovedTypeNames,
            SchemaPath = context.SchemaPath,
            OutputPath = context.OutputPath,
            Options = override_target(context.Options),
            Diagnostics = context.Diagnostics
        };

        return _inner.GenerateIncremental(overriddenContext);
    }

    /// <summary>
    ///     将选项中的 targets 覆盖为 dto
    /// </summary>
    private static Dictionary<string, object> override_target(IReadOnlyDictionary<string, object> original)
    {
        var result = new Dictionary<string, object>(original) { ["targets"] = "dto" };
        return result;
    }
}