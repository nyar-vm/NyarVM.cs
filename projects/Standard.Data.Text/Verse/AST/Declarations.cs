namespace Std.Data.Text.Verse.AST;


/// <summary>

///     编译单元（源文件根节点）


/// </summary>
public sealed record CompilationUnit : AstNode
{
    public CompilationUnit(IReadOnlyList<AstNode> declarations, string filePath = "", TextSpan span = default(TextSpan))
        : base(span)
    {
        Declarations = declarations;
        FilePath = filePath;
    }

    public override NodeType Kind => NodeType.CompilationUnit;

    
/// <summary>
    
///     顶层声明列表
    

/// </summary>
    public IReadOnlyList<AstNode> Declarations { get; }

    
/// <summary>
    
///     源文件路径
    

/// </summary>
    public string FilePath { get; }
}


/// <summary>

///     场景声明


/// </summary>
public sealed record SceneDecl : AstNode
{
    public SceneDecl(string name, IReadOnlyList<AstNode> body, TextSpan span = default(TextSpan))
        : base(span)
    {
        Name = name;
        Body = body;
    }

    public override NodeType Kind => NodeType.SceneDecl;

    
/// <summary>
    
///     场景名称
    

/// </summary>
    public string Name { get; }

    
/// <summary>
    
///     场景内容
    

/// </summary>
    public IReadOnlyList<AstNode> Body { get; }
}


/// <summary>

///     标签声明


/// </summary>
public sealed record LabelDecl : AstNode
{
    public LabelDecl(string name, TextSpan span = default(TextSpan))
        : base(span)
    {
        Name = name;
    }

    public override NodeType Kind => NodeType.LabelDecl;

    
/// <summary>
    
///     标签名称
    

/// </summary>
    public string Name { get; }
}


/// <summary>

///     对话行


/// </summary>
public sealed record DialogueLine : AstNode
{
    public DialogueLine(string? speaker, string text, string? emotion = null, TextSpan span = default(TextSpan))
        : base(span)
    {
        Speaker = speaker;
        Text = text;
        Emotion = emotion;
    }

    public override NodeType Kind => NodeType.DialogueLine;

    
/// <summary>
    
///     说话角色名称（null 表示旁白）
    

/// </summary>
    public string? Speaker { get; }

    
/// <summary>
    
///     对话文本
    

/// </summary>
    public string Text { get; }

    
/// <summary>
    
///     角色表情
    

/// </summary>
    public string? Emotion { get; }
}


/// <summary>

///     旁白行


/// </summary>
public sealed record NarrationLine : AstNode
{
    public NarrationLine(string text, TextSpan span = default(TextSpan))
        : base(span)
    {
        Text = text;
    }

    public override NodeType Kind => NodeType.NarrationLine;

    
/// <summary>
    
///     旁白文本
    

/// </summary>
    public string Text { get; }
}


/// <summary>

///     选项菜单


/// </summary>
public sealed record MenuDecl : AstNode
{
    public MenuDecl(IReadOnlyList<MenuItem> items, TextSpan span = default(TextSpan))
        : base(span)
    {
        Items = items;
    }

    public override NodeType Kind => NodeType.MenuDecl;

    
/// <summary>
    
///     选项列表
    

/// </summary>
    public IReadOnlyList<MenuItem> Items { get; }
}


/// <summary>

///     选项项


/// </summary>
public sealed record MenuItem : AstNode
{
    public MenuItem(string text, AstNode? condition, IReadOnlyList<AstNode> body, TextSpan span = default(TextSpan))
        : base(span)
    {
        Text = text;
        Condition = condition;
        Body = body;
    }

    public override NodeType Kind => NodeType.MenuItem;

    
/// <summary>
    
///     选项显示文本
    

/// </summary>
    public string Text { get; }

    
/// <summary>
    
///     选择条件（null 表示无条件）
    

/// </summary>
    public AstNode? Condition { get; }

    
/// <summary>
    
///     选择后执行的语句块
    

/// </summary>
    public IReadOnlyList<AstNode> Body { get; }
}


/// <summary>

///     跳转语句


/// </summary>
public sealed record JumpStmt : AstNode
{
    public JumpStmt(string target, AstNode? condition = null, TextSpan span = default(TextSpan))
        : base(span)
    {
        Target = target;
        Condition = condition;
    }

    public override NodeType Kind => NodeType.JumpStmt;

    
/// <summary>
    
///     目标标签或场景名
    

/// </summary>
    public string Target { get; }

    
/// <summary>
    
///     跳转条件（null 表示无条件）
    

/// </summary>
    public AstNode? Condition { get; }
}


/// <summary>

///     调用语句


/// </summary>
public sealed record CallStmt : AstNode
{
    public CallStmt(string target, IReadOnlyList<AstNode> arguments, TextSpan span = default(TextSpan))
        : base(span)
    {
        Target = target;
        Arguments = arguments;
    }

    public override NodeType Kind => NodeType.CallStmt;

    
/// <summary>
    
///     目标标签或场景名
    

/// </summary>
    public string Target { get; }

    
/// <summary>
    
///     调用参数
    

/// </summary>
    public IReadOnlyList<AstNode> Arguments { get; }
}


