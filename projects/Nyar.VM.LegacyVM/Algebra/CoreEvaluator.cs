namespace Nyar.VM.LegacyVM.Algebra;

/// <summary>
///     代数求值器，基于字典环境的解释执行。
///     提供基础运算与控制流的运行时语义，供各语言 AST 求值器使用。
/// </summary>
public class CoreEvaluator
{
    private readonly Dictionary<string, object> _env;

    /// <summary>
    ///     创建求值器
    /// </summary>
    /// <param name="env">初始变量环境，为 null 时创建空环境</param>
    public CoreEvaluator(Dictionary<string, object>? env = null)
    {
        _env = env ?? new Dictionary<string, object>();
    }

    /// <summary>
    ///     unit / null 值
    /// </summary>
    public object unit()
    {
        return null!;
    }

    /// <summary>
    ///     整数常量
    /// </summary>
    public object int_const(long value)
    {
        return value;
    }

    /// <summary>
    ///     浮点数常量
    /// </summary>
    public object float_const(double value)
    {
        return value;
    }

    /// <summary>
    ///     布尔常量
    /// </summary>
    public object bool_const(bool value)
    {
        return value;
    }

    /// <summary>
    ///     字符串常量
    /// </summary>
    public object str_const(string value)
    {
        return value;
    }

    /// <summary>
    ///     获取变量值
    /// </summary>
    public object var(string name)
    {
        if (_env.TryGetValue(name, out var value))
        {
            return value;
        }

        return null!;
    }

    /// <summary>
    ///     设置变量值
    /// </summary>
    public object set_var(string name, object value)
    {
        _env[name] = value;
        return value;
    }

    /// <summary>
    ///     加法（支持字符串拼接和数字加法）
    /// </summary>
    public object add(object left, object right)
    {
        if (left is string ls && right is string rs)
        {
            return ls + rs;
        }

        var l = CoreHelpers.to_f64(left);
        var r = CoreHelpers.to_f64(right);
        var result = l + r;

        if (IsIntegral(left) && IsIntegral(right))
        {
            return (long)result;
        }

        return result;
    }

    /// <summary>
    ///     减法
    /// </summary>
    public object sub(object left, object right)
    {
        var l = CoreHelpers.to_f64(left);
        var r = CoreHelpers.to_f64(right);
        var result = l - r;

        if (IsIntegral(left) && IsIntegral(right))
        {
            return (long)result;
        }

        return result;
    }

    /// <summary>
    ///     乘法
    /// </summary>
    public object mul(object left, object right)
    {
        var l = CoreHelpers.to_f64(left);
        var r = CoreHelpers.to_f64(right);
        var result = l * r;

        if (IsIntegral(left) && IsIntegral(right))
        {
            return (long)result;
        }

        return result;
    }

    /// <summary>
    ///     除法
    /// </summary>
    public object div(object left, object right)
    {
        var l = CoreHelpers.to_f64(left);
        var r = CoreHelpers.to_f64(right);

        if (r == 0)
        {
            return double.NaN;
        }

        var result = l / r;

        if (IsIntegral(left) && IsIntegral(right))
        {
            return (long)result;
        }

        return result;
    }

    /// <summary>
    ///     取模
    /// </summary>
    public object mod(object left, object right)
    {
        var l = CoreHelpers.to_i64(left);
        var r = CoreHelpers.to_i64(right);

        if (r == 0)
        {
            return 0L;
        }

        return l % r;
    }

    /// <summary>
    ///     等于
    /// </summary>
    public object eq(object left, object right)
    {
        return EqImpl(left, right);
    }

    /// <summary>
    ///     不等于
    /// </summary>
    public object ne(object left, object right)
    {
        return !EqImpl(left, right);
    }

    /// <summary>
    ///     小于
    /// </summary>
    public object lt(object left, object right)
    {
        var l = CoreHelpers.to_f64(left);
        var r = CoreHelpers.to_f64(right);
        return l < r;
    }

