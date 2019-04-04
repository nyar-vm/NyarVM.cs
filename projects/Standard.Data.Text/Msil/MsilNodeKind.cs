using Std.Data.Text.Syntax;

namespace Std.Data.Text.Msil;

/// <summary>
///     MSIL 语法节点类型
/// </summary>
public static class MsilNodeKind
{
    public static readonly NodeKind unknown = 0;

    #region 顶层声明

    public static readonly NodeKind assembly_directive = 1;
    public static readonly NodeKind module_directive = 2;
    public static readonly NodeKind namespace_directive = 3;
    public static readonly NodeKind class_directive = 4;
    public static readonly NodeKind method_directive = 5;
    public static readonly NodeKind field_directive = 6;

    #endregion

    #region 方法体

    public static readonly NodeKind max_stack_directive = 10;
    public static readonly NodeKind locals_directive = 11;
    public static readonly NodeKind entry_point_directive = 12;
    public static readonly NodeKind instruction = 13;
    public static readonly NodeKind label = 14;

    #endregion

    #region Token 类型

    public static readonly NodeKind directive = 20;
    public static readonly NodeKind opcode = 21;
    public static readonly NodeKind identifier = 22;
    public static readonly NodeKind type_reference = 23;
    public static readonly NodeKind number = 24;
    public static readonly NodeKind string_literal = 25;
    public static readonly NodeKind comment = 26;
    public static readonly NodeKind eof = 27;

    #endregion
}