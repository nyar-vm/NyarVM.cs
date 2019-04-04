namespace Sonic.Interactive;

/// <summary>
/// 行编辑处理器抽象，REPL 循环的"Read"阶段委托给此接口
/// 默认实现支持基本光标移动和编辑，可替换为第三方库
/// </summary>
public interface IReadLineHandler
{
    /// <summary>
    /// 异步读取一行编辑后的文本
    /// </summary>
    /// <param name="prompt">提示符</param>
    /// <param name="history">命令历史（用于 Up/Down 导航）</param>
    /// <param name="cancellation">取消令牌</param>
    /// <returns>编辑完成的文本，取消时返回 null</returns>
    Task<string?> read_line(string prompt, ReplHistory? history, CancellationToken cancellation = default);

    /// <summary>
    /// 是否支持 Tab 补全
    /// </summary>
    bool supports_completions { get; }

    /// <summary>
    /// 是否支持语法高亮
    /// </summary>
    bool supports_syntax_highlighting { get; }

    /// <summary>
    /// 是否支持多行编辑
    /// </summary>
    bool supports_multi_line { get; }
}
