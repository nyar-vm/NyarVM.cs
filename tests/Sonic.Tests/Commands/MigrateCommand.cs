using Core.Terminal;

namespace Commander.Testing.Commands;

[Command("migrate", "数据库迁移")]
public partial class MigrateCommand : ICommand
{
    [Option('t', "target", "目标版本")] public string? target { get; set; }

    [Option("dry-run", "试运行")] public bool dry_run { get; set; }

    public bool after_parse_called { get; set; }

    [AfterParse]
    public void OnParsed()
    {
        after_parse_called = true;
    }

    public Task<int> execute()
    {
        return Task.FromResult(0);
    }
}