    /// <summary>
    ///     大于
    /// </summary>
    public object gt(object left, object right)
    {
        var l = CoreHelpers.to_f64(left);
        var r = CoreHelpers.to_f64(right);
        return l > r;
    }

    /// <summary>
    ///     小于等于
    /// </summary>
    public object lte(object left, object right)
    {
        var l = CoreHelpers.to_f64(left);
        var r = CoreHelpers.to_f64(right);
        return l <= r;
    }

    /// <summary>
    ///     大于等于
    /// </summary>
    public object gte(object left, object right)
    {
        var l = CoreHelpers.to_f64(left);
        var r = CoreHelpers.to_f64(right);
        return l >= r;
    }

    /// <summary>
    ///     逻辑与
    /// </summary>
    public object and(object left, object right)
    {
        var lb = CoreHelpers.to_bool(left);
        var rb = CoreHelpers.to_bool(right);
        return lb && rb;
    }

    /// <summary>
    ///     逻辑或
    /// </summary>
    public object or(object left, object right)
    {
        var lb = CoreHelpers.to_bool(left);
        var rb = CoreHelpers.to_bool(right);
        return lb || rb;
    }

    /// <summary>
    ///     逻辑非
    /// </summary>
    public object not(object operand)
    {
        return !CoreHelpers.to_bool(operand);
    }

    /// <summary>
    ///     条件表达式（if-then-else）
    /// </summary>
    public object @if(object condition, object thenBranch, object elseBranch)
    {
        if (CoreHelpers.to_bool(condition))
        {
            return thenBranch;
        }

        return elseBranch;
    }

    /// <summary>
    ///     顺序执行多个语句，返回最后一个的值
    /// </summary>
    public object block(object[] statements)
    {
        if (statements.Length == 0)
        {
            return null!;
        }

        return statements[^1];
    }

    /// <summary>
    ///     创建 lambda 闭包
    /// </summary>
    public object lambda(string[] parameters, object body)
    {
        var closureEnv = new Dictionary<string, object>(_env);
        return new LambdaClosure(parameters, body, closureEnv);
    }

    /// <summary>
    ///     应用函数
    /// </summary>
    public object apply(object func, object[] args)
    {
        if (func is LambdaClosure closure)
        {
            var savedEnv = new Dictionary<string, object>(_env);

            foreach (var kvp in closure.CapturedEnvironment)
            {
                _env[kvp.Key] = kvp.Value;
            }

            for (var i = 0; i < closure.Parameters.Length && i < args.Length; i++)
            {
                _env[closure.Parameters[i]] = args[i];
            }

            var result = closure.Body;

            _env.Clear();
            foreach (var kvp in savedEnv)
            {
                _env[kvp.Key] = kvp.Value;
            }

            return result;
        }

        if (func is BuiltinFunction builtin)
        {
            return builtin.Implementation(args);
        }

        return null!;
    }

    /// <summary>
    ///     创建返回值哨兵
    /// </summary>
    public object @return(object value)
    {
        return new ReturnValue(value);
    }

    /// <summary>
    ///     解包返回值哨兵（用于模块顶层）
    /// </summary>
    public object eval_with_return_unwrap(object value)
    {
        if (value is ReturnValue rv)
        {
            return rv.Value;
        }

        return value;
    }

    #region 私有方法

    /// <summary>
    ///     判断值是否为整数类型
    /// </summary>
    private static bool IsIntegral(object val)
    {
        return val is long or int or short or byte;
    }

    /// <summary>
    ///     相等性比较实现
    /// </summary>
    private static bool EqImpl(object? left, object? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is long or int or short or double or float)
        {
            var l = CoreHelpers.to_f64(left);
            var r = CoreHelpers.to_f64(right);
            return Math.Abs(l - r) < double.Epsilon;
        }

        if (left is string ls && right is string rs)
        {
            return ls == rs;
        }

        if (left is bool lb && right is bool rb)
        {
            return lb == rb;
        }

        return false;
    }

    #endregion
}