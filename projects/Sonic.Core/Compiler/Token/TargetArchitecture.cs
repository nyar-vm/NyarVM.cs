namespace Core.Compiler.Token;

/// <summary>
///     目标架构枚举，定义编译器支持的输出架构
/// </summary>
public enum TargetArchitecture
{
    /// <summary>
    ///     x86-64 架构
    /// </summary>
    x86_64,

    /// <summary>
    ///     ARM64 架构
    /// </summary>
    arm64,

    /// <summary>
    ///     WebAssembly 架构
    /// </summary>
    wasm,

    /// <summary>
    ///     MLIR 中间表示
    /// </summary>
    mlir,

    /// <summary>
    ///     LLVM IR 中间表示
    /// </summary>
    llvm_ir
}