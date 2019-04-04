namespace Std.Template.Hbs;

/// <summary>
///     Handlebars 模板引擎渲染器（占位实现）
/// </summary>
public sealed class HbsRenderer : ITemplateEngine
{
    /// <summary>
    ///     渲染模板
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="context">渲染上下文变量</param>
    /// <returns>渲染结果</returns>
    /// <exception cref="NotImplementedException">占位实现，尚未完成</exception>
    public string Render(string template, IDictionary<string, object> context)
    {
        throw new NotImplementedException("Handlebars 模板引擎尚未实现");
    }
}