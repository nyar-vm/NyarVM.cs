namespace Core.Compiler.Document.Template;

/// <summary>
///     模板引擎接口，定义模板渲染的契约
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    ///     使用指定的模型渲染模板
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="model">数据模型</param>
    /// <returns>渲染后的字符串</returns>
    string render(string template, object model);
}