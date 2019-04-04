namespace Std.Data.Binary.Wasm.Scanner;

/// <summary>
///     Wasm 格式扫描器接口，提供的WebAssembly 二进制数据的快速探查能力的
/// </summary>
/// <remarks>
///     WebAssembly 是一种可移植、体积小、加载快的二进制指令格式的
///     扫描器专注于快速识的Wasm 文件版本、段信息、导入导出数量等元信息，
///     不做完整的对象反序列化，以实现零分配高性能扫描的
/// </remarks>
public interface IWasmScanner
{
    /// <summary>
    ///     读取 Wasm 文件头中的版本号的
    /// </summary>
    /// <returns>版本号的/returns>
    uint read_version();

    /// <summary>
    ///     读取 Wasm 变长名称（LEB128 长度前缀 + UTF-8 字节）的
    /// </summary>
    /// <returns>名称字符串的/returns>
    string read_name();

    /// <summary>
    ///     读取 LEB128 编码的无符号 32 位整数的
    /// </summary>
    /// <returns>无符的32 位整数值的/returns>
    uint read_leb128_u_int32();
}