using Core.Data.Search;

namespace Std.Data.Search;

/// <summary>
///     默认的空格分词器实现，按空格和标点符号拆分文本。
/// </summary>
public sealed class DefaultFieldAnalyzer : IFieldAnalyzer
{
    private static readonly char[] _separators =
    [
        ' ', '\t', '\n', '\r', ',', '.', ';', ':', '!', '?',
        '(', ')', '[', ']', '{', '}', '<', '>', '"', '\'',
        '/', '\\', '|', '@', '#', '$', '%', '^', '&', '*', '+', '=', '~', '`'
    ];

    /// <summary>
    ///     对输入文本进行分词。
    /// </summary>
    /// <param name="input">输入文本。</param>
    /// <returns>分词结果数组。</returns>
    public string[] tokenize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];

        return
        [
            .. input.Split(_separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToLowerInvariant())
        ];
    }
}