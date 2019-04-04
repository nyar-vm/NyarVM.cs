namespace Commander.Testing.Commands;

[Command("test", "测试当前项目")]
public partial class ProjectTestCommand : ICommand
{
    [Argument(0, "项目路径")] public string? project { get; set; }

    [Option('t', "target", "目标平台")] public string? target { get; set; }

    public Task<int> execute()
    {
        return Task.FromResult(0);
    }
}
