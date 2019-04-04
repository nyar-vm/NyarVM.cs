namespace Nyar.Language.Sql;

/// <summary>
///     SQL 语言定义
/// </summary>
public sealed class SqlLanguage : Language
{
    public override string name => "sql";

    public IReadOnlyList<string> extensions { get; init; } = [".sql"];
}