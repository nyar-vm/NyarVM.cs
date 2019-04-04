using Nyar.Language.Valkyrie.Semantic;
using Nyar.PackageManager.Tools;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Sdk;

/// <summary>
///     骞冲彴 SDK 鍖呰鑼?鈥?瀹氫箟 SDK 鍖呯殑鍏ュ彛鐐广€丗FI 缁戝畾澹版槑鍜屽钩鍙拌姹?///
/// </summary>
public class PlatformSdk
{
    private readonly string _file_path;
    private readonly SerdeParser _parse;

    /// <summary>
    ///     鍒涘缓 PlatformSdk 瀹炰緥
    /// </summary>
    /// <param name="directoryPath">椤圭洰鐩綍璺緞</param>
    /// <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public PlatformSdk(string directoryPath, SerdeParser parse)
    {
        _file_path = Path.Combine(directoryPath, "sdk.von");
        _parse = parse;
    }

    /// <summary>
    ///     SDK 鍚嶇О
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     SDK 鐗堟湰
    /// </summary>
    public string version { get; set; } = "0.0.0";

    /// <summary>
    ///     鐩爣骞冲彴锛歸asm / clr / jvm / native
    /// </summary>
    public string platform { get; set; } = "wasm";

    /// <summary>
    ///     SDK 鍏ュ彛鐐规ā鍧楀垪琛?    ///
    /// </summary>
    public List<SdkEntryPoint> entry_points { get; set; } = [];

    /// <summary>
    ///     FFI 缁戝畾澹版槑鍒楄〃
    /// </summary>
    public List<FfiBinding> ffi_bindings { get; set; } = [];

    /// <summary>
    ///     骞冲彴瑕佹眰
    /// </summary>
    public SdkPlatformRequirement platform_requirement { get; set; } = new();

    /// <summary>
    ///     SDK 鎻愪緵鐨勭被鍨嬪０鏄?    ///
    /// </summary>
    public List<SdkTypeDeclaration> type_declarations { get; set; } = [];

    /// <summary>
    ///     浠庣洰褰曞姞杞?sdk.von
    /// </summary>
    /// <param name="directoryPath">椤圭洰鐩綍璺緞</param>
    /// <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public static PlatformSdk load(string directoryPath, SerdeParser parse)
    {
        var sdk = new PlatformSdk(directoryPath, parse);
        sdk.load();
        return sdk;
    }

    /// <summary>
    ///     瑙ｆ瀽 sdk.von 鏂囦欢
    /// </summary>
    public void load()
    {
        if (!File.Exists(_file_path)) return;

        var content = File.ReadAllText(_file_path);
        var root = _parse(content);

        if (root.type != SerdeValueType.@object) return;

        name = ValkyrieValueProjector.bind_utf8_field(root, "name") ?? string.Empty;
        version = ValkyrieValueProjector.bind_utf8_field(root, "version") ?? "0.0.0";
        platform = ValkyrieValueProjector.bind_utf8_field(root, "platform") ?? "wasm";

        load_entry_points(root);
        load_ffi_bindings(root);
        load_platform_requirement(root);
        load_type_declarations(root);
    }

    /// <summary>
    ///     搴忓垪鍖栧苟淇濆瓨鍒?sdk.von
    /// </summary>
    public void save()
    {
        var dir = Path.GetDirectoryName(_file_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var fields = new Dictionary<string, SerdeValue>
        {
            ["name"] = SerdeValue.@string(name),
            ["version"] = SerdeValue.@string(version),
            ["platform"] = SerdeValue.@string(platform)
        };

        save_entry_points(fields);
        save_ffi_bindings(fields);
        save_platform_requirement(fields);
        save_type_declarations(fields);

        var root = SerdeValue.@object(fields);
        File.WriteAllText(_file_path, VonFormatter.format(root));
    }

    /// <summary>
    ///     楠岃瘉 SDK 瑙勮寖瀹屾暣鎬?    ///
    /// </summary>
    public SdkValidationResult validate()
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(name)) errors.Add("SDK 名称不能为空");

        if (string.IsNullOrWhiteSpace(version)) errors.Add("SDK 版本不能为空");

        if (!is_valid_platform(platform)) errors.Add($"不支持的平台：{platform}，应为 wasm / clr / jvm / native");

        if (entry_points.Count == 0) warnings.Add("未定义入口点，SDK 可能无法被正确加载");

        foreach (var entry in entry_points)
            if (string.IsNullOrWhiteSpace(entry.module))
                errors.Add($"入口点 '{entry.name}' 缺少 module 字段");

