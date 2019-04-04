namespace Hermes.Hosting;

public enum HermesServiceKind
{
    Database,
    Cache,
    Storage,
    Stream
}

public sealed class HermesBackendConfig
{
    public HermesServiceKind Kind { get; set; } = HermesServiceKind.Database;

    public string Type { get; set; } = "inmemory";

    public string? ConnectionString { get; set; }

    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Database { get; set; }

    public string? Bucket { get; set; }

    public string? Region { get; set; }

    public string? Endpoint { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string? BasePath { get; set; }

    public string? BootstrapServers { get; set; }

    public string? ConsumerGroupId { get; set; }

    public string KeyPrefix { get; set; } = "hermes:";

    public int DefaultExpireMinutes { get; set; } = 60;

    public bool UseHttps { get; set; } = true;

    public string? Charset { get; set; }
}

public sealed class HermesHostingOptions
{
    public Dictionary<string, HermesBackendConfig> Backends { get; set; } = new();

    public string DefaultDatabase { get; set; } = "primary-db";

    public string DefaultCache { get; set; } = "primary-cache";

    public string DefaultStorage { get; set; } = "primary-storage";

    public string DefaultStream { get; set; } = "primary-stream";
}