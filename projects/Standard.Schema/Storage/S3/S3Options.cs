namespace Hermes.Storage.S3;

public sealed class S3Options
{
    public string Endpoint { get; init; } = "s3.amazonaws.com";

    public string AccessKey { get; init; } = "";

    public string SecretKey { get; init; } = "";

    public string Bucket { get; init; } = "";

    public string Region { get; init; } = "us-east-1";

    public bool UseHttps { get; init; } = true;

    public bool PathStyle { get; init; }

    public int PresignedUrlExpirySeconds { get; init; } = 3600;
}