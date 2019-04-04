using System.Text;

namespace Std.Data.Text.Tailwind;

/// <summary>
///     Tailwind Preflight（CSS Reset）生成器
/// </summary>
public sealed class TailwindPreflight
{
    /// <summary>
    ///     生成 Preflight CSS Reset
    /// </summary>
    public static string generate()
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "*, ::before, ::after { box-sizing: border-box; border-width: 0; border-style: solid; border-color: theme('borderColor.DEFAULT', currentColor); }");
        sb.AppendLine("::before, ::after { --tw-content: ''; }");
        sb.AppendLine(
            "html { line-height: 1.5; -webkit-text-size-adjust: 100%; font-family: theme('fontFamily.sans', ui-sans-serif, system-ui, sans-serif); }");
        sb.AppendLine("body { margin: 0; line-height: inherit; }");
        sb.AppendLine("hr { height: 0; color: inherit; border-top-width: 1px; }");
        sb.AppendLine("abbr:where([title]) { text-decoration: underline dotted; }");
        sb.AppendLine("h1, h2, h3, h4, h5, h6 { font-size: inherit; font-weight: inherit; }");
        sb.AppendLine("a { color: inherit; text-decoration: inherit; }");
        sb.AppendLine("b, strong { font-weight: bolder; }");
        sb.AppendLine(
            "code, kbd, samp, pre { font-family: theme('fontFamily.mono', ui-monospace, SFMono-Regular, Menlo, monospace); font-size: 1em; }");
        sb.AppendLine("small { font-size: 80%; }");
        sb.AppendLine("sub, sup { font-size: 75%; line-height: 0; position: relative; vertical-align: baseline; }");
        sb.AppendLine("sub { bottom: -0.25em; }");
        sb.AppendLine("sup { top: -0.5em; }");
        sb.AppendLine("table { text-indent: 0; border-color: inherit; border-collapse: collapse; }");
        sb.AppendLine(
            "button, input, optgroup, select, textarea { font-family: inherit; font-size: 100%; font-weight: inherit; line-height: inherit; color: inherit; margin: 0; padding: 0; }");
        sb.AppendLine("button, select { text-transform: none; }");
        sb.AppendLine(
            "button, [type='button'], [type='reset'], [type='submit'] { -webkit-appearance: button; background-color: transparent; background-image: none; }");
        sb.AppendLine("progress { vertical-align: baseline; }");
        sb.AppendLine("::-webkit-inner-spin-button, ::-webkit-outer-spin-button { height: auto; }");
        sb.AppendLine("[type='search'] { -webkit-appearance: textfield; outline-offset: -2px; }");
        sb.AppendLine("::-webkit-search-decoration { -webkit-appearance: none; }");
        sb.AppendLine("::-webkit-file-upload-button { -webkit-appearance: button; font: inherit; }");
        sb.AppendLine("summary { display: list-item; }");
        sb.AppendLine("blockquote, dl, dd, h1, h2, h3, h4, h5, h6, hr, figure, p, pre { margin: 0; }");
        sb.AppendLine("fieldset { margin: 0; padding: 0; }");
        sb.AppendLine("legend { padding: 0; }");
        sb.AppendLine("ol, ul, menu { list-style: none; margin: 0; padding: 0; }");
        sb.AppendLine("textarea { resize: vertical; }");
        sb.AppendLine(
            "input::placeholder, textarea::placeholder { opacity: 1; color: theme('placeholderColor.DEFAULT', #9ca3af); }");
        sb.AppendLine("button, [role=\"button\"] { cursor: pointer; }");
        sb.AppendLine(":disabled { cursor: default; }");
        sb.AppendLine(
            "img, svg, video, canvas, audio, iframe, embed, object { display: block; vertical-align: middle; }");
        sb.AppendLine("img, video { max-width: 100%; height: auto; }");
        return sb.ToString();
    }
}