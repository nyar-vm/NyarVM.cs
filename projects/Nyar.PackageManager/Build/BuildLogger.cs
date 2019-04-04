using System.Text;

namespace Nyar.PackageManager.Build;

/// <summary>
///     结构化构建日志器。
///     支持阶段标签、错误分层、摘要输出。
/// </summary>
public sealed class BuildLogger
{
    private readonly StringBuilder _buffer = new();

    /// <summary>
    ///     记录阶段开始
    /// </summary>
    /// <param name="stage">构建阶段</param>
    /// <param name="moduleName">模块名</param>
    public void log_stage_start(BuildStage stage, string moduleName)
    {
        var label = get_stage_label(stage);
        var line = $"[{label}] {moduleName}：开始...";
        _buffer.AppendLine(line);
        Console.WriteLine(line);
    }

    /// <summary>
    ///     记录阶段完成
    /// </summary>
    /// <param name="stage">构建阶段</param>
    /// <param name="elapsed">耗时</param>
    public void log_stage_end(BuildStage stage, TimeSpan elapsed)
    {
        var line = $"      完成（耗时 {elapsed.TotalMilliseconds:F0}ms）";
        _buffer.AppendLine(line);
        Console.WriteLine(line);
    }

    /// <summary>
    ///     记录构建失败
    /// </summary>
    /// <param name="result">构建结果</param>
    public void log_failure(BuildResult result)
    {
        var stageLabel = result.failed_stage is not null ? get_stage_label(result.failed_stage.Value) : "UNKNOWN";
        var line = $"[ERROR] [{stageLabel}] {result.module_name} ({result.canonical_triple})：{result.error_message}";
        _buffer.AppendLine(line);
        Console.Error.WriteLine(line);

        if (result.fix_suggestion is not null)
        {
            var suggestionLine = $"       修复建议：{result.fix_suggestion}";
            _buffer.AppendLine(suggestionLine);
            Console.Error.WriteLine(suggestionLine);
        }
    }

    /// <summary>
    ///     记录构建成功
    /// </summary>
    /// <param name="result">构建结果</param>
    public void log_success(BuildResult result)
    {
        var line =
            $"[OK] {result.module_name} ({result.canonical_triple}) → {result.output_directory}（耗时 {result.elapsed.TotalMilliseconds:F0}ms）";
        _buffer.AppendLine(line);
        Console.WriteLine(line);

        if (result.artifact_set?.run_contract is not null)
        {
            var runContract = result.artifact_set.run_contract;
            var runLine = $"     运行验证：{runContract.validation_command}";
            _buffer.AppendLine(runLine);
            Console.WriteLine(runLine);
        }
    }

    /// <summary>
    ///     记录普通信息
    /// </summary>
    /// <param name="message">消息</param>
    public void log_info(string message)
    {
        _buffer.AppendLine(message);
        Console.WriteLine(message);
    }

    /// <summary>
    ///     获取完整日志文本
    /// </summary>
    /// <returns>日志内容</returns>
    public string get_log()
    {
        return _buffer.ToString();
    }

    /// <summary>
    ///     清空日志缓冲区
    /// </summary>
    public void clear()
    {
        _buffer.Clear();
    }

    private static string get_stage_label(BuildStage stage)
    {
        return stage switch
        {
            BuildStage.lex => "LEX",
            BuildStage.parse => "PARSE",
            BuildStage.semantic => "SEMANTIC",
            BuildStage.hir => "HIR",
            BuildStage.mir => "MIR",
            BuildStage.lir => "LIR",
            BuildStage.emit => "EMIT",
            BuildStage.packaging => "PACK",
            BuildStage.flush => "FLUSH",
            _ => "?"
        };
    }
}