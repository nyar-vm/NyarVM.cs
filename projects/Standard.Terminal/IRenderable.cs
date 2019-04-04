using Core.Terminal;

namespace Std.Terminal;

/// <summary>
///     可渲染布局接口，扩展 <see cref="ILayout" /> 增加在指定区域内渲染自身的能力
/// </summary>
public interface IRenderable : ILayout
{
    /// <summary>
    ///     在指定区域内渲染此组件
    /// </summary>
    /// <param name="renderer">终端渲染器</param>
    /// <param name="region">渲染区域</param>
    void render(AnsiTerminalRenderer renderer, Rectangle region);
}