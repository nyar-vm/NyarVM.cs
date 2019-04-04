using System.Text;

namespace Std.Net.Middleware;

/// <summary>
///     Cookie 解析辅助类，提供从请求头解析 Cookie 和构造响应 Cookie 的能力。
/// </summary>
public static class CookieParser
{
    /// <summary>
    ///     从请求头解析 Cookie 字符串为键值对字典。
    /// </summary>
    /// <param name="cookieHeader">Cookie 请求头的值</param>
    /// <returns>Cookie 键值对字典</returns>
    public static Dictionary<string, string> parse(string? cookieHeader)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(cookieHeader)) return result;

        var parts = cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            var key = kv[0].Trim();
            var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1].Trim()) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    /// <summary>
    ///     构造 Set-Cookie 响应头值。
    /// </summary>
    /// <param name="name">Cookie 名称</param>
    /// <param name="value">Cookie 值</param>
    /// <param name="options">Cookie 选项</param>
    /// <returns>完整的 Set-Cookie 头值</returns>
    public static string build_set_cookie(string name, string value, CookieOptions? options = null)
    {
        options ??= new CookieOptions();

        var sb = new StringBuilder();
        sb.Append(Uri.EscapeDataString(name));
        sb.Append('=');
        sb.Append(Uri.EscapeDataString(value));

        if (options.expires.HasValue)
        {
            sb.Append("; Expires=");
            sb.Append(options.expires.Value.ToString("R"));
        }

        if (options.max_age.HasValue)
        {
            sb.Append("; Max-Age=");
            sb.Append((int)options.max_age.Value.TotalSeconds);
        }

        sb.Append("; Path=");
        sb.Append(options.path ?? "/");

        if (options.domain is not null)
        {
            sb.Append("; Domain=");
            sb.Append(options.domain);
        }

        if (options.secure) sb.Append("; Secure");

        if (options.http_only) sb.Append("; HttpOnly");

        if (options.same_site != SameSiteMode.none)
        {
            sb.Append("; SameSite=");
            sb.Append(options.same_site.ToString());
        }

        return sb.ToString();
    }
}

/// <summary>
///     Cookie 选项。
/// </summary>
public sealed class CookieOptions
{
    /// <summary>
    ///     过期时间（UTC）。
    /// </summary>
    public DateTimeOffset? expires { get; set; }

    /// <summary>
    ///     最大生存时间（优先级高于 Expires）。
    /// </summary>
    public TimeSpan? max_age { get; set; }

    /// <summary>
    ///     Cookie 有效路径（默认 "/"）。
    /// </summary>
    public string? path { get; set; } = "/";

    /// <summary>
    ///     Cookie 有效域名。
    /// </summary>
    public string? domain { get; set; }

    /// <summary>
    ///     是否仅通过 HTTPS 发送。
    /// </summary>
    public bool secure { get; set; }

    /// <summary>
    ///     是否禁止 JavaScript 访问（HttpOnly）。
    /// </summary>
    public bool http_only { get; set; }

    /// <summary>
    ///     SameSite 策略。
    /// </summary>
    public SameSiteMode same_site { get; set; } = SameSiteMode.lax;
}

/// <summary>
///     SameSite 策略枚举。
/// </summary>
public enum SameSiteMode
{
    /// <summary>
    ///     不设置 SameSite 属性
    /// </summary>
    none,

    /// <summary>
    ///     仅在同一站点请求时发送
    /// </summary>
    lax,

    /// <summary>
    ///     仅在同站上下文请求时发送
    /// </summary>
    strict
}