using System.Threading.Tasks;
using Animator.Abstractions;

namespace Animator.Implementations.SpriteSheet;

/// <summary>
///     序列帧动画资源解析器适配器
///     <para>二进制格式委托 Standard.Data.Binary.SpriteSheet 解码</para>
///     <para>文本格式（.atlas）委托 Sonic.Standard.Data.Text.SpriteSheet 解码</para>
///     <para>当前为桩实现，待 Acorn/Oak 格式项目就绪后对接</para>
/// </summary>
public sealed class SpriteSheetResourceParser : IResourceParser
{
    /// <summary>判断当前解析器是否支持该文件</summary>
    public bool can_parse(string filePath)
    {
        return filePath.EndsWith(".spritesheet") || filePath.EndsWith(".atlas");
    }
    /// <summary>
    ///     异步解析文件为动画资源
    ///     <para>.spritesheet 二进制文件将委托 Standard.Data.Binary.SpriteSheet.DecodeAsync</para>
    ///     <para>.atlas 文本文件将委托 Sonic.Standard.Data.Text.SpriteSheet.ParseAsync</para>
    /// </summary>
    public async Task<IAnimationResource> parse(string filePath)
    {
        await Task.CompletedTask;
        if (filePath.EndsWith(".atlas"))
        {
            return parse_text_resource(filePath);
        }
        return parse_binary_resource(filePath);
    }

    private IAnimationResource parse_binary_resource(string filePath)
    {
        // TODO: 委托 Standard.Data.Binary.SpriteSheet 进行二进制解码
        return new SpriteSheetAnimationResource(filePath, 512f, 512f, 1, 30f);
    }

    private IAnimationResource parse_text_resource(string filePath)
    {
        // TODO: 委托 Sonic.Standard.Data.Text.SpriteSheet 进行文本解码
        return new SpriteSheetAnimationResource(filePath, 512f, 512f, 1, 30f);
    }
}