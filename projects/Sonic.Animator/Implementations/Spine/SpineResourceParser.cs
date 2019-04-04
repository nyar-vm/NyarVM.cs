using System.Threading.Tasks;
using Animator.Abstractions;

namespace Animator.Implementations.Spine;

/// <summary>
///     Spine 资源解析器适配器
///     <para>二进制格式（.skel）委托 Standard.Data.Binary.Spine 解码</para>
///     <para>文本格式（.json）委托 Sonic.Standard.Data.Text.Spine 解码</para>
///     <para>当前为桩实现，待 Acorn/Oak 格式项目就绪后对接</para>
/// </summary>
public sealed class SpineResourceParser : IResourceParser
{
    /// <summary>判断当前解析器是否支持该文件</summary>
    public bool can_parse(string filePath)
    {
        return filePath.EndsWith(".skel") || filePath.EndsWith(".json");
    }
    /// <summary>
    ///     异步解析文件为动画资源
    ///     <para>.skel 二进制文件将委托 Standard.Data.Binary.Spine.DecodeAsync</para>
    ///     <para>.json 文本文件将委托 Sonic.Standard.Data.Text.Spine.ParseAsync</para>
    /// </summary>
    public async Task<IAnimationResource> parse(string filePath)
    {
        await Task.CompletedTask;
        if (filePath.EndsWith(".json"))
        {
            return parse_text_resource(filePath);
        }
        return parse_binary_resource(filePath);
    }

    private IAnimationResource parse_binary_resource(string filePath)
    {
        // TODO: 委托 Standard.Data.Binary.Spine 进行二进制解码
        return new SpineAnimationResource(filePath, 512f, 512f);
    }

    private IAnimationResource parse_text_resource(string filePath)
    {
        // TODO: 委托 Sonic.Standard.Data.Text.Spine 进行文本解码
        return new SpineAnimationResource(filePath, 512f, 512f);
    }
}