using System.Text;
using System.Text.RegularExpressions;

namespace Std.Data.Text.Tailwind;

/// <summary>
///     Tailwind 工具类 CSS 生成器
///     扫描源码中的 Tailwind 类名，生成对应的 CSS 规则
/// </summary>
public sealed class TailwindGenerator
{
    private readonly TailwindConfig _config;

    public TailwindGenerator(TailwindConfig? config = null)
    {
        _config = config ?? TailwindConfig.@default;
    }


    /// <summary>
    ///     从源码文本中扫描 Tailwind 类名并生成 CSS
    /// </summary>
    public string generate(string source)
    {
        var classNames = scan_class_names(source);
        return generate_css(classNames);
    }


    /// <summary>
    ///     从多个源码文件中扫描 Tailwind 类名并生成 CSS
    /// </summary>
    public string generate(IEnumerable<string> sources)
    {
        var allClassNames = new HashSet<string>();

        foreach (var source in sources)
        foreach (var name in scan_class_names(source))
            allClassNames.Add(name);

        return generate_css(allClassNames);
    }


    /// <summary>
    ///     从源码中扫描 class 属性值
    /// </summary>
    public IReadOnlyList<string> scan_class_names(string source)
    {
        var classNames = new List<string>();

        var classAttrPattern = new Regex(@"class(?:Name)?\s*[=:]\s*[""'`]([^""'`]+)[""'`]", RegexOptions.Compiled);
        var matches = classAttrPattern.Matches(source);

        foreach (Match match in matches)
        {
            if (!match.Success) continue;

            var classList = match.Groups[1].Value;
            foreach (var name in classList.Split(' ', StringSplitOptions.RemoveEmptyEntries)) classNames.Add(name);
        }

        return classNames;
    }


