using System.Text;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.PackageManager.Tools;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Auth;

/// <summary>
///     Vendor 璁よ瘉浠ょ墝瀛樺偍锛岀鐞嗗悇娉ㄥ唽琛ㄧ殑鐧诲綍鐘舵€佸拰浠ょ墝鎸佷箙鍖?///
/// </summary>
public class VendorAuthStore
{
    private readonly string _auth_file_path;
    private readonly Dictionary<string, VendorAuthInfo> _auth_infos = new(StringComparer.OrdinalIgnoreCase);
    private readonly SerdeParser _parse;

    /// <summary>
    ///     鍒涘缓璁よ瘉瀛樺偍瀹炰緥
    /// </summary>
    /// <param name="configDirectory">
    ///     閰嶇疆鐩綍锛堥€氬父涓?~/.valkyrie/锛?/param>
    ///     <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public VendorAuthStore(string configDirectory, SerdeParser parse)
    {
        _auth_file_path = Path.Combine(configDirectory, "auth.von");
        _parse = parse;
    }

    /// <summary>
    ///     鑾峰彇鎵€鏈夎璇佷俊鎭?    ///
    /// </summary>
    public IReadOnlyDictionary<string, VendorAuthInfo> all => _auth_infos;

    /// <summary>
    ///     浠庢枃浠跺姞杞借璇佷俊鎭?    ///
    /// </summary>
    public void load()
    {
        if (!File.Exists(_auth_file_path)) return;

        try
        {
            var content = File.ReadAllText(_auth_file_path);

            if (string.IsNullOrWhiteSpace(content)) return;

            var value = _parse(content);

            if (value is { type: SerdeValueType.@object, fields: not null })
                foreach (var field in value.fields)
                {
                    var vendorName = field.Key;
                    var info = parse_auth_info(field.Value);

                    if (info is not null)
                    {
                        info.vendor_name = vendorName;
                        _auth_infos[vendorName] = info;
                    }
                }
        }
        catch
        {
        }
    }

    /// <summary>
    ///     淇濆瓨璁よ瘉淇℃伅鍒版枃浠?    ///
    /// </summary>
    public async Task save()
    {
        var dir = Path.GetDirectoryName(_auth_file_path);

        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var vendorFields = new Dictionary<string, SerdeValue>();

        foreach (var (name, info) in _auth_infos)
            if (info.is_logged_in)
                vendorFields[name] = serialize_auth_info(info);

        var root = SerdeValue.@object(vendorFields);
        var content = VonFormatter.format(root);
        await File.WriteAllTextAsync(_auth_file_path, content);
    }

    /// <summary>
    ///     淇濆瓨 Vendor 璁よ瘉浠ょ墝
    /// </summary>
    /// <param name="vendorName">Vendor 鍚嶇О</param>
    /// <param name="endpoint">
    ///     娉ㄥ唽琛ㄧ鐐?/param>
    ///     <param name="token">璁よ瘉浠ょ墝</param>
    ///     <param name="currentUser">
    ///         褰撳墠鐢ㄦ埛鍚?/param>
    ///         <param name="expiresAt">杩囨湡鏃堕棿</param>
    public void save_token(string vendorName, string endpoint, string token, string? currentUser = null,
        DateTime? expiresAt = null)
    {
        _auth_infos[vendorName] = new VendorAuthInfo
        {
            vendor_name = vendorName,
            endpoint = endpoint,
            token = token,
            current_user = currentUser,
            logged_in_at = DateTime.UtcNow,
            expires_at = expiresAt
        };
    }

    /// <summary>
    ///     鑾峰彇 Vendor 鐨勮璇佷护鐗?    ///
    /// </summary>
    /// <param name="vendorName">Vendor 鍚嶇О</param>
    /// <returns>璁よ瘉浠ょ墝锛屾湭鐧诲綍鎴栧凡杩囨湡杩斿洖 null</returns>
    public string? get_token(string vendorName)
    {
        if (_auth_infos.TryGetValue(vendorName, out var info) && info.is_logged_in) return info.token;

        return null;
    }

