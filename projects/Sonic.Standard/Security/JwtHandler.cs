using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Std.Security;

/// <summary>
///     JWT 令牌表示�?///
/// </summary>
public sealed class JwtToken
{
    /// <summary>
    ///     主体标识（sub）�?    ///
    /// </summary>
    public string? subject { get; set; }

    /// <summary>
    ///     用户名称�?    ///
    /// </summary>
    public string? name { get; set; }

    /// <summary>
    ///     角色列表�?    ///
    /// </summary>
    public IReadOnlyList<string> roles { get; set; } = [];

    /// <summary>
    ///     扩展声明�?    ///
    /// </summary>
    public IReadOnlyDictionary<string, object> claims { get; set; } = new Dictionary<string, object>();

    /// <summary>
    ///     签发者（iss）�?    ///
    /// </summary>
    public string? issuer { get; set; }

    /// <summary>
    ///     接收者（aud）�?    ///
    /// </summary>
    public string? audience { get; set; }

    /// <summary>
    ///     签发时间（iat）�?    ///
    /// </summary>
    public DateTimeOffset? issued_at { get; set; }

    /// <summary>
    ///     过期时间（exp）�?    ///
    /// </summary>
    public DateTimeOffset? expiration { get; set; }

    /// <summary>
    ///     不早于时间（nbf）�?    ///
    /// </summary>
    public DateTimeOffset? not_before { get; set; }

    /// <summary>
    ///     令牌是否未过期�?    ///
    /// </summary>
    public bool is_expired => expiration.HasValue && DateTimeOffset.UtcNow > expiration.Value;

    /// <summary>
    ///     令牌是否尚未生效�?    ///
    /// </summary>
    public bool is_not_yet_valid => not_before.HasValue && DateTimeOffset.UtcNow < not_before.Value;
}

/// <summary>
///     JWT 验证选项�?///
/// </summary>
public sealed class JwtValidationOptions
{
    /// <summary>
    ///     签名密钥（至�?16 字符，用�?HMAC-SHA256）�?    ///
    /// </summary>
    public string secret_key { get; set; } = "sonic-default-secret-key-min-16";

    /// <summary>
    ///     是否验证签名�?    ///
    /// </summary>
    public bool validate_signature { get; set; } = true;

    /// <summary>
    ///     是否验证过期时间�?    ///
    /// </summary>
    public bool validate_lifetime { get; set; } = true;

    /// <summary>
    ///     是否验证签发者�?    ///
    /// </summary>
    public bool validate_issuer { get; set; } = true;

    /// <summary>
    ///     有效的签发者�?    ///
    /// </summary>
    public string valid_issuer { get; set; } = "sonic";

    /// <summary>
    ///     是否验证接收者�?    ///
    /// </summary>
    public bool validate_audience { get; set; }

    /// <summary>
    ///     有效的接收者�?    ///
    /// </summary>
    public string valid_audience { get; set; } = string.Empty;

    /// <summary>
    ///     JWT �?Authorization 头中�?Scheme�?    ///
    /// </summary>
    public string authentication_scheme { get; set; } = "Bearer";

    /// <summary>
    ///     时钟偏移量（秒），用于容忍时钟不同步�?    ///
    /// </summary>
    public int clock_skew_seconds { get; set; } = 300;
}

/// <summary>
///     JWT 解析和验证器。不依赖外部 NuGet 包，使用 System.Security.Cryptography�?///
/// </summary>
public sealed class JwtHandler
{
    private readonly JwtValidationOptions _options;

    /// <summary>
    ///     初始�?JWT 处理器�?    ///
    /// </summary>
    /// <param name="options">JWT 验证选项</param>
    public JwtHandler(JwtValidationOptions? options = null)
    {
        _options = options ?? new JwtValidationOptions();
    }

