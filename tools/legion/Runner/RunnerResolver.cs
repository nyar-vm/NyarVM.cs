using Nyar.PackageManager.Package;

namespace Legion.CLI.Runner;

/// <summary>
///     Runner 解析器，根据 target 标签选择对应的 Runner 实现。
///     优先级：CLI --runner 参数 &gt; legion.von [runner] &gt; LEGION_RUNNER_* 环境变量 &gt; PATH 自动检测
/// </summary>
public static class RunnerResolver
{
    /// <summary>
    ///     根据 target 标签解析 Runner
    /// </summary>
    /// <param name="target">目标标签（nyar、clr、jvm、node）</param>
    /// <param name="runnerConfigs">legion.von 中的 Runner 配置列表</param>
    /// <param name="explicitRunner">CLI 显式指定的 runner 路径</param>
    /// <returns>对应的 IRunner 实例，或 null（目标不支持）</returns>
    public static IRunner? resolve(string target, List<RunnerConfig>? runnerConfigs, string? explicitRunner)
    {
        return target.ToLowerInvariant() switch
        {
            "nyar" => new NyarVmRunner(),
            "clr" => create_clr_runner(target, runnerConfigs, explicitRunner),
            "jvm" => create_jvm_runner(target, runnerConfigs, explicitRunner),
            "node" => create_node_runner(target, runnerConfigs, explicitRunner),
            _ => null
        };
    }

    private static IRunner create_clr_runner(string target, List<RunnerConfig>? runnerConfigs, string? explicitRunner)
    {
        var command = resolve_command(target, runnerConfigs, explicitRunner, "dotnet");
        return new ClrRunner(command);
    }

    private static IRunner create_jvm_runner(string target, List<RunnerConfig>? runnerConfigs, string? explicitRunner)
    {
        var command = resolve_command(target, runnerConfigs, explicitRunner, "java");
        return new JvmRunner(command);
    }

    private static IRunner create_node_runner(string target, List<RunnerConfig>? runnerConfigs, string? explicitRunner)
    {
        var command = resolve_command(target, runnerConfigs, explicitRunner, "node");
        return new NodeRunner(command);
    }

    /// <summary>
    ///     解析命令路径，按优先级：CLI --runner &gt; legion.von [runner] &gt; 环境变量 &gt; PATH 默认命令名
    /// </summary>
    private static string resolve_command(
        string target,
        List<RunnerConfig>? runnerConfigs,
        string? explicitRunner,
        string defaultCommand)
    {
        if (!string.IsNullOrWhiteSpace(explicitRunner)) return explicitRunner;

        if (runnerConfigs is not null)
        {
            var config = runnerConfigs.FirstOrDefault(c =>
                string.Equals(c.target, target, StringComparison.OrdinalIgnoreCase));
            if (config is not null && !string.IsNullOrWhiteSpace(config.command)) return config.command;
        }

        var envName = $"LEGION_RUNNER_{target.ToUpperInvariant()}";
        var envValue = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(envValue)) return envValue;

        return defaultCommand;
    }
}