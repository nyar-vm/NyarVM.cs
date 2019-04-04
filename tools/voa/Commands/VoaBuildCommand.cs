using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nyar.Language.Awsl.Asgard.Compiler;
using Nyar.Language.Valkyrie.Config;
using Nyar.Types.Targets;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa build 命令：构建项目
/// </summary>
[Command("build", "构建项目")]
public sealed class VoaBuildCommand : ICommand
{
    /// <summary>
    ///     支持的编译目标列表
    /// </summary>
    private static readonly string[] _supported_targets =
    [
        "wasm", "wasip1", "wasip2", "clr", "jvm", "native", "nyar", "gnosis"
    ];

    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string project { get; set; } = ".";

    /// <summary>
    ///     编译目标
    /// </summary>
    [Option('t', "target", "编译目标")]
    public string target { get; set; } = "wasm";

    /// <summary>
    ///     是否监视文件变更
    /// </summary>
    [Option('w', "watch", "监视文件变更并自动重建")]
    public bool watch { get; set; }

    /// <summary>
    ///     是否输出详细信息
    /// </summary>
    [Option('v', "verbose", "输出详细信息")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 build 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = VoaHelper.resolve_voa_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'（需包含 voa.von 文件）");
            return Task.FromResult(ExitCode.Error);
        }

        if (Array.IndexOf(_supported_targets, target) < 0)
        {
            Console.Error.WriteLine($"错误：不支持的编译目标 '{target}'，可选：{string.Join(" / ", _supported_targets)}");
            return Task.FromResult(ExitCode.Error);
        }

        var configLoader = new VoaConfigLoader();
        var projectConfig = configLoader.load(projectDir);
        var buildConfig = projectConfig.build ?? new VoaBuildConfig();
        buildConfig.app_name ??= projectConfig.name;
        buildConfig.entry ??= projectConfig.entry;
        buildConfig.html_template ??= projectConfig.html_template;
        buildConfig.runtime ??= projectConfig.runtime;
        buildConfig.target_mode = watch ? TargetMode.dev : TargetMode.prod;
        var outDir = Path.Combine(projectDir, buildConfig.output);

        if (verbose)
        {
            Console.WriteLine($"正在构建 {projectDir} → {target}...");
            Console.WriteLine($"  输出目录：{outDir}");
            Console.WriteLine($"  构建模式：{buildConfig.target_mode}");
        }

        var builder = new AsgardMultiTargetBuilder();
        var result = builder.build(buildConfig, projectDir, outDir, target, verbose);
        if (!result.success)
        {
            Console.Error.WriteLine($"构建失败：{result.error}");
            return Task.FromResult(ExitCode.Error);
        }

        var ssgResult = VoaHelper.run_ssg(projectDir, result.output_directory, verbose);
        if (!ssgResult.success)
        {
            Console.Error.WriteLine($"SSG 生成失败：{ssgResult.error}");
            return Task.FromResult(ExitCode.Error);
        }

        Console.WriteLine($"构建完成 → {result.output_directory}");

        if (watch)
        {
            Console.WriteLine("正在监视文件变更...");
            using var watcher = new FileSystemWatcher(projectDir)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            watcher.Changed += (_, e) =>
            {
                Console.WriteLine($"检测到文件变更：{Path.GetFileName(e.FullPath)}");
                Console.WriteLine("重新构建...");
                var watchBuilder = new AsgardMultiTargetBuilder();
                var rebuild = watchBuilder.build(buildConfig, projectDir, outDir, target, verbose);
                if (rebuild.success)
                {
                    VoaHelper.run_ssg(projectDir, rebuild.output_directory, verbose);
                    Console.WriteLine("重建完成");
                }
            };

            Console.WriteLine("按 Ctrl+C 停止");
            while (!cancel.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
        }

        return Task.FromResult(ExitCode.Success);
    }
}
