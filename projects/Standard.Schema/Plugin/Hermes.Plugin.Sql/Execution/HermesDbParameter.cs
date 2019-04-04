using System.Data;
using System.Data.Common;

namespace Hermes.Plugin.Sql.Execution;

/// <summary>
///     Hermes 数据库参数，桥接 ISqlExecutor 参数化接口。
/// </summary>
internal sealed class HermesDbParameter : DbParameter
{
    public HermesDbParameter(string parameterName, object? value)
    {
        ParameterName = parameterName;
        Value = value ?? DBNull.Value;
    }

    public override DbType DbType { get; set; }

    public override ParameterDirection Direction
    {
        get => ParameterDirection.Input;
        set { }
    }

    public override bool IsNullable { get; set; }

    public override DataRowVersion SourceVersion { get; set; }

    public override object? Value { get; set; }

    public override bool SourceColumnNullMapping { get; set; }

    public override int Size { get; set; }

    public override void ResetDbType()
    {
    }

#pragma warning disable CS8764 // 基类 DbParameter 的可空性注解在 .NET 预览版中不完整
    public override string? ParameterName { get; set; }

    public override string? SourceColumn { get; set; } = "";
#pragma warning restore CS8764
}