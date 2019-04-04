namespace Commander.Testing.Commands;

[Command("deploy", "部署管理")]
public partial class DeployCommand : ICommand
{
    [Option('e', "environment", "目标环境")] public string? environment { get; set; }

    public DeployAction action { get; set; }

    public Task<int> execute()
    {
        return Task.FromResult(0);
    }

    public Task<int> execute()
    {
        return Task.FromResult(0);
    }
}