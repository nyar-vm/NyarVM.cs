using System.Globalization;
using System.Text;
using Core.Data;
using Std.Config;
using Std.Config.Node;
using Std.Data.Text.Diagnostics;
using Std.DataProcess.Serialize;

namespace Valhalla.Config;

/// <summary>
///     瓦尓哈拉服务端配置模型
/// </summary>
[Data]
public class ValhallaConfig
{
    #region 基础信息

    /// <summary>实例名称</summary>
    public string name { get; set; } = "瓦尓哈拉";

    /// <summary>实例描述</summary>
    public string description { get; set; } = string.Empty;

    #endregion

    #region 可见性与注册

    /// <summary>是否为公开实例</summary>
    [Key("public")]
    public bool @public { get; set; } = true;

    /// <summary>注册模式</summary>
    public RegistrationMode registration { get; set; } = RegistrationMode.open;

    /// <summary>邀请码（当注册模式为 Invite 时使用）</summary>
    [Key("inviteCode")]
    public string? invite_code { get; set; }

    #endregion

    #region 安全策略

    /// <summary>PURGE 后的冷却期天数</summary>
    [Key("coolingPeriodDays")]
    public int cooling_period_days { get; set; } = 14;

    /// <summary>是否要求管理员审批重注册</summary>
    [Key("requireAdminForReRegistration")]
    public bool require_admin_for_re_registration { get; set; } = true;

    /// <summary>是否强制发布者提供 Ed25519 公钥</summary>
    [Key("pubkeyRequired")]
    public bool pubkey_required { get; set; } = true;

    #endregion

    #region 存储配置

    /// <summary>存储后端类型</summary>
    public StorageBackend storage { get; set; } = StorageBackend.local;

    /// <summary>存储路径（Local 模式下的目录路径，S3 模式下的桶名）</summary>
    [Key("storagePath")]
    public string storage_path { get; set; } = "./data";

    /// <summary>S3 配置（仅当 Storage 为 S3 时使用）</summary>
    public S3Config? s3 { get; set; }

    #endregion

    #region 限制

    /// <summary>单个包最大大小（MB）</summary>
    [Key("maxPackageSizeMB")]
    public int max_package_size_mb { get; set; } = 100;

    /// <summary>最大总存储空间（GB）</summary>
    [Key("maxTotalStorageGB")]
    public int max_total_storage_gb { get; set; } = 50;

    #endregion

    #region 功能开关

    /// <summary>审计日志是否公开可访问</summary>
    [Key("enableAuditPublicAccess")]
    public bool enable_audit_public_access { get; set; } = true;

    /// <summary>是否启用 Prometheus 指标</summary>
    [Key("enableMetrics")]
    public bool enable_metrics { get; set; } = true;

    #endregion

    #region 网络配置

    /// <summary>监听端口</summary>
    public int port { get; set; } = 8080;

    /// <summary>CORS 配置</summary>
    public CorsConfig cors { get; set; } = new();

    #endregion

    #region 序列化

    /// <summary>
    ///     从 valhalla.von 文件加载配置
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    public static async Task<ValhallaConfig> load(string configPath)
    {
        if (!File.Exists(configPath)) throw new FileNotFoundException($"配置文件不存在: {configPath}", configPath);

        var content = await File.ReadAllTextAsync(configPath);
        return parse(content);
    }

    /// <summary>
    ///     从 von 格式文本解析配置
    /// </summary>
    /// <param name="content">von 格式内容</param>
    public static ValhallaConfig parse(string content)
    {
        var parser = new VonParser(new DiagnosticSink());
        var value = parser.Deserialize(content);
        if (value.fields is null) throw new ArgumentException("配置文件必须是对象格式");

        return from_serde(value);
    }

