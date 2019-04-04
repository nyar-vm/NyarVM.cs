namespace Nyar.Language.Python;

/// <summary>
///     Python 语言定义
/// </summary>
public sealed class PythonLanguage : Language
{
    public override string name => "python";

    public IReadOnlyList<string> extensions { get; init; } = [".py", ".pyw"];
}