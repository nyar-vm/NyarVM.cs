using Core.Math;
using Std.Math.Foundation;

namespace Std.Math.Numeric.Scalar;

/// <summary>
///     鏈夌悊鏁扮粨鏋勪綋锛岃〃绀?numerator/denominator 褰㈠紡鐨勭簿纭垎鏁帮紝
///     鏋勯€犲拰杩愮畻鍚庤嚜鍔ㄧ害鍒嗭紝鍒嗘瘝濮嬬粓淇濇寔姝ｆ�?///
/// </summary>
/// <typeparam name="T">鍒嗗瓙鍜屽垎姣嶇殑鏁板€肩被鍨嬶紝蹇呴』涓哄€肩被鍨嬩笖鍙瘮�?/typeparam>
public readonly struct Rational<T> :
    IAdditive<Rational<T>>,
    IMultiplicative<Rational<T>>,
    INegation<Rational<T>, Rational<T>>,
    IApproximate<Rational<T>>,
    IEquatable<Rational<T>>,
    IComparable<Rational<T>>,
    IClone<Rational<T>>
    where T : struct, IEquatable<T>, IConvertible, IComparable<T>
{
    /// <summary>鍒嗗�?/summary>
    public readonly T numerator;

    /// <summary>鍒嗘瘝锛屽缁堜负姝ｆ暟</summary>
    public readonly T denominator;

    /// <summary>
    ///     鏋勯€犳湁鐞嗘暟锛岃嚜鍔ㄧ害鍒嗗苟纭繚鍒嗘瘝涓烘
    /// </summary>
    /// <param name="numerator">
    ///     鍒嗗�?/param>
    ///     <param name="denominator">
    ///         鍒嗘�?/param>
    ///         <exception cref="DivideByZeroException">鍒嗘瘝涓洪浂鏃舵姏鍑?/exception>
    public Rational(T numerator, T denominator)
    {
        if (Convert.ToInt64(denominator) == 0) throw new DivideByZeroException("有理数的分母不能为零。");

        var result = reduce(numerator, denominator);
        this.numerator = result.numerator;
        this.denominator = result.denominator;
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

    /// <summary>
    ///     灏嗘硾鍨嬪€艰浆鎹�?64 浣嶆湁绗﹀彿鏁存暟
    /// </summary>
    private static long to_int64(T value)
    {
        return Convert.ToInt64(value);
    }

    #endregion

    #region 宸ュ巶鏂规硶

    /// <summary>
    ///     浠庡垎瀛愬拰鍒嗘瘝鍒涘缓鏈夌悊�?    ///
    /// </summary>
    /// <param name="numerator">
    ///     鍒嗗�?/param>
    ///     <param name="denominator">
    ///         鍒嗘�?/param>
    ///         <returns>绾﹀垎鍚庣殑鏈夌悊鏁?/returns>
    public static Rational<T> from(T numerator, T denominator)
    {
        return new Rational<T>(numerator, denominator);
    }

    #endregion

    #region 灞炴�?

    /// <summary>
    ///     鍒ゆ柇褰撳墠鏈夌悊鏁版槸鍚︿负闆?    ///
    /// </summary>
    public bool is_zero => Convert.ToInt64(numerator) == 0;

    /// <summary>
    ///     鍒ゆ柇褰撳墠鏈夌悊鏁版槸鍚︿负涓�?    ///
    /// </summary>
    public bool is_one => Convert.ToInt64(numerator) == 1 && Convert.ToInt64(denominator) == 1;

    /// <summary>
    ///     鑾峰彇褰撳墠鏈夌悊鏁扮殑绗﹀彿锛?1 琛ㄧず璐熸暟�? 琛ㄧず闆讹紝1 琛ㄧず姝ｆ暟
    /// </summary>
    public int sign
    {
        get
        {
            var n = to_int64(numerator);
            if (n == 0) return 0;

            return n > 0 ? 1 : -1;
        }
    }

    #endregion

    #region IAdditive

    /// <summary>
    ///     鏈夌悊鏁板姞�?a/b + c/d = (ad + bc) / (bd)
    /// </summary>
    /// <param name="right">
    ///     鍔犳�?/param>
    ///     <returns>绾﹀垎鍚庣殑�?/returns>
    public Rational<T> add(Rational<T> right)
    {
        var a = to_int64(numerator);
        var b = to_int64(denominator);
        var c = to_int64(right.numerator);
        var d = to_int64(right.denominator);
        return reduce(from_double(a * d + b * c), from_double(b * d));
    }

    /// <summary>
    ///     鏈夌悊鏁板噺�?a/b - c/d = (ad - bc) / (bd)
    /// </summary>
    /// <param name="right">
    ///     鍑忔�?/param>
    ///     <returns>绾﹀垎鍚庣殑�?/returns>
    public Rational<T> sub(Rational<T> right)
    {
        var a = to_int64(numerator);
        var b = to_int64(denominator);
        var c = to_int64(right.numerator);
        var d = to_int64(right.denominator);
        return reduce(from_double(a * d - b * c), from_double(b * d));
    }

    /// <summary>
    ///     有理数加法运算符
    /// </summary>
    /// <param name="left">左侧操作数</param>
    /// <param name="right">右侧操作数</param>
    /// <returns>相加结果</returns>
    public static Rational<T> operator +(Rational<T> left, Rational<T> right)
    {
        return left.add(right);
    }

    #endregion

    #region IMultiplicative

    /// <summary>
    ///     鏈夌悊鏁颁箻�?a/b * c/d = (ac) / (bd)
    /// </summary>
    /// <param name="right">
    ///     涔樻�?/param>
    ///     <returns>绾﹀垎鍚庣殑�?/returns>
    public Rational<T> mul(Rational<T> right)
    {
        var a = to_int64(numerator);
        var b = to_int64(denominator);
        var c = to_int64(right.numerator);
        var d = to_int64(right.denominator);
        return reduce(from_double(a * c), from_double(b * d));
    }

    /// <summary>
    ///     鏈夌悊鏁伴櫎�?a/b / c/d = (ad) / (bc)
    /// </summary>
    /// <param name="right">
    ///     闄ゆ�?/param>
    ///     <returns>
    ///         绾﹀垎鍚庣殑�?/returns>
    ///         <exception cref="DivideByZeroException">闄ゆ暟鐨勫垎瀛愪负闆舵椂鎶涘�?/exception>
    public Rational<T> div(Rational<T> right)
    {
        if (right.is_zero) throw new DivideByZeroException("涓嶈兘闄や互闆舵湁鐞嗘暟");

        var a = to_int64(numerator);
        var b = to_int64(denominator);
        var c = to_int64(right.numerator);
        var d = to_int64(right.denominator);
        return reduce(from_double(a * d), from_double(b * c));
    }

    /// <summary>
    ///     有理数乘法运算符
    /// </summary>
    /// <param name="left">左侧操作数</param>
    /// <param name="right">右侧操作数</param>
    /// <returns>乘积结果</returns>
    public static Rational<T> operator *(Rational<T> left, Rational<T> right)
    {
        return left.mul(right);
    }

    #endregion

    #region INegation

    /// <summary>
    ///     鏈夌悊鏁板彇�?-(a/b) = (-a)/b
    /// </summary>
    /// <returns>鍙栬礋鍚庣殑鏈夌悊鏁?/returns>
    public Rational<T> neg()
    {
        return new Rational<T>(from_double(-to_double(numerator)), denominator);
    }

    #endregion

    #region 鍊掓�?

    /// <summary>
    ///     鏈夌悊鏁板彇鍊掓�?1/(a/b) = b/a
    /// </summary>
    /// <returns>
    ///     鍊掓�?/returns>
    ///     <exception cref="DivideByZeroException">褰撳墠鏈夌悊鏁颁负闆舵椂鎶涘�?/exception>
    public Rational<T> reciprocal()
    {
        if (is_zero) throw new DivideByZeroException("闆舵病鏈夊€掓暟");

        return new Rational<T>(denominator, numerator);
    }

    #endregion

    #region IEquatable

    /// <summary>
    ///     鍒ゆ柇涓や釜鏈夌悊鏁版槸鍚︾浉绛夛紝绾﹀垎鍚庡垎瀛愬垎姣嶅潎鐩哥瓑鏃惰繑�?true
    /// </summary>
    /// <param name="other">姣旇緝鐩爣</param>
    public bool Equals(Rational<T> other)
    {
        return numerator.Equals(other.numerator) && denominator.Equals(other.denominator);
    }

    #endregion

    #region IComparable

    /// <summary>
    ///     姣旇緝涓や釜鏈夌悊鏁扮殑澶у皬 a/b �?c/d 姣旇緝绛変环浜庢瘮杈?ad �?bc
    /// </summary>
    /// <param name="other">姣旇緝鐩爣</param>
    /// <returns>灏忎�?0 琛ㄧず褰撳墠瀵硅薄杈冨皬�? 琛ㄧず鐩哥瓑锛屽ぇ浜?0 琛ㄧず杈冨ぇ</returns>
    public int CompareTo(Rational<T> other)
    {
        var ad = to_int64(numerator) * to_int64(other.denominator);
        var bc = to_int64(denominator) * to_int64(other.numerator);
        return ad.CompareTo(bc);
    }

    #endregion

    #region IApproximate

    /// <summary>
    ///     鍒ゆ柇涓や釜鏈夌悊鏁版槸鍚﹀湪缁欏畾璇樊鑼冨洿鍐呰繎浼肩浉绛夛�?    /// 灏嗘湁鐞嗘暟杞崲涓哄弻绮惧害娴偣鏁板悗姣旇緝
    /// </summary>
    /// <param name="other">姣旇緝鐩爣</param>
    /// <param name="absolute_tolerance">
    ///     缁濆璇樊瀹归�?/param>
    ///     <param name="relative_tolerance">鐩稿璇樊瀹归�?/param>
    public bool is_close(Rational<T> other, double absoluteTolerance, double relativeTolerance)
    {
        var a = to_float64();
        var b = other.to_float64();
        var diff = SonicMath.abs(a - b);
        if (diff <= absoluteTolerance) return true;

        var maxAbs = SonicMath.max(SonicMath.abs(a), SonicMath.abs(b));
        return diff <= relativeTolerance * maxAbs;
    }

    /// <summary>
    ///     判断两个有理数是否在给定容差范围内近似相等
    /// </summary>
    /// <param name="other">比较目标</param>
    /// <param name="epsilon">容差</param>
    /// <returns>是否近似相等</returns>
    public bool approx_equals(Rational<T> other, Rational<T> epsilon)
    {
        var diff = SonicMath.abs(to_float64() - other.to_float64());
        return diff <= epsilon.to_float64();
    }

    #endregion

    #region 绫诲瀷杞崲

    /// <summary>
    ///     灏嗘湁鐞嗘暟杞崲涓哄弻绮惧害娴偣鏁帮紝鍗?numerator / denominator
    /// </summary>
    /// <returns>鍙岀簿搴︽诞鐐规暟鍊?/returns>
    public double to_float64()
    {
        return to_double(numerator) / to_double(denominator);
    }

    #endregion

    #region IClone

    /// <summary>
    ///     杩斿洖褰撳墠鏈夌悊鏁扮殑娣辨嫹璐濓紝鐢变簬鏄?readonly struct锛岀洿鎺ヨ繑鍥炶嚜韬嵆�?    ///
    /// </summary>
    public Rational<T> clone()
    {
        return new Rational<T>(numerator, denominator);
    }

    #endregion

    #region ToString

    /// <summary>
    ///     杩斿洖鏈夌悊鏁扮殑瀛楃涓茶〃绀猴紝鍒嗘瘝涓?1 鏃朵粎杈撳嚭鍒嗗瓙锛屽惁鍒欒緭鍑?"鍒嗗�?鍒嗘�?
    /// </summary>
    public override string ToString()
    {
        if (Convert.ToInt64(denominator) == 1) return numerator.ToString();

        return $"{numerator}/{denominator}";
    }

    #endregion

    #region 绉佹湁杈呭姪鏂规�?

    /// <summary>
    ///     璁＄畻涓や釜鏁存暟鐨勬渶澶у叕绾︽暟锛堟鍑犻噷寰楃畻娉曪級锛屼娇�?64 浣嶆暣鏁拌繍�?    ///
    /// </summary>
    /// <param name="a">绗竴涓暟</param>
    /// <param name="b">绗簩涓暟</param>
    /// <returns>鏈€澶у叕绾︽�?/returns>
    private static T gcd(T a, T b)
    {
        var x = SonicMath.abs(to_int64(a));
        var y = SonicMath.abs(to_int64(b));
        while (y != 0)
        {
            var temp = y;
            y = x % y;
            x = temp;
        }

        return from_double(x);
    }

    /// <summary>
    ///     绾﹀垎骞剁‘淇濆垎姣嶄负姝ｆ暟锛岃繑鍥炵害鍒嗗悗鐨勬湁鐞嗘�?    ///
    /// </summary>
    /// <param name="num">
    ///     鍒嗗�?/param>
    ///     <param name="den">
    ///         鍒嗘�?/param>
    ///         <returns>
    ///             绾﹀垎鍚庣殑鏈夌悊鏁?/returns>
    ///             <exception cref="DivideByZeroException">鍒嗘瘝涓洪浂鏃舵姏鍑?/exception>
    private static Rational<T> reduce(T num, T den)
    {
        var numLong = to_int64(num);
        var denLong = to_int64(den);

        if (denLong == 0) throw new DivideByZeroException("有理数的分母不能为零。");

        if (numLong == 0) return new Rational<T>(from_double(0), from_double(1));

        if (denLong < 0)
        {
            numLong = -numLong;
            denLong = -denLong;
        }

        var g = to_int64(gcd(from_double(numLong), from_double(denLong)));
        if (g > 1)
        {
            numLong /= g;
            denLong /= g;
        }

        return new Rational<T>(from_double(numLong), from_double(denLong));
    }

    #endregion
}