namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     指令编码形态。
///     它描述的是编码层的取值方式，不等同于运行时语义分类。
/// </summary>
public enum NyarInstructionForm : byte
{
    /// <summary>
    ///     无效形态。
    ///     通常对应默认值或无法识别的头码。
    /// </summary>
    invalid = 0,

    /// <summary>
    ///     仅包含一级头码，不带立即数字段。
    /// </summary>
    plain = 1,

    /// <summary>
    ///     头码后跟一个 `i32` 立即数字段。
    /// </summary>
    imm1 = 2,

    /// <summary>
    ///     头码后跟两个 `i32` 立即数字段。
    /// </summary>
    imm2 = 3,

    /// <summary>
    ///     头码后跟三个 `i32` 立即数字段。
    /// </summary>
    imm3 = 4,

    /// <summary>
    ///     前缀形态。
    ///     一级头码后还需继续解码二级子码与扩展负载。
    /// </summary>
    prefixed = 5
}