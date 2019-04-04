namespace Std.Data.Text.Scss;

/// <summary>
///     SCSS 变量作用域
/// </summary>
public sealed class ScssVariableScope
{
    private readonly Dictionary<string, string> _variables = new();

    public ScssVariableScope(ScssVariableScope? parent = null)
    {
        this.parent = parent;
    }


    /// <summary>
    ///     父作用域
    /// </summary>
    public ScssVariableScope? parent { get; }


    /// <summary>
    ///     获取所有变量（包含父作用域）
    /// </summary>
    public IReadOnlyDictionary<string, string> all_variables
    {
        get
        {
            var result = new Dictionary<string, string>();
            collect_into(result);
            return result;
        }
    }


    /// <summary>
    ///     定义变量
    /// </summary>
    public void define(string name, string value)
    {
        _variables[name] = value;
    }


    /// <summary>
    ///     尝试解析变量
    /// </summary>
    public bool try_resolve(string name, out string value)
    {
        if (_variables.TryGetValue(name, out value!)) return true;

        if (parent != null) return parent.try_resolve(name, out value);

        value = default!;
        return false;
    }


    /// <summary>
    ///     解析变量
    /// </summary>
    public string? resolve(string name)
    {
        return try_resolve(name, out var value) ? value : null;
    }


    /// <summary>
    ///     创建子作用域
    /// </summary>
    public ScssVariableScope push()
    {
        return new ScssVariableScope(this);
    }

    private void collect_into(Dictionary<string, string> target)
    {
        parent?.collect_into(target);

        foreach (var (key, value) in _variables) target[key] = value;
    }
}