        foreach (var binding in ffi_bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.name)) errors.Add("FFI 绑定缺少 name 字段");

            if (string.IsNullOrWhiteSpace(binding.signature)) errors.Add($"FFI 绑定 '{binding.name}' 缺少 signature 字段");
        }

        return new SdkValidationResult
        {
            is_valid = errors.Count == 0,
            errors = errors,
            warnings = warnings
        };
    }

    /// <summary>
    ///     妫€鏌?sdk.von 鏄惁瀛樺湪
    /// </summary>
    public bool exists()
    {
        return File.Exists(_file_path);
    }

    #region 绉佹湁鏂规硶

    private static bool is_valid_platform(string platform)
    {
        return platform is "wasm" or "clr" or "jvm" or "native";
    }

    private void load_entry_points(SerdeValue root)
    {
        entry_points.Clear();
        var field = root.get_field("entry_points");
        if (field?.type != SerdeValueType.array || field.elements is null) return;

        foreach (var element in field.elements)
        {
            if (element.type != SerdeValueType.@object || element.fields is null) continue;

            var entry = new SdkEntryPoint
            {
                name = ValkyrieValueProjector.bind_utf8_field(element, "name") ?? string.Empty,
                module = ValkyrieValueProjector.bind_utf8_field(element, "module") ?? string.Empty,
                description = ValkyrieValueProjector.bind_utf8_field(element, "description")
            };

            entry_points.Add(entry);
        }
    }

    private void load_ffi_bindings(SerdeValue root)
    {
        ffi_bindings.Clear();
        var field = root.get_field("ffi_bindings");
        if (field?.type != SerdeValueType.array || field.elements is null) return;

        foreach (var element in field.elements)
        {
            if (element.type != SerdeValueType.@object || element.fields is null) continue;

            var binding = new FfiBinding
            {
                name = ValkyrieValueProjector.bind_utf8_field(element, "name") ?? string.Empty,
                signature = ValkyrieValueProjector.bind_utf8_field(element, "signature") ?? string.Empty,
                library = ValkyrieValueProjector.bind_utf8_field(element, "library"),
                convention = ValkyrieValueProjector.bind_utf8_field(element, "convention") ?? "cdecl"
            };

            ffi_bindings.Add(binding);
        }
    }

    private void load_platform_requirement(SerdeValue root)
    {
        var field = root.get_field("platform_requirement");
        if (field?.type != SerdeValueType.@object || field.fields is null) return;

        platform_requirement = new SdkPlatformRequirement
        {
            min_version = ValkyrieValueProjector.bind_utf8_field(field, "min_version"),
            max_version = ValkyrieValueProjector.bind_utf8_field(field, "max_version"),
            os = ValkyrieValueProjector.bind_utf8_field(field, "os"),
            arch = ValkyrieValueProjector.bind_utf8_field(field, "arch")
        };
    }

    private void load_type_declarations(SerdeValue root)
    {
        type_declarations.Clear();
        var field = root.get_field("types");
        if (field?.type != SerdeValueType.array || field.elements is null) return;

        foreach (var element in field.elements)
        {
            if (element.type != SerdeValueType.@object || element.fields is null) continue;

            var typeDecl = new SdkTypeDeclaration
            {
                name = ValkyrieValueProjector.bind_utf8_field(element, "name") ?? string.Empty,
                kind = ValkyrieValueProjector.bind_utf8_field(element, "kind") ?? "struct",
                description = ValkyrieValueProjector.bind_utf8_field(element, "description")
            };

            type_declarations.Add(typeDecl);
        }
    }

    private void save_entry_points(Dictionary<string, SerdeValue> fields)
    {
        if (entry_points.Count == 0) return;

        var elements = new List<SerdeValue>();
        foreach (var entry in entry_points)
        {
            var entryFields = new Dictionary<string, SerdeValue>
            {
                ["name"] = SerdeValue.@string(entry.name),
                ["module"] = SerdeValue.@string(entry.module)
            };

            if (entry.description is not null) entryFields["description"] = SerdeValue.@string(entry.description);

            elements.Add(SerdeValue.@object(entryFields));
        }

        fields["entry_points"] = SerdeValue.array(elements);
    }

    private void save_ffi_bindings(Dictionary<string, SerdeValue> fields)
    {
        if (ffi_bindings.Count == 0) return;

        var elements = new List<SerdeValue>();
        foreach (var binding in ffi_bindings)
        {
            var bindingFields = new Dictionary<string, SerdeValue>
            {
                ["name"] = SerdeValue.@string(binding.name),
                ["signature"] = SerdeValue.@string(binding.signature),
                ["convention"] = SerdeValue.@string(binding.convention ?? "cdecl")
            };

            if (binding.library is not null) bindingFields["library"] = SerdeValue.@string(binding.library);

            elements.Add(SerdeValue.@object(bindingFields));
        }

        fields["ffi_bindings"] = SerdeValue.array(elements);
    }

    private void save_platform_requirement(Dictionary<string, SerdeValue> fields)
    {
        var reqFields = new Dictionary<string, SerdeValue>();

        if (platform_requirement.min_version is not null)
            reqFields["min_version"] = SerdeValue.@string(platform_requirement.min_version);

        if (platform_requirement.max_version is not null)
            reqFields["max_version"] = SerdeValue.@string(platform_requirement.max_version);

        if (platform_requirement.os is not null) reqFields["os"] = SerdeValue.@string(platform_requirement.os);

        if (platform_requirement.arch is not null) reqFields["arch"] = SerdeValue.@string(platform_requirement.arch);

        if (reqFields.Count > 0) fields["platform_requirement"] = SerdeValue.@object(reqFields);
    }

    private void save_type_declarations(Dictionary<string, SerdeValue> fields)
    {
        if (type_declarations.Count == 0) return;

        var elements = new List<SerdeValue>();
        foreach (var typeDecl in type_declarations)
        {
            var typeFields = new Dictionary<string, SerdeValue>
            {
                ["name"] = SerdeValue.@string(typeDecl.name),
                ["kind"] = SerdeValue.@string(typeDecl.kind ?? "struct")
            };

            if (typeDecl.description is not null) typeFields["description"] = SerdeValue.@string(typeDecl.description);

            elements.Add(SerdeValue.@object(typeFields));
        }

        fields["types"] = SerdeValue.array(elements);
    }

    #endregion
}
