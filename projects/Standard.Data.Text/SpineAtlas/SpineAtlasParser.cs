using System.Globalization;

namespace Std.Data.Text.SpineAtlas;

/// <summary>
///     Spine Atlas 文本格式解析器
/// </summary>
public static class SpineAtlasParser
{
    /// <summary>
    ///     解析 Spine Atlas 文本
    /// </summary>
    public static SpineAtlasData parse(string atlasContent)
    {
        var pages = new List<SpineAtlasPage>();
        var regions = new List<SpineAtlasRegion>();

        using var reader = new StringReader(atlasContent);
        var line = reader.ReadLine();
        var currentPageIndex = -1;

        while (line != null)
        {
            line = line.Trim();

            if (string.IsNullOrEmpty(line))
            {
                line = reader.ReadLine();
                continue;
            }

            if (!line.Contains(':'))
            {
                if (currentPageIndex >= 0) regions.Add(parse_region(line, reader, currentPageIndex));
            }
            else if (line.StartsWith("size:", StringComparison.OrdinalIgnoreCase))
            {
                currentPageIndex++;
                pages.Add(parse_page(line, reader));
            }

            line = reader.ReadLine();
        }

        return new SpineAtlasData
        {
            pages = pages,
            regions = regions
        };
    }

    private static SpineAtlasPage parse_page(string firstLine, StringReader reader)
    {
        var textureFilePath = string.Empty;
        var width = 0;
        var height = 0;
        var format = string.Empty;
        var filterMin = string.Empty;
        var filterMag = string.Empty;
        var wrapS = "clampToEdge";
        var wrapT = "clampToEdge";

        var parts = firstLine.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            var sizeParts = parts[1].Split(',', StringSplitOptions.TrimEntries);
            if (sizeParts.Length >= 2)
            {
                int.TryParse(sizeParts[0], out width);
                int.TryParse(sizeParts[1], out height);
            }
        }

        var line = reader.ReadLine();
        while (line != null && !string.IsNullOrWhiteSpace(line) && line.Contains(':'))
        {
            line = line.Trim();
            var kv = line.Split(':', StringSplitOptions.TrimEntries);
            if (kv.Length >= 2)
            {
                var key = kv[0].ToLowerInvariant();
                var value = kv[1];

                switch (key)
                {
                    case "format":
                        format = value;
                        break;
                    case "filter":
                        var filters = value.Split(',', StringSplitOptions.TrimEntries);
                        filterMin = filters.Length > 0 ? filters[0] : value;
                        filterMag = filters.Length > 1 ? filters[1] : filterMin;
                        break;
                    case "repeat":
                        wrapS = value.ToLowerInvariant() switch
                        {
                            "x" => "repeat",
                            "y" => "repeat",
                            "xy" => "repeat",
                            _ => "clampToEdge"
                        };
                        wrapT = value.ToLowerInvariant() switch
                        {
                            "x" => "clampToEdge",
                            "y" => "clampToEdge",
                            "xy" => "repeat",
                            _ => "clampToEdge"
                        };
                        break;
                }
            }

            line = reader.ReadLine();
        }

        return new SpineAtlasPage
        {
            texture_file_path = textureFilePath,
            width = width,
            height = height,
            format = format,
            filter_min = filterMin,
            filter_mag = filterMag,
            wrap_s = wrapS,
            wrap_t = wrapT
        };
    }

    private static SpineAtlasRegion parse_region(string name, StringReader reader, int pageIndex)
    {
        var region = new SpineAtlasRegion
        {
            name = name,
            page_index = pageIndex
        };

        var line = reader.ReadLine();
        while (line != null && !string.IsNullOrWhiteSpace(line) && line.Contains(':'))
        {
            line = line.Trim();
            var kv = line.Split(':', StringSplitOptions.TrimEntries);
            if (kv.Length >= 2)
            {
                var key = kv[0].ToLowerInvariant();
                var value = kv[1];

                switch (key)
                {
                    case "bounds":
                    case "xy":
                        var xyParts = value.Split(',', StringSplitOptions.TrimEntries);
                        if (xyParts.Length >= 2)
                        {
                            int.TryParse(xyParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x);
                            int.TryParse(xyParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y);
                            region = region with { x = x, y = y };
                        }

                        break;
                    case "size":
                        var sizeParts = value.Split(',', StringSplitOptions.TrimEntries);
                        if (sizeParts.Length >= 2)
                        {
                            int.TryParse(sizeParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var w);
                            int.TryParse(sizeParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var h);
                            region = region with { width = w, height = h };
                        }

                        break;
                    case "offset":
                        var offsetParts = value.Split(',', StringSplitOptions.TrimEntries);
                        if (offsetParts.Length >= 2)
                        {
                            int.TryParse(offsetParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture,
                                out var ox);
                            int.TryParse(offsetParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture,
                                out var oy);
                            region = region with { offset_x = ox, offset_y = oy };
                        }

                        break;
                    case "orig":
                    case "original":
                        var origParts = value.Split(',', StringSplitOptions.TrimEntries);
                        if (origParts.Length >= 2)
                        {
                            int.TryParse(origParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ow);
                            int.TryParse(origParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var oh);
                            region = region with { original_width = ow, original_height = oh };
                        }

                        break;
                    case "rotate":
                        region = region with
                        {
                            is_rotated = value.Equals("90", StringComparison.OrdinalIgnoreCase)
                                         || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                        };
                        break;
                    case "split":
                        region = region with { is_split = true, splits = parse_int_array(value) };
                        break;
                    case "pad":
                        region = region with { pads = parse_int_array(value) };
                        break;
                }
            }

            line = reader.ReadLine();
        }

        return region;
    }

    private static int[] parse_int_array(string value)
    {
        return
        [
            .. value.Split(',', StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                    ? result
                    : 0)
        ];
    }
}