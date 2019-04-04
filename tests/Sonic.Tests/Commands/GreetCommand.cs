namespace Commander.Testing.Commands;

[Command("greet", "打印问候语")]
public partial class GreetCommand : ICommand
{
    [Argument(0, "目标名称", required = true)] public string name { get; set; } = "";

    [Option('g', "greeting", "问候语模板")] public string greeting { get; set; } = "你好";

    [Option('l', "loud", "大写输出")] public bool loud { get; set; }

    public Task<int> execute()
    {
        var message = $"{greeting}, {name}!";
        if (loud) message = message.ToUpperInvariant();

        Console.WriteLine(message);
        return Task.FromResult(0);
    }
}