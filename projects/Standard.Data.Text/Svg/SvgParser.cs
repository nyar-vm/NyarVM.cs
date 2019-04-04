using System.Xml.Linq;

namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 文档解析器，基于 System.Xml.Linq 解析 SVG XML 结构
/// </summary>
public sealed class SvgParser
{
    private static readonly XNamespace _xlink_ns = "http://www.w3.org/1999/xlink";

    /// <summary>
    ///     解析 SVG 文本内容
    /// </summary>
    public SvgDocument parse(string content)
    {
        var xmlDoc = XDocument.Parse(content);
        var root = xmlDoc.Root;

        if (root is null) throw new FormatException("SVG 解析失败：未找到根元素");

        if (root.Name.LocalName is not ("svg" or "svg:svg"))
            throw new FormatException($"SVG 解析失败：根元素不是 svg，而是 {root.Name.LocalName}");

        var svgRoot = parse_svg_root(root);
        parse_children(root, svgRoot);

        return new SvgDocument { root = svgRoot };
    }

    private SvgRootElement parse_svg_root(XElement element)
    {
        var viewBox = parse_view_box(get_attr(element, "viewBox"));
        var (width, widthUnit) = parse_length(get_attr(element, "width"));
        var (height, heightUnit) = parse_length(get_attr(element, "height"));

        return new SvgRootElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            view_box = viewBox,
            width = width,
            height = height,
            width_unit = widthUnit,
            height_unit = heightUnit
        };
    }

    private SvgElement parse_element(XElement element)
    {
        var name = element.Name.LocalName;

        if (name.Contains(':')) name = name[(name.IndexOf(':') + 1)..];

        SvgElement svgElement = name switch
        {
            "g" => parse_group_element(element),
            "path" => parse_path_element(element),
            "rect" => parse_rect_element(element),
            "circle" => parse_circle_element(element),
            "ellipse" => parse_ellipse_element(element),
            "line" => parse_line_element(element),
            "polyline" => parse_polyline_element(element),
            "polygon" => parse_polygon_element(element),
            "text" => parse_text_element(element),
            "defs" => parse_defs_element(element),
            "use" => parse_use_element(element),
            "image" => parse_image_element(element),
            "linearGradient" => parse_linear_gradient_element(element),
            "radialGradient" => parse_radial_gradient_element(element),
            "clipPath" => parse_clip_path_element(element),
            "mask" => parse_mask_element(element),
            _ => parse_unknown_element(element)
        };

        parse_children(element, svgElement);

        return svgElement;
    }

    private void parse_children(XElement xmlElement, SvgElement svgElement)
    {
        foreach (var child in xmlElement.Elements()) svgElement.children.Add(parse_element(child));
    }

    #region 元素解析

    private SvgGroupElement parse_group_element(XElement element)
    {
        return new SvgGroupElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element)
        };
    }

    private SvgPathElement parse_path_element(XElement element)
    {
        var d = get_attr(element, "d") ?? string.Empty;

        return new SvgPathElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            commands = SvgPathDataParser.parse(d)
        };
    }

    private SvgRectElement parse_rect_element(XElement element)
    {
        return new SvgRectElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            x = parse_float(get_attr(element, "x"), 0f),
            y = parse_float(get_attr(element, "y"), 0f),
            width = parse_float(get_attr(element, "width"), 0f),
            height = parse_float(get_attr(element, "height"), 0f),
            rx = parse_float(get_attr(element, "rx"), 0f),
            ry = parse_float(get_attr(element, "ry"), 0f)
        };
    }

    private SvgCircleElement parse_circle_element(XElement element)
    {
        return new SvgCircleElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            cx = parse_float(get_attr(element, "cx"), 0f),
            cy = parse_float(get_attr(element, "cy"), 0f),
            r = parse_float(get_attr(element, "r"), 0f)
        };
    }

    private SvgEllipseElement parse_ellipse_element(XElement element)
    {
        return new SvgEllipseElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            cx = parse_float(get_attr(element, "cx"), 0f),
            cy = parse_float(get_attr(element, "cy"), 0f),
            rx = parse_float(get_attr(element, "rx"), 0f),
            ry = parse_float(get_attr(element, "ry"), 0f)
        };
    }

    private SvgLineElement parse_line_element(XElement element)
    {
        return new SvgLineElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            x1 = parse_float(get_attr(element, "x1"), 0f),
            y1 = parse_float(get_attr(element, "y1"), 0f),
            x2 = parse_float(get_attr(element, "x2"), 0f),
            y2 = parse_float(get_attr(element, "y2"), 0f)
        };
    }

    private SvgPolylineElement parse_polyline_element(XElement element)
    {
        return new SvgPolylineElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            points = parse_points(get_attr(element, "points"))
        };
    }

    private SvgPolygonElement parse_polygon_element(XElement element)
    {
        return new SvgPolygonElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            points = parse_points(get_attr(element, "points"))
        };
    }

    private SvgTextElement parse_text_element(XElement element)
    {
        return new SvgTextElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            x = parse_float(get_attr(element, "x"), 0f),
            y = parse_float(get_attr(element, "y"), 0f),
            text = element.Value ?? string.Empty,
            font_family = get_attr(element, "font-family") ?? string.Empty,
            font_size = parse_float(get_attr(element, "font-size"), 16f),
            text_anchor = get_attr(element, "text-anchor") ?? "start"
        };
    }

    private SvgDefsElement parse_defs_element(XElement element)
    {
        return new SvgDefsElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element)
        };
    }

    private SvgUseElement parse_use_element(XElement element)
    {
        var href = get_attr(element, "href")
                   ?? get_attr(element, _xlink_ns + "href")
                   ?? string.Empty;

        return new SvgUseElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            href = href,
            x = parse_float(get_attr(element, "x"), 0f),
            y = parse_float(get_attr(element, "y"), 0f),
            width = parse_float(get_attr(element, "width"), 0f),
            height = parse_float(get_attr(element, "height"), 0f)
        };
    }

    private SvgImageElement parse_image_element(XElement element)
    {
        var href = get_attr(element, "href")
                   ?? get_attr(element, _xlink_ns + "href")
                   ?? string.Empty;

        return new SvgImageElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            href = href,
            x = parse_float(get_attr(element, "x"), 0f),
            y = parse_float(get_attr(element, "y"), 0f),
            width = parse_float(get_attr(element, "width"), 0f),
            height = parse_float(get_attr(element, "height"), 0f)
        };
    }

    private SvgLinearGradientElement parse_linear_gradient_element(XElement element)
    {
        return new SvgLinearGradientElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            x1 = parse_float(get_attr(element, "x1"), 0f),
            y1 = parse_float(get_attr(element, "y1"), 0f),
            x2 = parse_float(get_attr(element, "x2"), 1f),
            y2 = parse_float(get_attr(element, "y2"), 0f),
            gradient_units = get_attr(element, "gradientUnits") ?? "objectBoundingBox"
        };
    }

    private SvgRadialGradientElement parse_radial_gradient_element(XElement element)
    {
        var cx = parse_float(get_attr(element, "cx"), 0.5f);
        var cy = parse_float(get_attr(element, "cy"), 0.5f);

        return new SvgRadialGradientElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            cx = cx,
            cy = cy,
            r = parse_float(get_attr(element, "r"), 0.5f),
            fx = parse_float(get_attr(element, "fx"), cx),
            fy = parse_float(get_attr(element, "fy"), cy),
            gradient_units = get_attr(element, "gradientUnits") ?? "objectBoundingBox"
        };
    }

    private SvgClipPathElement parse_clip_path_element(XElement element)
    {
        return new SvgClipPathElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            clip_path_units = get_attr(element, "clipPathUnits") ?? "userSpaceOnUse"
        };
    }

    private SvgMaskElement parse_mask_element(XElement element)
    {
        return new SvgMaskElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            mask_units = get_attr(element, "maskUnits") ?? "objectBoundingBox",
            mask_content_units = get_attr(element, "maskContentUnits") ?? "userSpaceOnUse"
        };
    }

    private SvgUnknownElement parse_unknown_element(XElement element)
    {
        var rawAttrs = new List<(string Name, string Value)>();

        foreach (var attr in element.Attributes()) rawAttrs.Add((attr.Name.LocalName, attr.Value));

        return new SvgUnknownElement
        {
            id = get_attr(element, "id") ?? string.Empty,
            @class = get_attr(element, "class") ?? string.Empty,
            transforms = parse_transform_attribute(get_attr(element, "transform")),
            style = parse_style_from_element(element),
            original_name = element.Name.LocalName,
            raw_attributes = rawAttrs
        };
    }

    #endregion

    #region 属性解析

    private static string? get_attr(XElement element, string name)
    {
        return element.Attribute(name)?.Value;
    }

    private static string? get_attr(XElement element, XName name)
    {
        return element.Attribute(name)?.Value;
    }

    private static float[] parse_view_box(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 4) return [];

        var viewBox = new float[4];

        for (var i = 0; i < 4; i++)
            if (!float.TryParse(parts[i], out viewBox[i]))
                return [];

        return viewBox;
    }

    private static (float Value, string Unit) parse_length(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (0f, string.Empty);

        var span = value.AsSpan();
        var i = 0;

        while (i < span.Length && (char.IsDigit(span[i]) || span[i] is '.' or '-' or '+' or 'e' or 'E')) i++;

        var numStr = span[..i];
        var unit = i < span.Length ? span[i..].ToString() : string.Empty;

        if (float.TryParse(numStr, out var num)) return (num, unit);

        return (0f, string.Empty);
    }

    private static List<SvgTransform> parse_transform_attribute(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];

        return SvgTransformParser.parse(value);
    }

    private static SvgStyle parse_style_from_element(XElement element)
    {
        var fill = get_attr(element, "fill");
        var stroke = get_attr(element, "stroke");
        var styleAttr = get_attr(element, "style");

        if (!string.IsNullOrWhiteSpace(styleAttr)) return parse_style_from_attribute(styleAttr, fill, stroke);

        return new SvgStyle
        {
            fill = fill,
            fill_opacity = parse_float(get_attr(element, "fill-opacity"), 1f),
            fill_rule = get_attr(element, "fill-rule") ?? "nonzero",
            stroke = stroke,
            stroke_width = parse_float(get_attr(element, "stroke-width"), 0f),
            stroke_opacity = parse_float(get_attr(element, "stroke-opacity"), 1f),
            stroke_linecap = get_attr(element, "stroke-linecap") ?? "butt",
            stroke_linejoin = get_attr(element, "stroke-linejoin") ?? "miter",
            stroke_dashoffset = parse_float(get_attr(element, "stroke-dashoffset"), 0f),
            stroke_dasharray = parse_dasharray(get_attr(element, "stroke-dasharray")),
            opacity = parse_float(get_attr(element, "opacity"), 1f)
        };
    }

    private static SvgStyle parse_style_from_attribute(string styleAttr, string? fill, string? stroke)
    {
        var style = new SvgStyle
        {
            fill = fill,
            stroke = stroke
        };

        var declarations = styleAttr.Split(';');

        foreach (var decl in declarations)
        {
            var colonIndex = decl.IndexOf(':');

            if (colonIndex < 0) continue;

            var property = decl[..colonIndex].Trim().ToLowerInvariant();
            var value = decl[(colonIndex + 1)..].Trim();

            style = property switch
            {
                "fill" => style with { fill = value },
                "fill-opacity" => style with { fill_opacity = parse_float(value, 1f) },
                "fill-rule" => style with { fill_rule = value },
                "stroke" => style with { stroke = value },
                "stroke-width" => style with { stroke_width = parse_float(value, 0f) },
                "stroke-opacity" => style with { stroke_opacity = parse_float(value, 1f) },
                "stroke-linecap" => style with { stroke_linecap = value },
                "stroke-linejoin" => style with { stroke_linejoin = value },
                "stroke-dashoffset" => style with { stroke_dashoffset = parse_float(value, 0f) },
                "stroke-dasharray" => style with { stroke_dasharray = parse_dasharray(value) },
                "opacity" => style with { opacity = parse_float(value, 1f) },
                _ => style
            };
        }

        return style;
    }

    private static float[] parse_points(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var points = new List<float>();

        foreach (var part in parts)
        {
            var coords = part.Split(',');

            foreach (var coord in coords)
                if (float.TryParse(coord, out var v))
                    points.Add(v);
        }

        return [.. points];
    }

    private static float[] parse_dasharray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "none") return [];

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<float>();

        foreach (var part in parts)
            if (float.TryParse(part.TrimEnd(','), out var v))
                result.Add(v);

        return [.. result];
    }

    private static float parse_float(string? value, float defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;

        return float.TryParse(value, out var result) ? result : defaultValue;
    }

    #endregion
}