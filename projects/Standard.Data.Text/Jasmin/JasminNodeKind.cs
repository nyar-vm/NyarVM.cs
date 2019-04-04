using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     Jasmin 语法节点类型
/// </summary>
public static class JasminNodeKind
{
    public static readonly NodeKind unknown = 0;

    #region 顶层声明

    public static readonly NodeKind class_directive = 1;
    public static readonly NodeKind super_directive = 2;
    public static readonly NodeKind implements_directive = 3;
    public static readonly NodeKind interface_directive = 4;
    public static readonly NodeKind field_directive = 5;
    public static readonly NodeKind method_directive = 6;
    public static readonly NodeKind end_method_directive = 7;
    public static readonly NodeKind source_directive = 8;
    public static readonly NodeKind version_directive = 9;

    #endregion

    #region 方法体指令

    public static readonly NodeKind limit_directive = 10;
    public static readonly NodeKind line_directive = 11;
    public static readonly NodeKind var_directive = 12;
    public static readonly NodeKind throws_directive = 13;
    public static readonly NodeKind catch_directive = 14;
    public static readonly NodeKind instruction = 15;
    public static readonly NodeKind label = 16;

    #endregion

    #region Token 类型

    public static readonly NodeKind directive = 20;
    public static readonly NodeKind opcode = 21;
    public static readonly NodeKind identifier = 22;
    public static readonly NodeKind descriptor = 23;
    public static readonly NodeKind number = 24;
    public static readonly NodeKind string_literal = 25;
    public static readonly NodeKind comment = 26;
    public static readonly NodeKind eof = 27;

    #endregion
}