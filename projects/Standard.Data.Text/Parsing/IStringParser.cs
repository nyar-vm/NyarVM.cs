namespace Std.Data.Text.Parsing;

/// <summary>
///     字符串解析器接口
/// </summary>
/// <typeparam name="TOutput">输出类型</typeparam>
public interface IStringParser<out TOutput> : IParser<string, TOutput>
{
}