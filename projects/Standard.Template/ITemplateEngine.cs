namespace Std.Template;

/// <summary>
///     模板引擎接口
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    ///     渲染模板
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="context">渲染上下文变量</param>
    /// <returns>渲染结果</returns>
    string Render(string template, IDictionary<string, object> context);
}