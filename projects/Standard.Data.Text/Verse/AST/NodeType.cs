namespace Std.Data.Text.Verse.AST;


/// <summary>

///     AST 节点类型枚举


/// </summary>
public enum NodeType
{
    
/// <summary>
    
///     编译单元（源文件根节点）
    

/// </summary>
    CompilationUnit,

    
/// <summary>
    
///     场景声明
    

/// </summary>
    SceneDecl,

    
/// <summary>
    
///     标签声明
    

/// </summary>
    LabelDecl,

    
/// <summary>
    
///     对话行
    

/// </summary>
    DialogueLine,

    
/// <summary>
    
///     旁白行
    

/// </summary>
    NarrationLine,

    
/// <summary>
    
///     选项菜单
    

/// </summary>
    MenuDecl,

    
/// <summary>
    
///     选项项
    

/// </summary>
    MenuItem,

    
/// <summary>
    
///     跳转语句
    

/// </summary>
    JumpStmt,

    
/// <summary>
    
///     调用语句
    

/// </summary>
    CallStmt,

    
/// <summary>
    
///     返回语句
    

/// </summary>
    ReturnStmt,

    
/// <summary>
    
///     暂停语句
    

/// </summary>
    PauseStmt,

    
/// <summary>
    
///     等待语句
    

/// </summary>
    WaitStmt,

    
/// <summary>
    
///     变量设置语句
    

/// </summary>
    SetStmt,

    
/// <summary>
    
///     条件分支
    

/// </summary>
    IfStmt,

    
/// <summary>
    
///     elif 分支
    

/// </summary>
    ElifBranch,

    
/// <summary>
    
///     else 分支
    

/// </summary>
    ElseBranch,

    
/// <summary>
    
///     命令调用
    

/// </summary>
    CommandCall,

    
/// <summary>
    
///     命令参数
    

/// </summary>
    CommandArg,

    
/// <summary>
    
///     二元表达式
    

/// </summary>
    BinaryExpr,

    
/// <summary>
    
///     一元表达式
    

/// </summary>
    UnaryExpr,

    
/// <summary>
    
///     字面量表达式
    

/// </summary>
    LiteralExpr,

    
/// <summary>
    
///     标识符表达式
    

/// </summary>
    IdentifierExpr,

    
/// <summary>
    
///     字符串插值表达式
    

/// </summary>
    InterpolatedStringExpr,

    
/// <summary>
    
///     成员访问表达式
    

/// </summary>
    MemberAccessExpr,

    
/// <summary>
    
///     赋值表达式
    

/// </summary>
    AssignmentExpr
}