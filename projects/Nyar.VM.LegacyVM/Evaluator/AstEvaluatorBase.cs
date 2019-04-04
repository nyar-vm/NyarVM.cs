using Nyar.VM.LegacyVM.Algebra;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     AST 求值器抽象基类。
///     提供通用的变量环境、函数环境和 CoreEvaluator 集成。
/// </summary>
/// <typeparam name="TAstNode">AST 节点类型</typeparam>
public abstract class AstEvaluatorBase<TAstNode> : IAstEvaluator<TAstNode>
{
    /// <summary>
    ///     变量环境
    /// </summary>
    protected readonly Dictionary<string, object> _env;

    /// <summary>
    ///     函数环境
    /// </summary>
    protected readonly Dictionary<string, Func<object[], object>> _functions = new();

    /// <summary>
    ///     Core 代数求值器
    /// </summary>
    protected readonly CoreEvaluator _core;

    /// <summary>
    ///     创建 AST 求值器
    /// </summary>
    /// <param name="env">初始变量环境</param>
    protected AstEvaluatorBase(Dictionary<string, object> env)
    {
        _env = new Dictionary<string, object>(env);
        _core = new CoreEvaluator();
    }

    /// <summary>
    ///     求值 AST 根节点
    /// </summary>
    /// <param name="ast">AST 根节点</param>
    /// <returns>求值结果</returns>
    public abstract object evaluate(TAstNode ast);

    /// <summary>
    ///     将值转换为整数
    /// </summary>
    protected static long to_int(object val)
    {
        return val switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            float f => (long)f,
            string s when long.TryParse(s, out var r) => r,
            _ => 0
        };
    }

    /// <summary>
    ///     将值转换为浮点数
    /// </summary>
    protected static double to_float(object val)
    {
        return val switch
        {
            double d => d,
            float f => f,
            long l => l,
            int i => i,
            string s when double.TryParse(s, out var r) => r,
            _ => 0.0
        };
    }

    /// <summary>
    ///     将值转换为布尔
    /// </summary>
    protected static bool to_bool(object val)
    {
        return val switch
        {
            bool b => b,
            long l => l != 0,
            int i => i != 0,
            double d => d != 0,
            null => false,
            string s => s.Length > 0,
            _ => true
        };
    }

    /// <summary>
    ///     将值转换为字符串
    /// </summary>
    protected static string to_str(object? val)
    {
        return val?.ToString() ?? "null";
    }

    /// <summary>
    ///     打印输出（映射到 Console.WriteLine）
    /// </summary>
    protected static void print_output(object val)
    {
        Console.WriteLine(to_str(val));
    }
}