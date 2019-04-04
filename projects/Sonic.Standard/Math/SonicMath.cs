namespace Std.Math;

using SystemMath = System.Math;

/// <summary>
///     `Sonic.Standard` 内部统一数学门面，避免业务层直接分散依赖 `System.Math`。
/// </summary>
public static class SonicMath
{
    public static int min(int left, int right)
    {
        return left < right ? left : right;
    }

    public static int max(int left, int right)
    {
        return left > right ? left : right;
    }

    public static long abs(long value)
    {
        return SystemMath.Abs(value);
    }

    public static double abs(double value)
    {
        return SystemMath.Abs(value);
    }

    public static double max(double left, double right)
    {
        return SystemMath.Max(left, right);
    }

    public static double min(double left, double right)
    {
        return SystemMath.Min(left, right);
    }

    public static double clamp(double value, double min, double max)
    {
        return SystemMath.Clamp(value, min, max);
    }

    public static double sqrt(double value)
    {
        return SystemMath.Sqrt(value);
    }

    public static double sin(double value)
    {
        return SystemMath.Sin(value);
    }

    public static double cos(double value)
    {
        return SystemMath.Cos(value);
    }

    public static double atan2(double y, double x)
    {
        return SystemMath.Atan2(y, x);
    }

    public static double exp(double value)
    {
        return SystemMath.Exp(value);
    }

    public static double log(double value)
    {
        return SystemMath.Log(value);
    }

    public static double arc_cosine(double value)
    {
        return SystemMath.Acos(value);
    }
}