    /// <summary>
    ///     从类名集合生成 CSS
    /// </summary>
    public string generate_css(IEnumerable<string> classNames)
    {
        var sb = new StringBuilder();
        var processed = new HashSet<string>();

        foreach (var name in classNames)
        {
            if (!processed.Add(name)) continue;

            var rule = resolve_utility_rule(name);
            if (rule is null) continue;

            sb.Append('.');
            sb.Append(escape_class_name(name));
            sb.Append(" { ");
            sb.Append(rule);
            sb.Append(" }");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string escape_class_name(string name)
    {
        var sb = new StringBuilder(name.Length + 8);

        foreach (var ch in name)
            if (ch is ':' or '/' or '.' or '[' or ']' or '(' or ')' or '%' or '@')
            {
                sb.Append('\\');
                sb.Append(ch);
            }
            else
            {
                sb.Append(ch);
            }

        return sb.ToString();
    }

    private string? resolve_utility_rule(string name)
    {
        if (try_resolve_spacing(name, out var spacing)) return spacing;

        if (try_resolve_sizing(name, out var sizing)) return sizing;

        if (try_resolve_color(name, out var color)) return color;

        if (try_resolve_typography(name, out var typo)) return typo;

        if (try_resolve_flexbox(name, out var flex)) return flex;

        if (try_resolve_grid(name, out var grid)) return grid;

        if (try_resolve_border(name, out var border)) return border;

        if (try_resolve_display(name, out var display)) return display;

        if (try_resolve_position(name, out var pos)) return pos;

        if (try_resolve_overflow(name, out var overflow)) return overflow;

        return null;
    }

    #region Spacing

    private bool try_resolve_spacing(string name, out string? rule)
    {
        rule = null;

        var spacingMap = new Dictionary<string, string>
        {
            ["p-0"] = "padding: 0", ["p-1"] = "padding: 0.25rem", ["p-2"] = "padding: 0.5rem",
            ["p-3"] = "padding: 0.75rem", ["p-4"] = "padding: 1rem", ["p-5"] = "padding: 1.25rem",
            ["p-6"] = "padding: 1.5rem", ["p-8"] = "padding: 2rem", ["p-10"] = "padding: 2.5rem",
            ["p-12"] = "padding: 3rem", ["p-16"] = "padding: 4rem", ["p-24"] = "padding: 6rem",
            ["px-0"] = "padding-left: 0; padding-right: 0",
            ["px-1"] = "padding-left: 0.25rem; padding-right: 0.25rem",
            ["px-2"] = "padding-left: 0.5rem; padding-right: 0.5rem",
            ["px-4"] = "padding-left: 1rem; padding-right: 1rem",
            ["px-6"] = "padding-left: 1.5rem; padding-right: 1.5rem",
            ["py-0"] = "padding-top: 0; padding-bottom: 0",
            ["py-1"] = "padding-top: 0.25rem; padding-bottom: 0.25rem",
            ["py-2"] = "padding-top: 0.5rem; padding-bottom: 0.5rem",
            ["py-4"] = "padding-top: 1rem; padding-bottom: 1rem",
            ["m-0"] = "margin: 0", ["m-1"] = "margin: 0.25rem", ["m-2"] = "margin: 0.5rem",
            ["m-4"] = "margin: 1rem", ["m-auto"] = "margin: auto",
            ["mx-auto"] = "margin-left: auto; margin-right: auto",
            ["my-0"] = "margin-top: 0; margin-bottom: 0",
            ["my-1"] = "margin-top: 0.25rem; margin-bottom: 0.25rem",
            ["mt-0"] = "margin-top: 0", ["mt-1"] = "margin-top: 0.25rem",
            ["mt-2"] = "margin-top: 0.5rem", ["mt-4"] = "margin-top: 1rem",
            ["mb-0"] = "margin-bottom: 0", ["mb-1"] = "margin-bottom: 0.25rem",
            ["mb-2"] = "margin-bottom: 0.5rem", ["mb-4"] = "margin-bottom: 1rem",
            ["ml-0"] = "margin-left: 0", ["ml-auto"] = "margin-left: auto",
            ["mr-0"] = "margin-right: 0", ["mr-auto"] = "margin-right: auto",
            ["gap-0"] = "gap: 0", ["gap-1"] = "gap: 0.25rem", ["gap-2"] = "gap: 0.5rem",
            ["gap-4"] = "gap: 1rem", ["gap-6"] = "gap: 1.5rem", ["gap-8"] = "gap: 2rem"
        };

        if (spacingMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion

    #region Sizing

    private bool try_resolve_sizing(string name, out string? rule)
    {
        rule = null;

        var widthPattern = new Regex(@"^w-(\d+)$");
        var wMatch = widthPattern.Match(name);
        if (wMatch.Success)
        {
            var val = int.Parse(wMatch.Groups[1].Value);
            rule = val switch
            {
                0 => "width: 0",
                _ when val % 4 == 0 => $"width: {val * 100 / 4 / 100.0:0}%",
                _ => $"width: {val * 0.25}rem"
            };
            return true;
        }

        var sizingMap = new Dictionary<string, string>
        {
            ["w-full"] = "width: 100%", ["w-screen"] = "width: 100vw",
            ["w-auto"] = "width: auto", ["w-1/2"] = "width: 50%",
            ["w-1/3"] = "width: 33.333333%", ["w-2/3"] = "width: 66.666667%",
            ["w-1/4"] = "width: 25%", ["w-3/4"] = "width: 75%",
            ["h-full"] = "height: 100%", ["h-screen"] = "height: 100vh",
            ["h-auto"] = "height: auto", ["h-0"] = "height: 0",
            ["min-h-screen"] = "min-height: 100vh", ["min-h-0"] = "min-height: 0",
            ["max-w-full"] = "max-width: 100%", ["max-w-screen-sm"] = "max-width: 640px",
            ["max-w-screen-md"] = "max-width: 768px", ["max-w-screen-lg"] = "max-width: 1024px",
            ["max-w-screen-xl"] = "max-width: 1280px"
        };

        if (sizingMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion

    #region Color

    private bool try_resolve_color(string name, out string? rule)
    {
        rule = null;

        var colorMap = new Dictionary<string, string>
        {
            ["bg-transparent"] = "background-color: transparent",
            ["bg-white"] = "background-color: #fff", ["bg-black"] = "background-color: #000",
            ["bg-gray-50"] = "background-color: #f9fafb", ["bg-gray-100"] = "background-color: #f3f4f6",
            ["bg-gray-200"] = "background-color: #e5e7eb", ["bg-gray-300"] = "background-color: #d1d5db",
            ["bg-gray-400"] = "background-color: #9ca3af", ["bg-gray-500"] = "background-color: #6b7280",
            ["bg-gray-600"] = "background-color: #4b5563", ["bg-gray-700"] = "background-color: #374151",
            ["bg-gray-800"] = "background-color: #1f2937", ["bg-gray-900"] = "background-color: #111827",
            ["bg-red-500"] = "background-color: #ef4444", ["bg-red-600"] = "background-color: #dc2626",
            ["bg-blue-500"] = "background-color: #3b82f6", ["bg-blue-600"] = "background-color: #2563eb",
            ["bg-green-500"] = "background-color: #22c55e", ["bg-green-600"] = "background-color: #16a34a",
            ["bg-purple-500"] = "background-color: #a855f7", ["bg-purple-600"] = "background-color: #9333ea",
            ["text-transparent"] = "color: transparent",
            ["text-white"] = "color: #fff", ["text-black"] = "color: #000",
            ["text-gray-50"] = "color: #f9fafb", ["text-gray-100"] = "color: #f3f4f6",
            ["text-gray-200"] = "color: #e5e7eb", ["text-gray-300"] = "color: #d1d5db",
            ["text-gray-400"] = "color: #9ca3af", ["text-gray-500"] = "color: #6b7280",
            ["text-gray-600"] = "color: #4b5563", ["text-gray-700"] = "color: #374151",
            ["text-gray-800"] = "color: #1f2937", ["text-gray-900"] = "color: #111827",
            ["text-red-500"] = "color: #ef4444", ["text-blue-500"] = "color: #3b82f6",
            ["text-green-500"] = "color: #22c55e", ["text-purple-500"] = "color: #a855f7"
        };

        if (colorMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion

    #region Typography

    private bool try_resolve_typography(string name, out string? rule)
    {
        rule = null;

        var typoMap = new Dictionary<string, string>
        {
            ["text-xs"] = "font-size: 0.75rem; line-height: 1rem",
            ["text-sm"] = "font-size: 0.875rem; line-height: 1.25rem",
            ["text-base"] = "font-size: 1rem; line-height: 1.5rem",
            ["text-lg"] = "font-size: 1.125rem; line-height: 1.75rem",
            ["text-xl"] = "font-size: 1.25rem; line-height: 1.75rem",
            ["text-2xl"] = "font-size: 1.5rem; line-height: 2rem",
            ["text-3xl"] = "font-size: 1.875rem; line-height: 2.25rem",
            ["text-4xl"] = "font-size: 2.25rem; line-height: 2.5rem",
            ["font-thin"] = "font-weight: 100", ["font-extralight"] = "font-weight: 200",
            ["font-light"] = "font-weight: 300", ["font-normal"] = "font-weight: 400",
            ["font-medium"] = "font-weight: 500", ["font-semibold"] = "font-weight: 600",
            ["font-bold"] = "font-weight: 700", ["font-extrabold"] = "font-weight: 800",
            ["font-mono"] = "font-family: ui-monospace, SFMono-Regular, Menlo, monospace",
            ["font-sans"] = "font-family: ui-sans-serif, system-ui, sans-serif",
            ["font-serif"] = "font-family: ui-serif, Georgia, serif",
            ["text-left"] = "text-align: left", ["text-center"] = "text-align: center",
            ["text-right"] = "text-align: right", ["text-justify"] = "text-align: justify",
            ["leading-none"] = "line-height: 1", ["leading-tight"] = "line-height: 1.25",
            ["leading-snug"] = "line-height: 1.375", ["leading-normal"] = "line-height: 1.5",
            ["leading-relaxed"] = "line-height: 1.625", ["leading-loose"] = "line-height: 2",
            ["tracking-tight"] = "letter-spacing: -0.025em",
            ["tracking-normal"] = "letter-spacing: 0em",
            ["tracking-wide"] = "letter-spacing: 0.025em",
            ["uppercase"] = "text-transform: uppercase", ["lowercase"] = "text-transform: lowercase",
            ["capitalize"] = "text-transform: capitalize", ["normal-case"] = "text-transform: none",
            ["underline"] = "text-decoration-line: underline",
            ["line-through"] = "text-decoration-line: line-through",
            ["no-underline"] = "text-decoration-line: none",
            ["truncate"] = "overflow: hidden; text-overflow: ellipsis; white-space: nowrap"
        };

        if (typoMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion

    #region Flexbox

    private bool try_resolve_flexbox(string name, out string? rule)
    {
        rule = null;

        var flexMap = new Dictionary<string, string>
        {
            ["flex"] = "display: flex", ["inline-flex"] = "display: inline-flex",
            ["flex-row"] = "flex-direction: row", ["flex-col"] = "flex-direction: column",
            ["flex-row-reverse"] = "flex-direction: row-reverse",
            ["flex-col-reverse"] = "flex-direction: column-reverse",
            ["flex-wrap"] = "flex-wrap: wrap", ["flex-nowrap"] = "flex-wrap: nowrap",
            ["flex-1"] = "flex: 1 1 0%", ["flex-auto"] = "flex: 1 1 auto",
            ["flex-initial"] = "flex: 0 1 auto", ["flex-none"] = "flex: none",
            ["grow"] = "flex-grow: 1", ["grow-0"] = "flex-grow: 0",
            ["shrink"] = "flex-shrink: 1", ["shrink-0"] = "flex-shrink: 0",
            ["justify-start"] = "justify-content: flex-start",
            ["justify-end"] = "justify-content: flex-end",
            ["justify-center"] = "justify-content: center",
            ["justify-between"] = "justify-content: space-between",
            ["justify-around"] = "justify-content: space-around",
            ["justify-evenly"] = "justify-content: space-evenly",
            ["items-start"] = "align-items: flex-start", ["items-end"] = "align-items: flex-end",
            ["items-center"] = "align-items: center", ["items-baseline"] = "align-items: baseline",
            ["items-stretch"] = "align-items: stretch",
            ["self-start"] = "align-self: flex-start", ["self-end"] = "align-self: flex-end",
            ["self-center"] = "align-self: center", ["self-stretch"] = "align-self: stretch",
            ["order-first"] = "order: -9999", ["order-last"] = "order: 9999",
            ["order-none"] = "order: 0"
        };

        if (flexMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion

    #region Grid

    private bool try_resolve_grid(string name, out string? rule)
    {
        rule = null;

        var gridMap = new Dictionary<string, string>
        {
            ["grid"] = "display: grid", ["inline-grid"] = "display: inline-grid",
            ["grid-cols-1"] = "grid-template-columns: repeat(1, minmax(0, 1fr))",
            ["grid-cols-2"] = "grid-template-columns: repeat(2, minmax(0, 1fr))",
            ["grid-cols-3"] = "grid-template-columns: repeat(3, minmax(0, 1fr))",
            ["grid-cols-4"] = "grid-template-columns: repeat(4, minmax(0, 1fr))",
            ["grid-cols-6"] = "grid-template-columns: repeat(6, minmax(0, 1fr))",
            ["grid-cols-12"] = "grid-template-columns: repeat(12, minmax(0, 1fr))",
            ["col-span-1"] = "grid-column: span 1 / span 1",
            ["col-span-2"] = "grid-column: span 2 / span 2",
            ["col-span-3"] = "grid-column: span 3 / span 3",
            ["col-span-full"] = "grid-column: 1 / -1"
        };

        if (gridMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion

    #region Border

    private bool try_resolve_border(string name, out string? rule)
    {
        rule = null;

        var borderMap = new Dictionary<string, string>
        {
            ["border"] = "border-width: 1px",
            ["border-0"] = "border-width: 0",
            ["border-t"] = "border-top-width: 1px", ["border-b"] = "border-bottom-width: 1px",
            ["border-l"] = "border-left-width: 1px", ["border-r"] = "border-right-width: 1px",
            ["rounded-none"] = "border-radius: 0", ["rounded-sm"] = "border-radius: 0.125rem",
            ["rounded"] = "border-radius: 0.25rem", ["rounded-md"] = "border-radius: 0.375rem",
            ["rounded-lg"] = "border-radius: 0.5rem", ["rounded-xl"] = "border-radius: 0.75rem",
            ["rounded-2xl"] = "border-radius: 1rem", ["rounded-full"] = "border-radius: 9999px",
            ["border-solid"] = "border-style: solid", ["border-dashed"] = "border-style: dashed",
            ["border-transparent"] = "border-color: transparent",
            ["border-gray-200"] = "border-color: #e5e7eb",
            ["border-gray-300"] = "border-color: #d1d5db",
            ["border-gray-700"] = "border-color: #374151",
            ["border-purple-500"] = "border-color: #a855f7"
        };

        if (borderMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion

    #region Display

    private bool try_resolve_display(string name, out string? rule)
    {
        rule = null;

        var displayMap = new Dictionary<string, string>
        {
            ["block"] = "display: block", ["inline-block"] = "display: inline-block",
            ["inline"] = "display: inline", ["hidden"] = "display: none",
            ["table"] = "display: table", ["table-row"] = "display: table-row",
            ["table-cell"] = "display: table-cell"
        };

        if (displayMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion

    #region Position

    private bool try_resolve_position(string name, out string? rule)
    {
        rule = null;

        var posMap = new Dictionary<string, string>
        {
            ["static"] = "position: static", ["fixed"] = "position: fixed",
            ["absolute"] = "position: absolute", ["relative"] = "position: relative",
            ["sticky"] = "position: sticky",
            ["inset-0"] = "top: 0; right: 0; bottom: 0; left: 0",
            ["top-0"] = "top: 0", ["right-0"] = "right: 0",
            ["bottom-0"] = "bottom: 0", ["left-0"] = "left: 0",
            ["z-0"] = "z-index: 0", ["z-10"] = "z-index: 10",
            ["z-20"] = "z-index: 20", ["z-50"] = "z-index: 50"
        };

        if (posMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion

    #region Overflow

    private bool try_resolve_overflow(string name, out string? rule)
    {
        rule = null;

        var overflowMap = new Dictionary<string, string>
        {
            ["overflow-auto"] = "overflow: auto", ["overflow-hidden"] = "overflow: hidden",
            ["overflow-visible"] = "overflow: visible", ["overflow-scroll"] = "overflow: scroll",
            ["overflow-x-auto"] = "overflow-x: auto", ["overflow-y-auto"] = "overflow-y: auto",
            ["overflow-x-hidden"] = "overflow-x: hidden", ["overflow-y-hidden"] = "overflow-y: hidden"
        };

        if (overflowMap.TryGetValue(name, out var value))
        {
            rule = value;
            return true;
        }

        return false;
    }

    #endregion
}