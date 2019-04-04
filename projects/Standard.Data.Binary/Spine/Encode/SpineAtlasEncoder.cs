using System.Text;
using Std.Data.Binary.Spine.Data;

namespace Std.Data.Binary.Spine.Encode;

/// <summary>
///     Spine Atlas 编码器，的<see cref="SpineAtlasData" /> 编码的Spine Atlas 文本格式的
/// </summary>
/// <remarks>
///     Spine Atlas 格式是纯文本格式，包含页面定义和区域定义的
///     每个页面的size 属性开头，后跟区域定义的
/// </remarks>
public sealed class SpineAtlasEncoder
{
    private readonly StringBuilder _builder = new();

    /// <summary>
    ///     的Atlas 数据编码为文本字符串的
    /// </summary>
    /// <param name="data">
    ///     Atlas 数据的/param>
    ///     <returns>编码后的 Atlas 文本的/returns>
    public string encode(SpineAtlasData data)
    {
        _builder.Clear();

        for (var pageIndex = 0; pageIndex < data.pages.Count; pageIndex++)
        {
            var page = data.pages[pageIndex];
            encode_page(page);

            var regions = data.regions.Where(r => r.page_index == pageIndex).ToList();

            foreach (var region in regions) encode_region(region);
        }

        return _builder.ToString();
    }

    private void encode_page(SpineAtlasPage page)
    {
        _builder.AppendLine(page.texture_file_path);
        _builder.AppendLine($"size: {page.width}, {page.height}");

        if (!string.IsNullOrEmpty(page.format)) _builder.AppendLine($"format: {page.format}");

        if (!string.IsNullOrEmpty(page.filter_min) || !string.IsNullOrEmpty(page.filter_mag))
        {
            var filterMin = page.filter_min;
            var filterMag = page.filter_mag;

            if (string.IsNullOrEmpty(filterMin)) filterMin = filterMag;

            if (string.IsNullOrEmpty(filterMag)) filterMag = filterMin;

            _builder.AppendLine($"filter: {filterMin}, {filterMag}");
        }

        var wrapS = page.wrap_s.ToLowerInvariant();
        var wrapT = page.wrap_t.ToLowerInvariant();

        if (wrapS == "repeat" && wrapT == "repeat")
            _builder.AppendLine("repeat: xy");
        else if (wrapS == "repeat")
            _builder.AppendLine("repeat: x");
        else if (wrapT == "repeat") _builder.AppendLine("repeat: y");

        _builder.AppendLine();
    }

    private void encode_region(SpineAtlasRegion region)
    {
        _builder.AppendLine(region.name);
        _builder.AppendLine($"  bounds: {region.x}, {region.y}, {region.width}, {region.height}");

        if (region.offset_x != 0 || region.offset_y != 0)
            _builder.AppendLine($"  offset: {region.offset_x}, {region.offset_y}");

        if (region.original_width != 0 || region.original_height != 0)
            _builder.AppendLine($"  orig: {region.original_width}, {region.original_height}");

        if (region.is_rotated) _builder.AppendLine("  rotate: 90");

        if (region is { is_split: true, splits: not null })
            _builder.AppendLine($"  split: {string.Join(", ", region.splits)}");

        if (region.pads != null) _builder.AppendLine($"  pad: {string.Join(", ", region.pads)}");

        _builder.AppendLine();
    }
}