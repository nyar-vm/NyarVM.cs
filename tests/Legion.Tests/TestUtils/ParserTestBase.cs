using Xunit;

namespace Valkyrie.PackageManager.Tests.TestUtils;

/// <summary>
///     Parser 测试基类，提供语法分析器测试的通用工具方法
/// </summary>
/// <typeparam name="TAstNode">AST 节点类型</typeparam>
/// <typeparam name="TInput">解析器输入类型</typeparam>
public abstract class ParserTestBase<TInput, TAstNode> where TAstNode : class
{
    /// <summary>
    ///     默认超时时间（毫秒），防止语法分析器死循环
    /// </summary>
    protected virtual int _timeout_ms => 5000;

    /// <summary>
    ///     执行语法分析并应用超时保护
    /// </summary>
    protected TAstNode parse_with_timeout(IParser<TInput, TAstNode> parser, TInput input)
    {
        TAstNode? result = null;
        Exception? exception = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = parser.Parse(input);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.Start();

        if (!thread.Join(_timeout_ms))
        {
            thread.Interrupt();
            thread.Join(100);
            throw new TimeoutException($"语法分析器在 {_timeout_ms}ms 内未完成，可能存在死循环");
        }

        if (exception is not null) throw exception;

        return result!;
    }

    /// <summary>
    ///     断言解析结果不为空
    /// </summary>
    protected static void assert_parse_result_not_null(TAstNode result)
    {
        Assert.NotNull(result);
    }

    /// <summary>
    ///     断言没有诊断错误
    /// </summary>
    protected static void assert_no_errors(DiagnosticSink diagnostics)
    {
        Assert.False(diagnostics.HasErrors, $"语法分析产生了意外的错误：\n{diagnostics.FormatAll()}");
    }

    /// <summary>
    ///     断言存在指定数量的错误
    /// </summary>
    protected static void assert_error_count(DiagnosticSink diagnostics, int expectedCount)
    {
        Assert.Equal(expectedCount, diagnostics.Errors.Count);
    }

    /// <summary>
    ///     断言抛出 ParseException
    /// </summary>
    protected static void assert_throws_parse_exception(Action action)
    {
        Assert.Throws<ParseException>(action);
    }
}