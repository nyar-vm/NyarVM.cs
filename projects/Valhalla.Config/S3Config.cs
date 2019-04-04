using Core.Data;
using Std.Config;

namespace Valhalla.Config;

/// <summary>
///     S3 存储配置
/// </summary>
[Data]
public class S3Config
{
    /// <summary>S3 端点地址</summary>
    public string endpoint { get; set; } = string.Empty;

    /// <summary>存储桶名称</summary>
    public string bucket { get; set; } = string.Empty;

    /// <summary>访问密钥</summary>
    [Key("accessKey")]
    public string access_key { get; set; } = string.Empty;

    /// <summary>秘密密钥</summary>
    [Key("secretKey")]
    public string secret_key { get; set; } = string.Empty;

    /// <summary>区域</summary>
    public string region { get; set; } = "us-east-1";
}
