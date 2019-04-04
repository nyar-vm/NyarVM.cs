namespace Std.Data.Text.Scss;

/// <summary>
///     SCSS Mixin 定义
/// </summary>
public sealed class ScssMixin
{
    public ScssMixin(string name, IReadOnlyList<string> parameters, string body)
    {
        this.name = name;
        this.parameters = parameters;
        this.body = body;
    }


    /// <summary>
    ///     Mixin 名称
    /// </summary>
    public string name { get; }


    /// <summary>
    ///     参数列表
    /// </summary>
    public IReadOnlyList<string> parameters { get; }


    /// <summary>
    ///     Mixin 体
    /// </summary>
    public string body { get; }
}