namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar SIMD 二级指令编码。
///     它是 `simd` 前缀指令的一部分，不是独立的一级 opcode。
///     禁止继续占用一级 opcode 槽位。
/// </summary>
public enum NyarSimdCode
{
    #region SIMD 二级指令编码

    /// <summary>
    ///     v128 常量加载
    /// </summary>
    v128_const = 0x00,

    /// <summary>
    ///     v128 内存加载
    /// </summary>
    v128_load = 0x01,

    /// <summary>
    ///     v128 内存存储
    /// </summary>
    v128_store = 0x02,

    /// <summary>
    ///     i32x4 向量加法
    /// </summary>
    i32_x4_add = 0x03,

    /// <summary>
    ///     i32x4 向量减法
    /// </summary>
    i32_x4_sub = 0x04,

    /// <summary>
    ///     i32x4 向量乘法
    /// </summary>
    i32_x4_mul = 0x05,

    /// <summary>
    ///     f32x4 向量加法
    /// </summary>
    f32_x4_add = 0x06,

    /// <summary>
    ///     f32x4 向量减法
    /// </summary>
    f32_x4_sub = 0x07,

    /// <summary>
    ///     f32x4 向量乘法
    /// </summary>
    f32_x4_mul = 0x08,

    /// <summary>
    ///     i8x16 通道广播
    /// </summary>
    i8_x16_splat = 0x09,

    /// <summary>
    ///     i16x8 通道广播
    /// </summary>
    i16_x8_splat = 0x0A,

    /// <summary>
    ///     i32x4 通道广播
    /// </summary>
    i32_x4_splat = 0x0B,

    /// <summary>
    ///     f32x4 通道广播
    /// </summary>
    f32_x4_splat = 0x0C,

    /// <summary>
    ///     f64x2 通道广播
    /// </summary>
    f64_x2_splat = 0x0D,

    /// <summary>
    ///     i8x16 提取有符号通道
    /// </summary>
    i8_x16_extract_lane_s = 0x0E,

    /// <summary>
    ///     i8x16 替换通道
    /// </summary>
    i8_x16_replace_lane = 0x0F,

    /// <summary>
    ///     i32x4 提取通道
    /// </summary>
    i32_x4_extract_lane = 0x10,

    /// <summary>
    ///     i32x4 替换通道
    /// </summary>
    i32_x4_replace_lane = 0x11,

    /// <summary>
    ///     v128 按位与
    /// </summary>
    v128_and = 0x12,

    /// <summary>
    ///     v128 按位或
    /// </summary>
    v128_or = 0x13,

    /// <summary>
    ///     v128 按位异或
    /// </summary>
    v128_xor = 0x14,

    /// <summary>
    ///     v128 按位取反
    /// </summary>
    v128_not = 0x15,

    /// <summary>
    ///     v128 位选择
    /// </summary>
    v128_bit_select = 0x16,

    /// <summary>
    ///     i64x2 向量加法
    /// </summary>
    i64_x2_add = 0x17,

    /// <summary>
    ///     i64x2 向量减法
    /// </summary>
    i64_x2_sub = 0x18,

    /// <summary>
    ///     f64x2 向量加法
    /// </summary>
    f64_x2_add = 0x19,

    /// <summary>
    ///     f64x2 向量减法
    /// </summary>
    f64_x2_sub = 0x1A,

    /// <summary>
    ///     f64x2 向量乘法
    /// </summary>
    f64_x2_mul = 0x1B,

    /// <summary>
    ///     i8x16 通道重排
    /// </summary>
    i8_x16_shuffle = 0x1C,

    /// <summary>
    ///     i32x4 相等比较
    /// </summary>
    i32_x4_eq = 0x1D,

    /// <summary>
    ///     f32x4 相等比较
    /// </summary>
    f32_x4_eq = 0x1E,

    /// <summary>
    ///     v128 任意为真
    /// </summary>
    v128_any_true = 0x1F

    #endregion
}