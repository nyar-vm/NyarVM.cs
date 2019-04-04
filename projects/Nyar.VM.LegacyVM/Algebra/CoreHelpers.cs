namespace Nyar.VM.LegacyVM.Algebra;

/// <summary>
///     代数通用工具类，提供类型转换静态方法。
/// </summary>
public static class CoreHelpers
{
    /// <summary>
    ///     将对象转换为 64 位整数
    /// </summary>
    public static long to_i64(object val)
    {
        return val switch
        {
            long l => l,
            int i => i,
            short s => s,
            byte b => b,
            double d => (long)d,
            float f => (long)f,
            string s when long.TryParse(s, out var r) => r,
            bool b => b ? 1L : 0L,
            _ => 0L
        };
    }

    /// <summary>
    ///     将对象转换为 64 位浮点数
    /// </summary>
    public static double to_f64(object val)
    {
        return val switch
        {
            double d => d,
            float f => f,
            long l => l,
            int i => i,
            string s when double.TryParse(s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var r) => r,
            _ => 0.0
        };
    }

    /// <summary>
    ///     将对象转换为布尔值
    /// </summary>
    public static bool to_bool(object val)
    {
        return val switch
        {
            bool b => b,
            null => false,
            long l => l != 0,
            int i => i != 0,
            double d => d != 0,
            string s => s.Length > 0,
            System.Collections.IList list => list.Count > 0,
            _ => true
        };
    }

    /// <summary>
    ///     将对象转换为字符串
    /// </summary>
    public static string to_str(object? val)
    {
        return val switch
        {
            null => "null",
            string s => s,
            long l => l.ToString(),
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => val.ToString() ?? "null"
        };
    }
}