    /// <summary>
    ///     �?JWT 字符串中解析并验证令牌�?    ///
    /// </summary>
    /// <param name="token">
    ///     JWT 令牌字符�?/param>
    ///     <returns>解析后的令牌，验证失败返�?null</returns>
    public JwtToken? parse_and_validate(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            var headerJson = base64_url_decode(parts[0]);
            var payloadJson = base64_url_decode(parts[1]);
            var signature = parts[2];

            if (_options.validate_signature)
            {
                var computedSignature = compute_signature(parts[0], parts[1]);
                if (!constant_time_equals(signature, computedSignature)) return null;
            }

            var payload = JsonSerializer.Deserialize<JwtPayload>(payloadJson);
            if (payload == null) return null;

            var jwtToken = new JwtToken
            {
                subject = payload.sub,
                name = payload.name,
                roles = payload.role switch
                {
                    string single => [single],
                    JsonElement { ValueKind: JsonValueKind.Array } arr =>
                        arr.EnumerateArray().Select(e => e.GetString()!).ToList(),
                    _ => []
                },
                issuer = payload.iss,
                audience = payload.aud,
                issued_at = unix_time_to_date_time(payload.iat),
                expiration = unix_time_to_date_time(payload.exp),
                not_before = unix_time_to_date_time(payload.nbf),
                claims = extract_extra_claims(payload)
            };

            if (_options.validate_lifetime)
            {
                var now = DateTimeOffset.UtcNow;
                if (jwtToken.expiration.HasValue
                    && now > jwtToken.expiration.Value.Add(TimeSpan.FromSeconds(_options.clock_skew_seconds)))
                    return null;

                if (jwtToken.not_before.HasValue
                    && now < jwtToken.not_before.Value.Add(TimeSpan.FromSeconds(-_options.clock_skew_seconds)))
                    return null;
            }

            if (_options.validate_issuer
                && !string.Equals(jwtToken.issuer, _options.valid_issuer, StringComparison.OrdinalIgnoreCase))
                return null;

            if (_options.validate_audience
                && !string.Equals(jwtToken.audience, _options.valid_audience, StringComparison.OrdinalIgnoreCase))
                return null;

            return jwtToken;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     �?Authorization 头值中提取并验�?JWT 令牌�?    ///
    /// </summary>
    /// <param name="authorizationHeader">
    ///     Authorization 头的�?/param>
    ///     <returns>解析后的令牌，验证失败返�?null</returns>
    public JwtToken? parse_from_header(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader)) return null;

        var scheme = $"{_options.authentication_scheme} ";
        if (!authorizationHeader.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)) return null;

        var token = authorizationHeader[scheme.Length..].Trim();
        return parse_and_validate(token);
    }

    /// <summary>
    ///     生成一�?JWT 令牌（用于测试和颁发场景）�?    ///
    /// </summary>
    /// <param name="subject">主体标识</param>
    /// <param name="name">用户名称</param>
    /// <param name="roles">角色列表</param>
    /// <param name="expiryMinutes">过期时间（分钟）</param>
    /// <param name="extraClaims">额外声明</param>
    /// <returns>JWT 令牌字符�?/returns>
    public string generate_token(
        string subject,
        string? name = null,
        IReadOnlyList<string>? roles = null,
        int expiryMinutes = 60,
        IReadOnlyDictionary<string, object>? extraClaims = null)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object>
        {
            ["sub"] = subject,
            ["iat"] = date_time_to_unix_time(now),
            ["exp"] = date_time_to_unix_time(now.AddMinutes(expiryMinutes)),
            ["iss"] = _options.valid_issuer
        };

        if (name != null) payload["name"] = name;

        if (roles is { Count: > 0 }) payload["role"] = roles;

        if (extraClaims != null)
            foreach (var (key, value) in extraClaims)
                payload[key] = value;

        var payloadJson = JsonSerializer.Serialize(payload);
        var headerJson = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";

        var headerBase64 = base64_url_encode(headerJson);
        var payloadBase64 = base64_url_encode(payloadJson);
        var signature = compute_signature(headerBase64, payloadBase64);

        return $"{headerBase64}.{payloadBase64}.{signature}";
    }

    private string compute_signature(string headerBase64, string payloadBase64)
    {
        var data = $"{headerBase64}.{payloadBase64}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.secret_key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return base64_url_encode(Convert.ToBase64String(hash));
    }

    private static bool constant_time_equals(string a, string b)
    {
        if (a.Length != b.Length) return false;

        var result = 0;
        for (var i = 0; i < a.Length; i++) result |= a[i] ^ b[i];

        return result == 0;
    }

    private static string base64_url_encode(string base64)
    {
        var bytes = Encoding.UTF8.GetBytes(base64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string base64_url_decode(string base64Url)
    {
        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private static DateTimeOffset? unix_time_to_date_time(long? unixTime)
    {
        return unixTime.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(unixTime.Value)
            : null;
    }

    private static long date_time_to_unix_time(DateTimeOffset dt)
    {
        return dt.ToUnixTimeSeconds();
    }

    private static IReadOnlyDictionary<string, object> extract_extra_claims(JwtPayload payload)
    {
        var known = new HashSet<string> { "sub", "name", "role", "iss", "aud", "iat", "exp", "nbf" };
        var claims = new Dictionary<string, object>();

        foreach (var prop in payload.extra_properties)
            if (!known.Contains(prop.Key))
                claims[prop.Key] = prop.Value;

        return claims;
    }

    private sealed class JwtPayload
    {
        [JsonPropertyName("sub")] public string? sub { get; set; }

        [JsonPropertyName("name")] public string? name { get; set; }

        [JsonPropertyName("role")] public object? role { get; set; }

        [JsonPropertyName("iss")] public string? iss { get; set; }

        [JsonPropertyName("aud")] public string? aud { get; set; }

        [JsonPropertyName("iat")] public long? iat { get; set; }

        [JsonPropertyName("exp")] public long? exp { get; set; }

        [JsonPropertyName("nbf")] public long? nbf { get; set; }

        [JsonExtensionData] public Dictionary<string, JsonElement> extra_properties { get; set; } = [];
    }
}