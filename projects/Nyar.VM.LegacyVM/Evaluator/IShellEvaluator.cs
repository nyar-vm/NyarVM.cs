namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     Shell 语言求值器统一接口。
///     Bash、Batch、PowerShell 求值器均实现此接口，提供统一的 Shell 脚本求值入口。
/// </summary>
public interface IShellEvaluator
{
    /// <summary>
    ///     Shell 可执行文件名（如 "bash", "cmd.exe", "powershell.exe"）
    /// </summary>
    string shell_name { get; }

    /// <summary>
    ///     求值 Shell 脚本
    /// </summary>
    /// <param name="source">Shell 脚本源码</param>
    /// <param name="env">运行环境变量</param>
    /// <returns>执行结果</returns>
    object evaluate(string source, Dictionary<string, object> env);
}