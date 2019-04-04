using Std.Data.Binary.Frame;

namespace Nyar.Assembler.CRT;

/// <summary>
///     入口点存根接口，定义可执行文件的入口点代码生的
/// </summary>
public interface IEntryPointStub
{
    /// <summary>
    ///     存根名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     发射入口点存根代的
    /// </summary>
    /// <param name="writer">
    ///     字节缓冲写入的/param>
    ///     <param name="textVa">代码段虚拟地址。</param>
    ///     <returns>入口的RVA。</returns>
    uint emit_stub(ref ByteBufferWriter writer, uint textVa);
}