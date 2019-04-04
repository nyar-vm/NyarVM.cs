using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Standard;

/// <summary>
///     Standard 方言的 OA 接口定义。
///     该接口声明字符串、内存对象、并发与时序等通用运行时操作。
///     宿主互操作不在这里通过伪装成普通节点的 I/O 方法表达，而是通过 Import/Apply 形式承载。
/// </summary>
[Dialect("standard", may_have_children = true)]
public interface IStandard<T>
{
    #region 类型转换与位操作

    /// <summary>
    ///     类型转换
    /// </summary>
    /// <param name="value">输入值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <returns>转换结果。</returns>
    [Operator("cast")]
    Term<T> cast(Term<T> value, string targetType);

    /// <summary>
    ///     截断转换
    /// </summary>
    /// <param name="value">输入值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <returns>转换结果。</returns>
    [Operator("trunc")]
    Term<T> trunc(Term<T> value, string targetType);

    /// <summary>
    ///     无符号扩展
    /// </summary>
    /// <param name="value">输入值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <returns>转换结果。</returns>
    [Operator("zext")]
    Term<T> z_ext(Term<T> value, string targetType);

    /// <summary>
    ///     有符号扩展
    /// </summary>
    /// <param name="value">输入值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <returns>转换结果。</returns>
    [Operator("sext")]
    Term<T> s_ext(Term<T> value, string targetType);

    /// <summary>
    ///     按位与
    /// </summary>
    [Operator("bitand")]
    Term<T> bit_and(Term<T> left, Term<T> right);

    /// <summary>
    ///     按位或
    /// </summary>
    [Operator("bitor")]
    Term<T> bit_or(Term<T> left, Term<T> right);

    /// <summary>
    ///     按位异或
    /// </summary>
    [Operator("bitxor")]
    Term<T> bit_xor(Term<T> left, Term<T> right);

    /// <summary>
    ///     左移
    /// </summary>
    [Operator("shl")]
    Term<T> shl(Term<T> value, Term<T> shift);

    /// <summary>
    ///     逻辑右移
    /// </summary>
    [Operator("lshr")]
    Term<T> l_shr(Term<T> value, Term<T> shift);

    /// <summary>
    ///     算术右移
    /// </summary>
    [Operator("ashr")]
    Term<T> a_shr(Term<T> value, Term<T> shift);

    #endregion

    #region UTF-8 文本操作

    /// <summary>
    ///     UTF-8 文本拼接
    /// </summary>
    [Operator("utf8_concat")]
    Term<T> utf8_concat(Term<T> left, Term<T> right);

    /// <summary>
    ///     UTF-8 文本格式化
    /// </summary>
    [Operator("utf8_format")]
    Term<T> utf8_format(string template, Term<T>[] args);

    /// <summary>
    ///     UTF-8 文本子串
    /// </summary>
    [Operator("utf8_substr")]
    Term<T> utf8_substr(Term<T> value, Term<T> start, Term<T> length);

    /// <summary>
    ///     UTF-8 文本长度
    /// </summary>
    [Operator("utf8_len")]
    Term<T> utf8_len(Term<T> value);

    /// <summary>
    ///     UTF-8 文本比较
    /// </summary>
    [Operator("utf8_compare")]
    Term<T> utf8_compare(Term<T> left, Term<T> right);

    #endregion

    #region 数组与切片操作

    /// <summary>
    ///     创建数组
    /// </summary>
    [Operator("arrnew")]
    Term<T> array_new(Term<T> length, Term<T> initialValue);

    /// <summary>
    ///     获取数组长度
    /// </summary>
    [Operator("arrlen")]
    Term<T> array_length(Term<T> array);

    /// <summary>
    ///     读取数组元素
    /// </summary>
    [Operator("arrget")]
    Term<T> array_get(Term<T> array, Term<T> index);

    /// <summary>
    ///     写入数组元素
    /// </summary>
    [Operator("arrset")]
    Term<T> array_set(Term<T> array, Term<T> index, Term<T> value);

    /// <summary>
    ///     读取序数索引
    /// </summary>
    [Operator("getordinalidx")]
    Term<T> get_ordinal_idx(Term<T> obj, Term<T> index);

    /// <summary>
    ///     写入序数索引
    /// </summary>
    [Operator("setordinalidx")]
    Term<T> set_ordinal_idx(Term<T> obj, Term<T> index, Term<T> value);

    /// <summary>
    ///     读取偏移索引
    /// </summary>
    [Operator("getoffsetidx")]
    Term<T> get_offset_idx(Term<T> obj, Term<T> index);

    /// <summary>
    ///     写入偏移索引
    /// </summary>
    [Operator("setoffsetidx")]
    Term<T> set_offset_idx(Term<T> obj, Term<T> index, Term<T> value);

    /// <summary>
    ///     创建切片
    /// </summary>
    [Operator("slicenew")]
    Term<T> slice_new(Term<T> pointer, Term<T> length);

    #endregion

    #region 结构体操作

    /// <summary>
    ///     声明结构体
    /// </summary>
    [Operator("stdecl")]
    Term<T> struct_declare(string name, string[] fields);

    /// <summary>
    ///     创建结构体实例
    /// </summary>
    [Operator("stnew")]
    Term<T> struct_new(string name, Term<T>[] fields);

    /// <summary>
    ///     读取结构体字段
    /// </summary>
    [Operator("stget")]
    Term<T> struct_get(Term<T> target, string fieldName);

    /// <summary>
    ///     写入结构体字段
    /// </summary>
    [Operator("stset")]
    Term<T> struct_set(Term<T> target, string fieldName, Term<T> value);

    #endregion

    #region 原子操作

    /// <summary>
    ///     原子读取
    /// </summary>
    [Operator("atomicload")]
    Term<T> atomic_load(Term<T> pointer, string order);

    /// <summary>
    ///     原子写入
    /// </summary>
    [Operator("atomicstore")]
    Term<T> atomic_store(Term<T> pointer, Term<T> value, string order);

    /// <summary>
    ///     原子比较交换
    /// </summary>
    [Operator("atomiccas")]
    Term<T> atomic_cas(Term<T> pointer, Term<T> expected, Term<T> desired, string order);

    #endregion

    #region 并发操作

    /// <summary>
    ///     创建并发任务
    /// </summary>
    /// <param name="body">任务体。</param>
    /// <returns>任务标识。</returns>
    [Operator("fork")]
    Term<T> fork(Term<T> body);

    /// <summary>
    ///     让出执行权
    /// </summary>
    /// <returns>让出结果。</returns>
    [Operator("yield")]
    Term<T> yield();

    /// <summary>
    ///     等待任务完成
    /// </summary>
    /// <param name="task">要等待的任务。</param>
    /// <returns>任务结果。</returns>
    [Operator("await")]
    Term<T> await(Term<T> task);

    #endregion

    #region 时序操作

    /// <summary>
    ///     休眠指定毫秒数
    /// </summary>
    /// <param name="ms">休眠毫秒数。</param>
    /// <returns>休眠结果。</returns>
    [Operator("sleep")]
    Term<T> sleep(Term<T> ms);

    /// <summary>
    ///     获取当前时间戳
    /// </summary>
    /// <returns>当前时间戳（毫秒）。</returns>
    [Operator("timenow")]
    Term<T> time_now();

    #endregion
}