namespace Nyar.IR.Rewrite;

/// <summary>
///     模式变量标记类型，用于 [RewriteRule] 属性标记的模式方法参数中。
///     Source Generator 解析 Var&lt;T&gt; 参数，在生成 IRewriteRule 实现时将其替换为 EGraph 中的 Id 绑定。
/// </summary>
/// <typeparam name="T">变量携带的类型信息（仅用于编译时标记）</typeparam>
public readonly struct Var<T>
{
    /// <summary>
    ///     初始化模式变量
    /// </summary>
    /// <param name="name">变量名称。</param>
    public Var(string name)
    {
        this.name = name;
    }

    /// <summary>
    ///     变量名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     隐式转换到 T，允许在需要 T 的地方使用 Var&lt;T&gt;
    /// </summary>
    public static implicit operator T(Var<T> v)
    {
        return default!;
    }
}