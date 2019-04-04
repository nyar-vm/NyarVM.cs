using ExitCode = Core.Terminal.ExitCode;

namespace Legion.CLI.Commands;

/// <summary>
///     legion spy mir 子模式：dump 指定函数的 MIR（占位，待实现）
/// </summary>
internal static partial class LegionSpyMir
{
    /// <summary>
    ///     执行 MIR dump
    /// </summary>
    public static Task<ExitCode> run(string? project, string? func, bool json)
    {
        Console.Error.WriteLine("legion spy mir 尚未实现");
        return Task.FromResult(ExitCode.Error);
    }
}
