namespace Std.DL.Topos;

/// <summary>端口类型定义（Name + DataType）</summary>
public sealed class Port
{
    /// <summary>端口名称</summary>
    public string Name { get; init; } = "";

    /// <summary>端口数据类型</summary>
    public string DataType { get; init; } = "";

    /// <summary>是否为输入端口</summary>
    public bool IsInput { get; init; } = true;
}