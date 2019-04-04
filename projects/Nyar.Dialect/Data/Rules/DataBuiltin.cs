namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     Data 方言内置函数 ID
/// </summary>
public enum DataBuiltin : long
{
    Scan = 0xA001,
    IndexScan = 0xA002,
    Filter = 0xA003,
    Project = 0xA004,
    Join = 0xA005,
    Aggregate = 0xA006,
    OrderBy = 0xA007,
    Limit = 0xA008,
    TableStats = 0xA009,
    GroupBy = 0xA00A,
    Having = 0xA00B,
    WindowFunction = 0xA00C,
    Distinct = 0xA00D,
    Subquery = 0xA00E,
    Union = 0xA00F
}