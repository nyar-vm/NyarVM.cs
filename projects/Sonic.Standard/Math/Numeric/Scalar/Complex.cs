using Core.Math;
using Std.Math.Foundation;

namespace Std.Math.Numeric.Scalar;

/// <summary>
///     澶嶆暟缁撴瀯浣擄紝琛ㄧ�?a + bi 褰㈠紡鐨勫�?///
/// </summary>
/// <typeparam name="T">瀹為儴鍜岃櫄閮ㄧ殑鏁板€肩被鍨嬶紝蹇呴』涓烘诞鐐圭被鍨?/typeparam>
public readonly struct Complex<T> :
    IAdditive<Complex<T>>,
    IMultiplicative<Complex<T>>,
    INegation<Complex<T>, Complex<T>>,
    IApproximate<Complex<T>>,
    IEquatable<Complex<T>>,
    IClone<Complex<T>>
    where T : struct, IEquatable<T>, IConvertible
{
    /// <summary>瀹為�?/summary>
    public readonly T real;

    /// <summary>铏氶�?/summary>
    public readonly T imaginary;

    /// <summary>
    ///     鏋勯€犲鏁?    ///
    /// </summary>
    /// <param name="real">
    ///     瀹為�?/param>
    ///     <param name="imaginary">铏氶�?/param>
    public Complex(T real, T imaginary)
    {
        this.real = real;
        this.imaginary = imaginary;
    }

    #region 绫诲瀷杞崲杈呭�?

    /// <summary>
    ///     灏嗘硾鍨嬪€艰浆鎹负鍙岀簿搴︽诞鐐规�?    ///
    /// </summary>
    private static double to_double(T value)
    {
        return value.ToDouble(null);
    }

    /// <summary>
    ///     灏嗗弻绮惧害娴偣鏁拌浆鎹负娉涘瀷鍊?    ///
    /// </summary>
    private static T from_double(double value)
    {
        return (T)Convert.ChangeType(value, typeof(T));
    }

    #endregion

    #region 宸ュ巶鏂规硶

    /// <summary>
    ///     浠庢瀬鍧愭爣鏋勯€犲鏁?    ///
    /// </summary>
    /// <param name="magnitude">
    ///     妯￠�?/param>
    ///     <param name="phase">
    ///         杈愯锛堝姬搴︼�?/param>
    ///         <returns>瀵瑰簲鐨勫�?/returns>
    public static Complex<T> from_polar(T magnitude, T phase)
    {
        var r = to_double(magnitude);
        var theta = to_double(phase);
        return new Complex<T>(
            from_double(r * SonicMath.cos(theta)),
            from_double(r * SonicMath.sin(theta))
        );
    }

    #endregion

    #region 灞炴�?

    /// <summary>
    ///     妯￠暱锛屽嵆澶嶆暟鍒板師鐐圭殑璺濈 sqrt(real^2 + imaginary^2)
    /// </summary>
    public double magnitude()
    {
        var a = to_double(real);
        var b = to_double(imaginary);
        return SonicMath.sqrt(a * a + b * b);
    }

    /// <summary>
    ///     妯￠暱鐨勫钩�?real^2 + imaginary^2锛岄伩鍏嶅紑鏂硅繍绠?    ///
    /// </summary>
    public double magnitude_squared()
    {
        var a = to_double(real);
        var b = to_double(imaginary);
        return a * a + b * b;
    }

    /// <summary>
    ///     杈愯锛堢浉浣嶈锛夛紝鍗冲鏁颁笌姝ｅ疄杞寸殑澶硅�?atan2(imaginary, real)
    /// </summary>
    public double phase()
    {
        return SonicMath.atan2(to_double(imaginary), to_double(real));
    }

    /// <summary>
    ///     鍏辫江澶嶆暟锛岃櫄閮ㄥ彇�?    ///
    /// </summary>
    public Complex<T> conjugate()
    {
        return new Complex<T>(real, from_double(-to_double(imaginary)));
    }

    #endregion

    #region IAdditive

    /// <summary>
    ///     澶嶆暟鍔犳硶 (a + bi) + (c + di) = (a+c) + (b+d)i
    /// </summary>
    public Complex<T> add(Complex<T> right)
    {
        return new Complex<T>(
            from_double(to_double(real) + to_double(right.real)),
            from_double(to_double(imaginary) + to_double(right.imaginary))
        );
    }

    /// <summary>
    ///     澶嶆暟鍑忔硶 (a + bi) - (c + di) = (a-c) + (b-d)i
    /// </summary>
    public Complex<T> sub(Complex<T> right)
    {
        return new Complex<T>(
            from_double(to_double(real) - to_double(right.real)),
            from_double(to_double(imaginary) - to_double(right.imaginary))
        );
    }

    /// <summary>
    ///     复数加法运算符
    /// </summary>
    /// <param name="left">左侧操作数</param>
    /// <param name="right">右侧操作数</param>
    /// <returns>相加结果</returns>
    public static Complex<T> operator +(Complex<T> left, Complex<T> right)
    {
        return left.add(right);
    }

    #endregion

    #region IMultiplicative

    /// <summary>
    ///     澶嶆暟涔樻硶 (a + bi) * (c + di) = (ac - bd) + (ad + bc)i
    /// </summary>
    public Complex<T> mul(Complex<T> right)
    {
        var a = to_double(real);
        var b = to_double(imaginary);
        var c = to_double(right.real);
        var d = to_double(right.imaginary);
        return new Complex<T>(
            from_double(a * c - b * d),
            from_double(a * d + b * c)
        );
    }

    /// <summary>
    ///     澶嶆暟闄ゆ硶 (a + bi) / (c + di) = ((ac + bd) + (bc - ad)i) / (c^2 + d^2)
    /// </summary>
    public Complex<T> div(Complex<T> right)
    {
        var a = to_double(real);
        var b = to_double(imaginary);
        var c = to_double(right.real);
        var d = to_double(right.imaginary);
        var denominator = c * c + d * d;
        return new Complex<T>(
            from_double((a * c + b * d) / denominator),
            from_double((b * c - a * d) / denominator)
        );
    }

    /// <summary>
    ///     复数乘法运算符
    /// </summary>
    /// <param name="left">左侧操作数</param>
    /// <param name="right">右侧操作数</param>
    /// <returns>乘积结果</returns>
    public static Complex<T> operator *(Complex<T> left, Complex<T> right)
    {
        return left.mul(right);
    }

    #endregion

    #region INegation

    /// <summary>
    ///     澶嶆暟鍙栬礋 -(a + bi) = -a + (-b)i
    /// </summary>
    public Complex<T> neg()
    {
        return new Complex<T>(
            from_double(-to_double(real)),
            from_double(-to_double(imaginary))
        );
    }

    #endregion

    #region IApproximate

    /// <summary>
    ///     鍒ゆ柇涓や釜澶嶆暟鏄惁鍦ㄧ粰瀹氳宸寖鍥村唴杩戜技鐩哥瓑锛屽垎鍒瘮杈冨疄閮ㄥ拰铏氶儴
    /// </summary>
    /// <param name="other">姣旇緝鐩爣</param>
    /// <param name="absoluteTolerance">
    ///     缁濆璇樊瀹归�?/param>
    ///     <param name="relativeTolerance">鐩稿璇樊瀹归�?/param>
    public bool is_close(Complex<T> other, double absoluteTolerance, double relativeTolerance)
    {
        return is_component_close(to_double(real), to_double(other.real), absoluteTolerance, relativeTolerance)
               && is_component_close(to_double(imaginary), to_double(other.imaginary), absoluteTolerance,
                   relativeTolerance);
    }

    /// <summary>
    ///     判断两个复数是否在给定逐分量容差范围内近似相等
    /// </summary>
    /// <param name="other">比较目标</param>
    /// <param name="epsilon">逐分量容差</param>
    /// <returns>是否近似相等</returns>
    public bool approx_equals(Complex<T> other, Complex<T> epsilon)
    {
        return SonicMath.abs(to_double(real) - to_double(other.real)) <= to_double(epsilon.real)
               && SonicMath.abs(to_double(imaginary) - to_double(other.imaginary)) <= to_double(epsilon.imaginary);
    }

    /// <summary>
    ///     鍒ゆ柇鍗曚釜鍒嗛噺鏄惁鍦ㄨ宸寖鍥村唴杩戜技鐩哥�?    ///
    /// </summary>
    private static bool is_component_close(double a, double b, double absoluteTolerance, double relativeTolerance)
    {
        var diff = SonicMath.abs(a - b);
        if (diff <= absoluteTolerance) return true;

        var maxAbs = SonicMath.max(SonicMath.abs(a), SonicMath.abs(b));
        return diff <= relativeTolerance * maxAbs;
    }

    #endregion

    #region IEquatable

    /// <summary>
    ///     鍒ゆ柇涓や釜澶嶆暟鏄惁鐩哥瓑锛屽疄閮ㄥ拰铏氶儴鍧囩浉绛夋椂杩斿�?true
    /// </summary>
    public bool Equals(Complex<T> other)
    {
        return real.Equals(other.real) && imaginary.Equals(other.imaginary);
    }

    #endregion

    #region IClone

    /// <summary>
    ///     杩斿洖褰撳墠澶嶆暟鐨勬繁鎷疯礉锛岀敱浜庢槸 readonly struct锛岀洿鎺ヨ繑鍥炶嚜韬嵆�?    ///
    /// </summary>
    public Complex<T> clone()
    {
        return new Complex<T>(real, imaginary);
    }

    #endregion

    #region 澶嶆暟鏁板鍑芥�?

    /// <summary>
    ///     澶嶆暟姝ｅ鸡 sin(a + bi) = sin(a)*cosh(b) + i*cos(a)*sinh(b)
    /// </summary>
    /// <param name="z">杈撳叆澶嶆暟</param>
    /// <returns>姝ｅ鸡鍊?/returns>
    public static Complex<T> sine(Complex<T> z)
    {
        var a = to_double(z.real);
        var b = to_double(z.imaginary);
        var sinhB = (SonicMath.exp(b) - SonicMath.exp(-b)) * 0.5;
        var coshB = (SonicMath.exp(b) + SonicMath.exp(-b)) * 0.5;
        return new Complex<T>(
            from_double(SonicMath.sin(a) * coshB),
            from_double(SonicMath.cos(a) * sinhB)
        );
    }

    /// <summary>
    ///     澶嶆暟浣欏鸡 cos(a + bi) = cos(a)*cosh(b) - i*sin(a)*sinh(b)
    /// </summary>
    /// <param name="z">杈撳叆澶嶆暟</param>
    /// <returns>浣欏鸡鍊?/returns>
    public static Complex<T> cosine(Complex<T> z)
    {
        var a = to_double(z.real);
        var b = to_double(z.imaginary);
        var sinhB = (SonicMath.exp(b) - SonicMath.exp(-b)) * 0.5;
        var coshB = (SonicMath.exp(b) + SonicMath.exp(-b)) * 0.5;
        return new Complex<T>(
            from_double(SonicMath.cos(a) * coshB),
            from_double(-SonicMath.sin(a) * sinhB)
        );
    }

    /// <summary>
    ///     澶嶆暟鎸囨暟 e^(a + bi) = e^a * (cos(b) + i*sin(b))
    /// </summary>
    /// <param name="z">杈撳叆澶嶆暟</param>
    /// <returns>鎸囨暟鍊?/returns>
    public static Complex<T> exponential(Complex<T> z)
    {
        var a = to_double(z.real);
        var b = to_double(z.imaginary);
        var expA = SonicMath.exp(a);
        return new Complex<T>(
            from_double(expA * SonicMath.cos(b)),
            from_double(expA * SonicMath.sin(b))
        );
    }

    /// <summary>
    ///     澶嶆暟鑷劧瀵规�?ln(z) = ln(|z|) + i*arg(z)
    /// </summary>
    /// <param name="z">杈撳叆澶嶆暟</param>
    /// <returns>鑷劧瀵规暟鍊?/returns>
    public static Complex<T> logarithm(Complex<T> z)
    {
        var mag = z.magnitude();
        var arg = z.phase();
        return new Complex<T>(
            from_double(SonicMath.log(mag)),
            from_double(arg)
        );
    }

    #endregion
}