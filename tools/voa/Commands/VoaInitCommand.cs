using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa init 命令：初始化项目配置
/// </summary>
[Command("init", "初始化项目配置")]
public sealed class VoaInitCommand : ICommand
{
    /// <summary>
    ///     编译目标
    /// </summary>
    [Argument(0, "编译目标")]
    public string target { get; set; } = "wasm";

    /// <summary>
    ///     项目类型
    /// </summary>
    [Option("type", "项目类型")]
    public string type { get; set; } = "frontend";

    /// <summary>
    ///     是否强制覆盖
    /// </summary>
    [Option('f', "force", "强制覆盖现有配置")]
    public bool force { get; set; }

    /// <summary>
    ///     执行 init 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var vonPath = Path.Combine(currentDir, "voa.config.v");

        if (File.Exists(vonPath) && !force)
        {
            Console.WriteLine("voa.config.v 已存在，使用 --force 覆盖");
            return Task.FromResult(ExitCode.Error);
        }

        Console.WriteLine($"正在初始化 VOA 项目（类型：{type}，目标：{target}）...");

        var vonContent = VoaHelper.generate_voa_config(type, target);
        File.WriteAllText(vonPath, vonContent);

        Console.WriteLine("项目初始化完成（voa.config.v）");
        return Task.FromResult(ExitCode.Success);
    }
}
