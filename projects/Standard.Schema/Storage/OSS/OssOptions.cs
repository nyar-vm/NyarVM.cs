namespace Hermes.Storage.Oss;

public sealed class OssOptions
{
    public string Endpoint { get; init; } = "oss-cn-hangzhou.aliyuncs.com";

    public string AccessKey { get; init; } = "";

    public string SecretKey { get; init; } = "";

    public string Bucket { get; init; } = "";

    public bool UseHttps { get; init; } = true;

    public int PresignedUrlExpirySeconds { get; init; } = 3600;
}