    /// <summary>
    ///     鑾峰彇 Vendor 璁よ瘉淇℃伅
    /// </summary>
    /// <param name="vendorName">Vendor 鍚嶇О</param>
    /// <returns>璁よ瘉淇℃伅锛屾湭鎵惧埌杩斿洖 null</returns>
    public VendorAuthInfo? get_auth_info(string vendorName)
    {
        return _auth_infos.GetValueOrDefault(vendorName);
    }

    /// <summary>
    ///     绉婚櫎 Vendor 璁よ瘉浠ょ墝锛堢櫥鍑猴級
    /// </summary>
    /// <param name="vendorName">Vendor 鍚嶇О</param>
    /// <returns>鏄惁鎴愬姛绉婚櫎</returns>
    public bool remove_token(string vendorName)
    {
        return _auth_infos.Remove(vendorName);
    }

    /// <summary>
    ///     妫€鏌ユ槸鍚﹀凡鐧诲綍鎸囧畾 Vendor
    /// </summary>
    /// <param name="vendorName">Vendor 鍚嶇О</param>
    /// <returns>鏄惁宸茬櫥褰曚笖浠ょ墝鏈繃鏈?/returns>
    public bool is_logged_in(string vendorName)
    {
        return _auth_infos.TryGetValue(vendorName, out var info) && info.is_logged_in;
    }

    /// <summary>
    ///     鐢熸垚浠ょ墝鐨勬贩娣嗗瓨鍌ㄥ瓧绗︿覆锛堝熀纭€淇濇姢锛?    ///
    /// </summary>
    /// <param name="token">鍘熷浠ょ墝</param>
    /// <returns>娣锋穯鍚庣殑浠ょ墝</returns>
    public static string obfuscate_token(string token)
    {
        if (string.IsNullOrEmpty(token)) return token;

        var bytes = Encoding.UTF8.GetBytes(token);

        for (var i = 0; i < bytes.Length; i++) bytes[i] ^= 0x55;

        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    ///     鍙嶆贩娣嗕护鐗?    ///
    /// </summary>
    /// <param name="obfuscated">娣锋穯鍚庣殑浠ょ墝</param>
    /// <returns>鍘熷浠ょ墝</returns>
    public static string deobfuscate_token(string obfuscated)
    {
        if (string.IsNullOrEmpty(obfuscated)) return obfuscated;

        try
        {
            var bytes = Convert.FromBase64String(obfuscated);

            for (var i = 0; i < bytes.Length; i++) bytes[i] ^= 0x55;

            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return obfuscated;
        }
    }

    private static VendorAuthInfo? parse_auth_info(SerdeValue value)
    {
        if (value.type != SerdeValueType.@object || value.fields is null) return null;

        var info = new VendorAuthInfo();

        info.endpoint = ValkyrieValueProjector.bind_utf8_field(value, "endpoint") ?? string.Empty;

        if (ValkyrieValueProjector.bind_utf8_field(value, "token") is { } token)
        {
            info.token = deobfuscate_token(token);
        }

        info.current_user = ValkyrieValueProjector.bind_utf8_field(value, "user");

        if (ValkyrieValueProjector.bind_utf8_field(value, "loggedInAt") is { } loginAt
            && DateTime.TryParse(loginAt, out var loginTime))
            info.logged_in_at = loginTime;

        if (ValkyrieValueProjector.bind_utf8_field(value, "expiresAt") is { } expiryAt
            && DateTime.TryParse(expiryAt, out var expiryTime))
            info.expires_at = expiryTime;

        return info;
    }

    private static SerdeValue serialize_auth_info(VendorAuthInfo info)
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["endpoint"] = SerdeValue.@string(info.endpoint),
            ["token"] = SerdeValue.@string(obfuscate_token(info.token)),
            ["loggedInAt"] = SerdeValue.@string(info.logged_in_at.ToString("O"))
        };

        if (info.current_user is not null) fields["user"] = SerdeValue.@string(info.current_user);

        if (info.expires_at.HasValue) fields["expiresAt"] = SerdeValue.@string(info.expires_at.Value.ToString("O"));

        return SerdeValue.@object(fields);
    }
}
