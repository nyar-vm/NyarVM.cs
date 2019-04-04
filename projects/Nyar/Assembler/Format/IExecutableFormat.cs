namespace Nyar.Assembler.Format;

/// <summary>
///     可执行格式接口，定义可执行文件的构建方式
/// </summary>
public interface IExecutableFormat
{
    /// <summary>
    ///     格式名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     构建可执行文件数据结构并编码为字的
    /// </summary>
    /// <param name="context">
    ///     Native 构建上下的/param>
    ///     <returns>编码后的可执行文件字的/returns>
    byte[] build_and_encode(NativeBuildContext context);
}