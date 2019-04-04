namespace Commander.Testing.Commands;

[Command("calc", "计算工具")]
public partial class CalcCommand : ICommand
{
    [Option('p', "precision", "精度", environment_variable = "PRECISION")]
    public int precision { get; set; } = 2;

    [Option('f', "format", "输出格式", environment_variable = "FORMAT")]
    public string? format { get; set; }

    public Task<int> execute()
    {
        return Task.FromResult(0);
    }
}