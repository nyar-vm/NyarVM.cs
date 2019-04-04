using System.Threading.Tasks;
using Animator.Abstractions;

namespace Animator.Implementations.Live2D;

/// <summary>
///     Live2D/Cubism 资源解析器适配器
///     <para>二进制格式（.moc3）委托 Standard.Data.Binary.Cubism 解码</para>
///     <para>文本格式（.model3.json）委托 Sonic.Standard.Data.Text.Cubism 解码</para>
///     <para>当前为桩实现，待 Acorn/Oak 格式项目就绪后对接</para>
/// </summary>
public sealed class Live2DResourceParser : IResourceParser
{
    /// <summary>判断当前解析器是否支持该文件</summary>
    public bool can_parse(string filePath)
    {
        return filePath.EndsWith(".model3.json") || filePath.EndsWith(".moc3");
    }
    /// <summary>
    ///     异步解析文件为动画资源
    ///     <para>.moc3 二进制文件将委托 Standard.Data.Binary.Cubism.DecodeAsync</para>
    ///     <para>.model3.json 文本文件将委托 Sonic.Standard.Data.Text.Cubism.ParseAsync</para>
    /// </summary>
    public async Task<IAnimationResource> parse(string filePath)
    {
        await Task.CompletedTask;
        if (filePath.EndsWith(".model3.json"))
        {
            return parse_text_resource(filePath);
        }
        return parse_binary_resource(filePath);
    }

    private IAnimationResource parse_binary_resource(string filePath)
    {
        // TODO: 委托 Standard.Data.Binary.Cubism 进行二进制解码
        return new Live2DAnimationResource(filePath, 512f, 512f);
    }

    private IAnimationResource parse_text_resource(string filePath)
    {
        // TODO: 委托 Sonic.Standard.Data.Text.Cubism 进行文本解码
        return new Live2DAnimationResource(filePath, 512f, 512f);
    }
}