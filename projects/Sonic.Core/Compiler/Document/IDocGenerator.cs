namespace Core.Compiler.Document;

/// <summary>
///     文档生成器接口，定义文档生成的契约
/// </summary>
public interface IDocGenerator
{
    /// <summary>
    ///     生成文档内容
    /// </summary>
    /// <returns>生成的文档字符串</returns>
    string generate();
}