/// <summary>

///     返回语句


/// </summary>
public sealed record ReturnStmt : AstNode
{
    public ReturnStmt(TextSpan span = default(TextSpan))
        : base(span)
    {
    }

    public override NodeType Kind => NodeType.ReturnStmt;
}


/// <summary>

///     暂停语句


/// </summary>
public sealed record PauseStmt : AstNode
{
    public PauseStmt(double? duration = null, TextSpan span = default(TextSpan))
        : base(span)
    {
        Duration = duration;
    }

    public override NodeType Kind => NodeType.PauseStmt;

    
/// <summary>
    
///     暂停时长（秒），null 表示等待用户点击
    

/// </summary>
    public double? Duration { get; }
}


/// <summary>

///     等待语句


/// </summary>
public sealed record WaitStmt : AstNode
{
    public WaitStmt(double duration, TextSpan span = default(TextSpan))
        : base(span)
    {
        Duration = duration;
    }

    public override NodeType Kind => NodeType.WaitStmt;

    
/// <summary>
    
///     等待时长（秒）
    

/// </summary>
    public double Duration { get; }
}


/// <summary>

///     变量设置语句


/// </summary>
public sealed record SetStmt : AstNode
{
    public SetStmt(string variableName, string op, AstNode value, TextSpan span = default(TextSpan))
        : base(span)
    {
        VariableName = variableName;
        Operator = op;
        Value = value;
    }

    public override NodeType Kind => NodeType.SetStmt;

    
/// <summary>
    
///     变量名
    

/// </summary>
    public string VariableName { get; }

    
/// <summary>
    
///     赋值运算符
    

/// </summary>
    public string Operator { get; }

    
/// <summary>
    
///     赋值表达式
    

/// </summary>
    public AstNode Value { get; }
}


/// <summary>

///     条件分支


/// </summary>
public sealed record IfStmt : AstNode
{
    public IfStmt(
        AstNode condition,
        IReadOnlyList<AstNode> thenBody,
        IReadOnlyList<ElifBranch> elifBranches,
        ElseBranch? elseBranch,
        TextSpan span = default(TextSpan))
        : base(span)
    {
        Condition = condition;
        ThenBody = thenBody;
        ElifBranches = elifBranches;
        ElseBranch = elseBranch;
    }

    public override NodeType Kind => NodeType.IfStmt;

    
/// <summary>
    
///     条件表达式
    

/// </summary>
    public AstNode Condition { get; }

    
/// <summary>
    
///     then 分支
    

/// </summary>
    public IReadOnlyList<AstNode> ThenBody { get; }

    
/// <summary>
    
///     elif 分支列表
    

/// </summary>
    public IReadOnlyList<ElifBranch> ElifBranches { get; }

    
/// <summary>
    
///     else 分支
    

/// </summary>
    public ElseBranch? ElseBranch { get; }
}


/// <summary>

///     elif 分支


/// </summary>
public sealed record ElifBranch : AstNode
{
    public ElifBranch(AstNode condition, IReadOnlyList<AstNode> body, TextSpan span = default(TextSpan))
        : base(span)
    {
        Condition = condition;
        Body = body;
    }

    public override NodeType Kind => NodeType.ElifBranch;

    
/// <summary>
    
///     条件表达式
    

/// </summary>
    public AstNode Condition { get; }

    
/// <summary>
    
///     elif 分支体
    

/// </summary>
    public IReadOnlyList<AstNode> Body { get; }
}


/// <summary>

///     else 分支


/// </summary>
public sealed record ElseBranch : AstNode
{
    public ElseBranch(IReadOnlyList<AstNode> body, TextSpan span = default(TextSpan))
        : base(span)
    {
        Body = body;
    }

    public override NodeType Kind => NodeType.ElseBranch;

    
/// <summary>
    
///     else 分支体
    

/// </summary>
    public IReadOnlyList<AstNode> Body { get; }
}


/// <summary>

///     命令调用


/// </summary>
public sealed record CommandCall : AstNode
{
    public CommandCall(string commandName, IReadOnlyList<CommandArg> arguments, TextSpan span = default(TextSpan))
        : base(span)
    {
        CommandName = commandName;
        Arguments = arguments;
    }

    public override NodeType Kind => NodeType.CommandCall;

    
/// <summary>
    
///     命令名称
    

/// </summary>
    public string CommandName { get; }

    
/// <summary>
    
///     命令参数列表
    

/// </summary>
    public IReadOnlyList<CommandArg> Arguments { get; }
}


/// <summary>

///     命令参数


/// </summary>
public sealed record CommandArg : AstNode
{
    public CommandArg(string? name, AstNode value, TextSpan span = default(TextSpan))
        : base(span)
    {
        Name = name;
        Value = value;
    }

    public override NodeType Kind => NodeType.CommandArg;

    
/// <summary>
    
///     参数名（命名参数）
    

/// </summary>
    public string? Name { get; }

    
/// <summary>
    
///     参数值
    

/// </summary>
    public AstNode Value { get; }
}