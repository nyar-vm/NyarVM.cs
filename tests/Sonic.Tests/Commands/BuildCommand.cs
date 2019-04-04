namespace Commander.Testing.Commands;

[Command("build", "构建项目")]
public partial class BuildCommand : ICommand
{
    [Option('c', "config", "构建配置")] public string config { get; set; } = "Debug";

    [Option('j', "jobs", "并行任务数")] public int jobs { get; set; } = 1;

    [Option('v', "verbose", "详细输出")] public bool verbose { get; set; }

    [Option('o', "output", "输出目录")] public string? output { get; set; }

    public Task<int> execute()
    {
        return Task.FromResult(0);
    }
}