    /// <summary>
    ///     保存配置到 valhalla.von 文件
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    public async Task save(string configPath)
    {
        var dir = Path.GetDirectoryName(configPath);
        if (dir is not null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var serde = to_serde();
        var content = format_von(serde);
        await File.WriteAllTextAsync(configPath, content);
    }

    /// <summary>
    ///     将配置序列化为 von 格式字符串
    /// </summary>
    public string to_von_string()
    {
        return format_von(to_serde());
    }

    #endregion

    #region Serde 转换

    private static ValhallaConfig from_serde(SerdeValue obj)
    {
        if (SerdeConfigNodeAdapter.from_serde(obj) is not ObjectConfigNode root)
        {
            throw new ArgumentException("配置文件必须是对象格式");
        }

        var config = new ValhallaConfig();
        ConfigProjector.project_into(config, root);
        return config;
    }

    private SerdeValue to_serde()
    {
        var fields = new Dictionary<string, SerdeValue>
        {
            ["name"] = SerdeValue.@string(name),
            ["description"] = SerdeValue.@string(description),
            ["public"] = SerdeValue.boolean(@public),
            ["registration"] = SerdeValue.@string(registration switch
            {
                RegistrationMode.invite => "invite",
                RegistrationMode.closed => "closed",
                _ => "open"
            }),
            ["coolingPeriodDays"] = SerdeValue.integer(cooling_period_days.ToString()),
            ["requireAdminForReRegistration"] = SerdeValue.boolean(require_admin_for_re_registration),
            ["pubkeyRequired"] = SerdeValue.boolean(pubkey_required),
            ["storage"] = SerdeValue.@string(storage switch
            {
                StorageBackend.s3 => "s3",
                _ => "local"
            }),
            ["storagePath"] = SerdeValue.@string(storage_path),
            ["maxPackageSizeMB"] = SerdeValue.integer(max_package_size_mb.ToString()),
            ["maxTotalStorageGB"] = SerdeValue.integer(max_total_storage_gb.ToString()),
            ["enableAuditPublicAccess"] = SerdeValue.boolean(enable_audit_public_access),
            ["enableMetrics"] = SerdeValue.boolean(enable_metrics),
            ["port"] = SerdeValue.integer(port.ToString())
        };

        if (!string.IsNullOrWhiteSpace(invite_code)) fields["inviteCode"] = SerdeValue.@string(invite_code);

        if (s3 is not null)
        {
            var s3Fields = new Dictionary<string, SerdeValue>
            {
                ["endpoint"] = SerdeValue.@string(s3.endpoint),
                ["bucket"] = SerdeValue.@string(s3.bucket),
                ["accessKey"] = SerdeValue.@string(s3.access_key),
                ["secretKey"] = SerdeValue.@string(s3.secret_key),
                ["region"] = SerdeValue.@string(s3.region)
            };
            fields["s3"] = SerdeValue.@object(s3Fields);
        }

        var corsFields = new Dictionary<string, SerdeValue>
        {
            ["origins"] = SerdeValue.array(
                [.. cors.origins.Select(o => SerdeValue.@string(o))])
        };
        fields["cors"] = SerdeValue.@object(corsFields);

        return SerdeValue.@object(fields);
    }

    #endregion

    #region 工具方法

    /// <summary>
    ///     创建默认配置
    /// </summary>
    public static ValhallaConfig create_default()
    {
        return new ValhallaConfig();
    }

    /// <summary>
    ///     验证配置是否有效
    /// </summary>
    /// <returns>验证错误列表，空列表表示有效</returns>
    public List<string> validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name)) errors.Add("实例名称不能为空");

        if (max_package_size_mb <= 0) errors.Add("单个包最大大小必须大于 0");

        if (max_total_storage_gb <= 0) errors.Add("最大总存储空间必须大于 0");

        if (cooling_period_days < 0) errors.Add("冷却期天数不能为负数");

        if (port is <= 0 or > 65535) errors.Add("端口号必须在 1-65535 范围内");

        if (storage == StorageBackend.s3 && s3 is null) errors.Add("使用 S3 存储时必须配置 S3 信息");

        if (storage == StorageBackend.s3 && s3 is not null)
        {
            if (string.IsNullOrWhiteSpace(s3.endpoint)) errors.Add("S3 端点地址不能为空");

            if (string.IsNullOrWhiteSpace(s3.bucket)) errors.Add("S3 存储桶名称不能为空");
        }

        return errors;
    }

    #endregion

    #region 私有辅助

    private static string format_von(SerdeValue value, int indent = 0)
    {
        var sb = new StringBuilder();
        format_value(sb, value, indent);
        return sb.ToString();
    }

    private static void format_value(StringBuilder sb, SerdeValue value, int indent)
    {
        switch (value.type)
        {
            case SerdeValueType.@null:
                sb.Append("null");
                break;
            case SerdeValueType.boolean:
                sb.Append(value.get_boolean() ? "true" : "false");
                break;
            case SerdeValueType.integer:
                sb.Append(value.get_integer_string() ?? "0");
                break;
            case SerdeValueType.@decimal:
                sb.Append(value.get_decimal_string() ?? "0.0");
                break;
            case SerdeValueType.@string:
                sb.Append('"');
                sb.Append(escape_string(value.get_string() ?? ""));
                sb.Append('"');
                break;
            case SerdeValueType.array:
                format_von_array(sb, value, indent);
                break;
            case SerdeValueType.@object:
                format_von_object(sb, value, indent);
                break;
        }
    }

    private static void format_von_object(StringBuilder sb, SerdeValue value, int indent)
    {
        if (value.fields is null || value.fields.Count == 0)
        {
            sb.Append("{}");
            return;
        }

        var indentStr = new string(' ', indent * 4);
        var innerIndent = new string(' ', (indent + 1) * 4);

        sb.AppendLine("{");

        var fieldList = value.fields.ToList();
        for (var i = 0; i < fieldList.Count; i++)
        {
            var (key, fieldValue) = fieldList[i];
            sb.Append(innerIndent);
            sb.Append(key);
            sb.Append(": ");

            if (fieldValue.type is SerdeValueType.@object or SerdeValueType.array)
                format_value(sb, fieldValue, indent + 1);
            else
                format_value(sb, fieldValue, 0);

            if (i < fieldList.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }

        sb.Append(indentStr);
        sb.Append('}');
    }

    private static void format_von_array(StringBuilder sb, SerdeValue value, int indent)
    {
        if (value.elements is null || value.elements.Count == 0)
        {
            sb.Append("[]");
            return;
        }

        var allSimple = value.elements.All(e =>
            e.type is not SerdeValueType.@object and not SerdeValueType.array);

        if (allSimple)
        {
            sb.Append("[ ");
            for (var i = 0; i < value.elements.Count; i++)
            {
                format_value(sb, value.elements[i], 0);
                if (i < value.elements.Count - 1) sb.Append(", ");
            }

            sb.Append(" ]");
        }
        else
        {
            var indentStr = new string(' ', indent * 4);
            var innerIndent = new string(' ', (indent + 1) * 4);

            sb.AppendLine("[");
            for (var i = 0; i < value.elements.Count; i++)
            {
                sb.Append(innerIndent);
                format_value(sb, value.elements[i], indent + 1);
                if (i < value.elements.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }

            sb.Append(indentStr);
            sb.Append(']');
        }
    }

    private static string escape_string(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }

        return sb.ToString();
    }

    #endregion
}
