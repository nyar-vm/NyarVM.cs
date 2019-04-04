using Core.Math;
using Core.Math.Linear;
using Std.Math.Foundation;

namespace Std.Math.Numeric.Linear;

/// <summary>
///     二维向量，表示具有 x 和 y 分量的二维空间向量
/// </summary>
/// <typeparam name="T">分量数值类型，必须为值类型且支持相等比较和类型转换</typeparam>
public readonly struct Vec2<T> :
    IAdditive<Vec2<T>>,
    INegation<Vec2<T>, Vec2<T>>,
    IApproximate<Vec2<T>>,
    IEquatable<Vec2<T>>,
    IClone<Vec2<T>>,
    IZero<Vec2<T>>,
    IOne<Vec2<T>>,
    IVector<T, int>
    where T : struct, IEquatable<T>, IConvertible
{
    /// <summary>x 鍒嗛�?/summary>
    public readonly T x;

    /// <summary>y 鍒嗛�?/summary>
    public readonly T y;

    /// <summary>绾㈣壊閫氶亾鍒悕锛岀瓑浠蜂簬 x 鍒嗛�?/summary>
    public T r => x;

    /// <summary>缁胯壊閫氶亾鍒悕锛岀瓑浠蜂簬 y 鍒嗛�?/summary>
    public T g => y;

    #region IVector

    /// <summary>
    ///     获取或设置指定索引处的分量
    /// </summary>
    /// <param name="index">分量索引，0 为 x，1 为 y</param>
    /// <returns>指定索引处的分量值</returns>
    public T this[int index]
    {
        get => index switch
        {
            0 => x,
            1 => y,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set => throw new NotSupportedException("Vec2 是不可变结构体，不支持索引器设置");
    }

    /// <summary>
    ///     获取向量的维度
    /// </summary>
    public int dimension => 2;

    #endregion

    /// <summary>
    ///     鏋勯€犱簩缁村悜�?    ///
    /// </summary>
    /// <param name="x">
    ///     x 鍒嗛�?/param>
    ///     <param name="y">y 鍒嗛�?/param>
    public Vec2(T x, T y)
    {
        this.x = x;
        this.y = y;
    }

    #region 绫诲瀷杞崲杈呭�?

    /// <summary>
    ///     灏嗘硾鍨嬪€艰浆鎹负鍙岀簿搴︽诞鐐规�?    ///
    /// </summary>
    private static double to_d(T v)
    {
        return v.ToDouble(null);
    }

    /// <summary>
    ///     灏嗗弻绮惧害娴偣鏁拌浆鎹负娉涘瀷鍊?    ///
    /// </summary>
    private static T from_d(double v)
    {
        return (T)Convert.ChangeType(v, typeof(T));
    }

    #endregion

    #region 鍚戦噺杩愮畻

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓簩缁村悜閲忕殑鐐圭�?    ///
    /// </summary>
    /// <param name="other">
    ///     鍙︿竴涓簩缁村悜閲?/param>
    ///     <returns>鐐圭Н�?/returns>
    public double dot(Vec2<T> other)
    {
        return to_d(x) * to_d(other.x) + to_d(y) * to_d(other.y);
    }

    /// <summary>
    ///     璁＄畻鍚戦噺鐨勯暱搴︼紙妯★級锛屽嵆鍒板師鐐圭殑璺濈�?    ///
    /// </summary>
    /// <returns>鍚戦噺闀垮害</returns>
    public double length()
    {
        return SonicMath.sqrt(length_squared());
    }

    /// <summary>
    ///     璁＄畻鍚戦噺闀垮害鐨勫钩鏂癸紝閬垮厤寮€鏂硅繍�?    ///
    /// </summary>
    /// <returns>闀垮害骞虫柟鍊?/returns>
    public double length_squared()
    {
        var dx = to_d(x);
        var dy = to_d(y);
        return dx * dx + dy * dy;
    }

    /// <summary>
    ///     杩斿洖褰撳墠鍚戦噺鐨勫崟浣嶅悜閲忥紙褰掍竴鍖栵級
    /// </summary>
    /// <returns>鍗曚綅鍚戦噺</returns>
    public Vec2<T> normalize()
    {
        var len = length();
        if (len == 0.0) return new Vec2<T>(from_d(0), from_d(0));

        var inv = 1.0 / len;
        return new Vec2<T>(from_d(to_d(x) * inv), from_d(to_d(y) * inv));
    }

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓簩缁村悜閲忕殑绾挎€ф彃鍊?    ///
    /// </summary>
    /// <param name="other">鐩爣鍚戦噺</param>
    /// <param name="t">鎻掑€煎洜瀛愶�? 杩斿洖鑷韩�? 杩斿洖鐩爣</param>
    /// <returns>鎻掑€肩粨鏋?/returns>
    public Vec2<T> lerp(Vec2<T> other, double t)
    {
        return new Vec2<T>(
            from_d(to_d(x) + (to_d(other.x) - to_d(x)) * t),
            from_d(to_d(y) + (to_d(other.y) - to_d(y)) * t)
        );
    }

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓簩缁村悜閲忎箣闂寸殑娆у嚑閲屽緱璺濈
    /// </summary>
    /// <param name="other">鐩爣鍚戦噺</param>
    /// <returns>涓ょ偣闂磋窛�?/returns>
    public double distance_to(Vec2<T> other)
    {
        var dx = to_d(x) - to_d(other.x);
        var dy = to_d(y) - to_d(other.y);
        return SonicMath.sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    ///     璁＄畻涓庡彟涓€涓簩缁村悜閲忎箣闂寸殑澶硅锛堝姬搴︼�?    ///
    /// </summary>
    /// <param name="other">
    ///     鍙︿竴涓簩缁村悜閲?/param>
    ///     <returns>澶硅寮у害鍊?/returns>
    public double angle_between(Vec2<T> other)
    {
        var denom = length() * other.length();
        if (denom == 0.0) return 0.0;

        var cosVal = dot(other) / denom;
        return SonicMath.atan2(
            to_d(x) * to_d(other.y) - to_d(y) * to_d(other.x),
            dot(other)
        );
    }

    #endregion

    #region 鏍囬噺杩愮畻

    /// <summary>
    ///     灏嗗悜閲忕殑姣忎釜鍒嗛噺涔樹互鏍囬噺
    /// </summary>
    /// <param name="v">杈撳叆鍚戦噺</param>
    /// <param name="scalar">鏍囬噺涔樻暟</param>
    /// <returns>缂╂斁鍚庣殑鍚戦�?/returns>
    public static Vec2<T> scale(Vec2<T> v, T scalar)
    {
        var s = to_d(scalar);
        return new Vec2<T>(from_d(to_d(v.x) * s), from_d(to_d(v.y) * s));
    }

    /// <summary>
    ///     灏嗗悜閲忕殑姣忎釜鍒嗛噺闄や互鏍囬噺
    /// </summary>
    /// <param name="v">杈撳叆鍚戦噺</param>
    /// <param name="scalar">鏍囬噺闄ゆ暟</param>
    /// <returns>缂╂斁鍚庣殑鍚戦�?/returns>
    public static Vec2<T> scale_inverse(Vec2<T> v, T scalar)
    {
        var s = to_d(scalar);
        return new Vec2<T>(from_d(to_d(v.x) / s), from_d(to_d(v.y) / s));
    }

    #endregion

    #region IAdditive

    /// <summary>
    ///     向量加法，逐分量相加
    /// </summary>
    /// <param name="right">右侧操作数</param>
    /// <returns>相加结果</returns>
    public Vec2<T> add(Vec2<T> right)
    {
        return new Vec2<T>(
            from_d(to_d(x) + to_d(right.x)),
            from_d(to_d(y) + to_d(right.y))
        );
    }

    /// <summary>
    ///     向量减法，逐分量相减
    /// </summary>
    /// <param name="right">右侧操作数</param>
    /// <returns>相减结果</returns>
    public Vec2<T> sub(Vec2<T> right)
    {
        return new Vec2<T>(
            from_d(to_d(x) - to_d(right.x)),
            from_d(to_d(y) - to_d(right.y))
        );
    }

    /// <summary>
    ///     向量加法运算符，逐分量相加
    /// </summary>
    /// <param name="left">左侧操作数</param>
    /// <param name="right">右侧操作数</param>
    /// <returns>相加结果</returns>
    public static Vec2<T> operator +(Vec2<T> left, Vec2<T> right)
    {
        return left.add(right);
    }

    #endregion

    #region INegation

    /// <summary>
    ///     鍚戦噺鍙栬礋锛屾瘡涓垎閲忓彇鍙?    ///
    /// </summary>
    /// <returns>鍙栬礋缁撴灉</returns>
    public Vec2<T> neg()
    {
        return new Vec2<T>(from_d(-to_d(x)), from_d(-to_d(y)));
    }

    #endregion

    #region IApproximate

    /// <summary>
    ///     判断两个二维向量是否在给定容差范围内近似相等，逐分量比较
    /// </summary>
    /// <param name="other">比较目标</param>
    /// <param name="absoluteTolerance">绝对误差容限</param>
    /// <param name="relativeTolerance">相对误差容限</param>
    /// <returns>是否近似相等</returns>
    public bool is_close(Vec2<T> other, double absoluteTolerance, double relativeTolerance)
    {
        return is_component_close(to_d(x), to_d(other.x), absoluteTolerance, relativeTolerance)
               && is_component_close(to_d(y), to_d(other.y), absoluteTolerance, relativeTolerance);
    }

    /// <summary>
    ///     判断两个二维向量是否在给定逐分量容差范围内近似相等
    /// </summary>
    /// <param name="other">比较目标</param>
    /// <param name="epsilon">逐分量容差</param>
    /// <returns>是否近似相等</returns>
    public bool approx_equals(Vec2<T> other, Vec2<T> epsilon)
    {
        return SonicMath.abs(to_d(x) - to_d(other.x)) <= to_d(epsilon.x)
               && SonicMath.abs(to_d(y) - to_d(other.y)) <= to_d(epsilon.y);
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
    ///     鍒ゆ柇涓や釜浜岀淮鍚戦噺鏄惁鐩哥瓑锛屾瘡涓垎閲忓潎鐩哥瓑鏃惰繑鍥?true
    /// </summary>
    /// <param name="other">姣旇緝鐩爣</param>
    /// <returns>鏄惁鐩哥瓑</returns>
    public bool Equals(Vec2<T> other)
    {
        return x.Equals(other.x) && y.Equals(other.y);
    }

    #endregion

    #region IClone

    /// <summary>
    ///     杩斿洖褰撳墠浜岀淮鍚戦噺鐨勬繁鎷疯礉锛岀敱浜庢槸 readonly struct锛岀洿鎺ヨ繑鍥炶嚜韬嵆�?    ///
    /// </summary>
    /// <returns>鍏嬮殕缁撴灉</returns>
    public Vec2<T> clone()
    {
        return new Vec2<T>(x, y);
    }

    #endregion

    #region IZero

    /// <summary>
    ///     获取零向量 (0, 0)
    /// </summary>
    public static Vec2<T> zero => new(from_d(0), from_d(0));

    #endregion

    #region IOne

    /// <summary>
    ///     获取单位向量 (1, 1)
    /// </summary>
    public static Vec2<T> one => new(from_d(1), from_d(1));

    #endregion
}