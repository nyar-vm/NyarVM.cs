namespace Commander.Testing.Commands;

[Command("echo", "无选项命令，用于测试纯位置参数")]
public partial class EchoCommand : ICommand
{
    [Argument(0, "回显内容", required = true)] public string message { get; set; } = "";

    [Argument(1, "重复次数")] public int count { get; set; } = 1;

    public Task<int> execute()
    {
        for (var i = 0; i < count; i++) Console.WriteLine(message);

        return Task.FromResult(0);
    }
}