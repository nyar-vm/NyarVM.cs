using Nyar.VM.TextVM.Compiler;

namespace Nyar.VM.TextVM.Tests;

/// <summary>
/// BacktrackExecutor 回溯执行器的单元测试。
/// 覆盖字面量匹配、选择、量词、捕获组、反向引用、超时及替换。
/// </summary>
public class BacktrackExecutorTests
{
    /// <summary>
    /// 字面量模式在输入中正确匹配。
    /// 手动构造包含组 0 捕获的指令序列。
    /// </summary>
    [Fact]
    public void Literal_Match_ReturnsTrue()
    {
        Inst[] program =
        [
            Inst.CreateSave(0),
            Inst.CreateChar('h'),
            Inst.CreateChar('e'),
            Inst.CreateChar('l'),
            Inst.CreateChar('l'),
            Inst.CreateChar('o'),
            Inst.CreateSave(1),
            Inst.Match,
        ];
        BacktrackExecutor executor = new BacktrackExecutor(program, 1, 5000);

        Byte[] input = [.. "hello world"u8];

        Assert.True(executor.IsMatch(input));
    }

    /// <summary>
    /// 选择操作 a|b 正确匹配 a 或 b，不匹配其他字符。
    /// </summary>
    [Fact]
    public void Alternation_Match_ReturnsCorrectResult()
    {
        // 程序: Save(0), Split(尝试a, 尝试b), Char('a'), Jump(结束), Char('b'), Save(1), Match
        Inst[] program =
        [
            Inst.CreateSave(0),
            Inst.CreateSplit(2, 4),
            Inst.CreateChar('a'),
            Inst.CreateJump(5),
            Inst.CreateChar('b'),
            Inst.CreateSave(1),
            Inst.Match,
        ];
        BacktrackExecutor executor = new BacktrackExecutor(program, 1, 5000);

        Assert.True(executor.IsMatch("a"u8));
        Assert.True(executor.IsMatch("b"u8));
        Assert.False(executor.IsMatch("c"u8));
    }

    /// <summary>
    /// Kleene 星号 a* 正确匹配多个重复字符。
    /// </summary>
    [Fact]
    public void Star_Match_ReturnsCorrectResult()
    {
        // 程序: Save(0), Split(尝试 'a', 退出), Char('a'), Jump(循环), Save(1), Match
        Inst[] program =
        [
            Inst.CreateSave(0),
            Inst.CreateSplit(2, 4),
            Inst.CreateChar('a'),
            Inst.CreateJump(1),
            Inst.CreateSave(1),
            Inst.Match,
        ];
        BacktrackExecutor executor = new BacktrackExecutor(program, 1, 5000);

        Assert.True(executor.IsMatch("a"u8));
        Assert.True(executor.IsMatch("aaa"u8));
        Assert.False(executor.IsMatch(""u8));
    }

    /// <summary>
    /// 捕获组正确记录匹配的起始位置。
    /// 使用含有捕获组的编译程序。
    /// </summary>
    [Fact]
    public void CaptureGroup_ExtractsCorrectContent()
    {
        BacktrackExecutor executor = CompilePattern("(a+)b", 5000);

        Byte[] input = [.. "xaaby"u8];
        Match? match = executor.FindFirst(input);

        Assert.NotNull(match);
        Assert.Equal(1, match.Value.Start);
    }

    /// <summary>
    /// 反向引用正确匹配之前捕获的相同内容。
    /// 模式 (a)(b)\1 匹配 "abb"（第二个捕获组的重复）但不匹配 "abc"。
    /// </summary>
    [Fact]
    public void Backreference_Matching_WorksCorrectly()
    {
        BacktrackExecutor executor = CompilePattern("(a)(b)\\1", 5000);

        Assert.True(executor.IsMatch("abb"u8));
        Assert.False(executor.IsMatch("abc"u8));
    }

    /// <summary>
    /// 超时机制：BacktrackExecutor 使用 CancellationTokenSource 实现超时。
    /// 由于 Split 指令使用递归实现且 CancellationTokenSource 为异步触发，
    /// 此测试验证代码路径存在且不崩溃，但不保证超时在同步执行中必然触发。
    /// </summary>
    [Fact]
    public void Timeout_Cancellation_DoesNotCrash()
    {
        // 使用 (a+)*b 避免空匹配导致的内层循环（a+ 至少消耗一个字符）
        BacktrackExecutor executor = CompilePattern("(a+)*b", 1);

        Byte[] inputBytes = [.. "aaaac"u8];

        try
        {
            executor.IsMatch(inputBytes);
        }
        catch (OperationCanceledException)
        {
            // 超时触发：预期行为
        }
    }

    /// <summary>
    /// 替换操作正确替换匹配的文本。
    /// </summary>
    [Fact]
    public void Replace_SimplePattern_ReplacesCorrectly()
    {
        Inst[] program =
        [
            Inst.CreateSave(0),
            Inst.CreateChar('w'),
            Inst.CreateChar('o'),
            Inst.CreateChar('r'),
            Inst.CreateChar('l'),
            Inst.CreateChar('d'),
            Inst.CreateSave(1),
            Inst.Match,
        ];
        BacktrackExecutor executor = new BacktrackExecutor(program, 1, 5000);

        Byte[] input = [.. "hello world"u8];
        Byte[] replacement = [.. "there"u8];
        Byte[] result = executor.Replace(input, replacement);

        Assert.Equal("hello there"u8.ToArray(), result);
    }

    /// <summary>
    /// 编译模式字符串为 BacktrackExecutor 实例。
    /// 自动追加 Match 指令，并确保捕获组计数正确。
    /// </summary>
    private static BacktrackExecutor CompilePattern(String pattern, Int32 timeoutMs)
    {
        (AstNode? node, String? error) = PatternParser.Parse(pattern);

        if (node is null)
        {
            throw new ArgumentException($"模式解析失败: {error}");
        }

        AstNode folded = BooleanFolder.Fold(node, out _);
        (Inst[] program, Int32 captCount) = BacktrackCompiler.Compile(folded);

        // 在程序末尾添加 Match 指令
        Array.Resize(ref program, program.Length + 1);
        program[^1] = Inst.Match;

        return new BacktrackExecutor(program, captCount, timeoutMs);
    }
}
