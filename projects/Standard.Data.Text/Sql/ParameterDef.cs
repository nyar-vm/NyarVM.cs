namespace Std.Data.Text.Sql;

/// <summary>
///     函数/存储过程参数定义
///     。
/// </summary>
public sealed class ParameterDef
{
    public ParameterDef(string name, string type)
    {
        this.name = name;
        this.type = type;
    }


    /// <summary>
    ///     参数名
    ///     。
    /// </summary>
    public string name { get; }


    /// <summary>
    ///     参数类型
    ///     。
    /// </summary>
    public string type { get; }
}