namespace Nyar.Language.Batch;

/// <summary>
///     Batch 语言定义
/// </summary>
public sealed class BatchLanguage : Language
{
    public override string name => "batch";

    public IReadOnlyList<string> extensions { get; init; } = [".bat", ".cmd"];
}