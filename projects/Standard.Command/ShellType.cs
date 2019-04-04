namespace Std.Command;

/// <summary>
///     Shell 补全脚本的目标 Shell 类型，支持内置预设以及自定义扩展
/// </summary>
public readonly record struct ShellType
{
    /// <summary>
    ///     Bash Shell
    /// </summary>
    public static readonly ShellType bash = new("bash");

    /// <summary>
    ///     Zsh Shell
    /// </summary>
    public static readonly ShellType zsh = new("zsh");

    /// <summary>
    ///     Fish Shell
    /// </summary>
    public static readonly ShellType fish = new("fish");

    /// <summary>
    ///     PowerShell
    /// </summary>
    public static readonly ShellType power_shell = new("power_shell");

    /// <summary>
    ///     使用指定名称创建 Shell 类型实例，可用于注册自定义 Shell
    /// </summary>
    /// <param name="name">Shell 名称标识符</param>
    public ShellType(string name)
    {
        this.name = name;
    }

    /// <summary>
    ///     Shell 名称标识符
    /// </summary>
    public string name { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return name;
    }

    /// <summary>
    ///     隐式转换为字符串
    /// </summary>
    public static implicit operator string(ShellType shell_type)
    {
        return shell_type.name;
    }

    /// <summary>
    ///     从字符串隐式转换
    /// </summary>
    public static implicit operator ShellType(string name)
    {
        return new ShellType(name);
    }
}