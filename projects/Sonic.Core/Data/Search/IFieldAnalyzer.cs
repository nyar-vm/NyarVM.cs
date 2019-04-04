namespace Core.Data.Search;

/// <summary>
///     字段分词器接口
/// </summary>
public interface IFieldAnalyzer
{
    /// <summary>
    ///     对输入文本进行分词
    /// </summary>
    /// <param name="input">输入文本</param>
    /// <returns>分词结果数组</returns>
    string[] tokenize(string input);
}