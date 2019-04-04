namespace Hermes.Hosting;

public static class HermesHostingExtensions
{
    public static IServiceCollection AddHermesInfrastructure(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        var options = new HermesHostingOptions();
        section.Bind(options);

        services.AddSingleton(options);

        foreach (var (name, backend) in options.Backends) RegisterBackend(services, name, backend);

        return services;
    }

    private static void RegisterBackend(
        IServiceCollection services,
        string name,
        HermesBackendConfig backend)
    {
        switch (backend.Kind)
        {
            case HermesServiceKind.Database:
                RegisterDatabase(services, name, backend);
                break;
            case HermesServiceKind.Cache:
                RegisterCache(services, name, backend);
                break;
            case HermesServiceKind.Storage:
                RegisterStorage(services, name, backend);
                break;
            case HermesServiceKind.Stream:
                RegisterStream(services, name, backend);
                break;
        }
    }

    #region Cache

    private static void RegisterCache(
        IServiceCollection services,
        string name,
        HermesBackendConfig config)
    {
        switch (config.type.ToLowerInvariant())
        {
            case "redis":
                services.AddSingleton<ICacheService>(_ =>
                    new RedisCacheService(new CacheOptions
                    {
                        ConnectionString = config.ConnectionString ?? config.Endpoint ?? "localhost:6379",
                        KeyPrefix = config.KeyPrefix,
                        DefaultExpireMinutes = config.DefaultExpireMinutes
                    }));
                break;
            default:
                services.AddSingleton<ICacheService>(_ =>
                    new MemoryCacheService(new CacheOptions
                    {
                        KeyPrefix = config.KeyPrefix,
                        DefaultExpireMinutes = config.DefaultExpireMinutes
                    }));
                break;
        }
    }

    #endregion

    #region Storage

    private static void RegisterStorage(
        IServiceCollection services,
        string name,
        HermesBackendConfig config)
    {
        switch (config.type.ToLowerInvariant())
        {
            case "s3":
                services.AddSingleton<IObjectStorage>(_ =>
                    new S3Storage(new S3Options
                    {
                        Endpoint = config.Endpoint ?? "",
                        AccessKey = config.AccessKey ?? "",
                        SecretKey = config.SecretKey ?? "",
                        Bucket = config.Bucket ?? "",
                        Region = config.Region,
                        UseHttps = config.UseHttps
                    }));
                break;
            case "oss":
                services.AddSingleton<IObjectStorage>(_ =>
                    new OssStorage(new OssOptions
                    {
                        Endpoint = config.Endpoint ?? "",
                        AccessKey = config.AccessKey ?? "",
                        SecretKey = config.SecretKey ?? "",
                        Bucket = config.Bucket ?? "",
                        UseHttps = config.UseHttps
                    }));
                break;
            default:
                services.AddSingleton<IObjectStorage>(_ =>
                    new LocalFileSystemStorage(new StorageOptions
                    {
                        BasePath = config.BasePath ?? "./storage"
                    }));
                break;
        }
    }

    #endregion

    #region Stream

    private static void RegisterStream(
        IServiceCollection services,
        string name,
        HermesBackendConfig config)
    {
        switch (config.type.ToLowerInvariant())
        {
            case "kafka":
                services.AddSingleton<IStreamService>(_ =>
                    new KafkaStreamService(new StreamOptions
                    {
                        BootstrapServers = config.BootstrapServers ?? config.ConnectionString,
                        ConsumerGroupId = config.ConsumerGroupId
                    }));
                break;
            default:
                services.AddSingleton<IStreamService>(_ =>
                    new MemoryStreamService(new StreamOptions
                    {
                        TopicPrefix = config.KeyPrefix
                    }));
                break;
        }
    }

    #endregion

    private sealed class StorageAdapterQueryExecutor : IQueryExecutor
    {
        private readonly IStorageAdapter _adapter;

        public StorageAdapterQueryExecutor(IStorageAdapter adapter)
        {
            _adapter = adapter;
        }

        public async Task<OrmQueryResult> ExecuteAsync(
            QueryExpression query, CancellationToken ct = default)
        {
            var dbResult = query switch
            {
                FindQuery => await _adapter.ReadAsync(query, ct),
                AggregateQuery => await _adapter.AggregateAsync(query, ct),
                CreateQuery => await _adapter.WriteAsync(query, ct),
                UpdateQuery => await _adapter.WriteAsync(query, ct),
                DeleteQuery => await _adapter.DeleteAsync(query, ct),
                _ => QueryResult.Fail($"不支持的查询类型：{query.QueryKind}")
            };

            if (!dbResult.Success) return OrmQueryResult.Fail(dbResult.Error ?? "未知数据库错误");

            if (dbResult.Rows.Count > 0) return OrmQueryResult.Ok(dbResult.Rows);

            return OrmQueryResult.Ok(dbResult.AffectedCount);
        }
    }

    #region Database

    private static void RegisterDatabase(
        IServiceCollection services,
        string name,
        HermesBackendConfig config)
    {
        switch (config.type.ToLowerInvariant())
        {
            case "mysql":
                RegisterMySql(services, config);
                break;
            case "postgresql":
            case "pgsql":
                RegisterPostgreSql(services, config);
                break;
            default:
                RegisterInMemory(services);
                break;
        }
    }

    private static void RegisterMySql(IServiceCollection services, HermesBackendConfig config)
    {
        var options = BuildMySqlOptions(config);
        services.AddSingleton(options);
        services.AddSingleton<MySqlStorageAdapter>();
        services.AddSingleton<IQueryExecutor>(sp =>
            new StorageAdapterQueryExecutor(sp.GetRequiredService<MySqlStorageAdapter>()));
    }

    private static void RegisterPostgreSql(IServiceCollection services, HermesBackendConfig config)
    {
        var options = BuildPostgreSqlOptions(config);
        services.AddSingleton(options);
        services.AddSingleton<PostgreSqlStorageAdapter>();
        services.AddSingleton<IQueryExecutor>(sp =>
            new StorageAdapterQueryExecutor(sp.GetRequiredService<PostgreSqlStorageAdapter>()));
    }

    private static void RegisterInMemory(IServiceCollection services)
    {
        services.AddSingleton<IAtlasDatabase, InMemoryDatabase>();
        services.AddSingleton<IQueryExecutor>(sp =>
            new InMemoryQueryExecutor(sp.GetRequiredService<IAtlasDatabase>()));
    }

    private static MySqlConnectOptions BuildMySqlOptions(HermesBackendConfig config)
    {
        if (!string.IsNullOrEmpty(config.ConnectionString)) return ParseMySqlConnectionString(config.ConnectionString);

        return new MySqlConnectOptions
        {
            Host = config.Host ?? "localhost",
            Port = config.Port ?? 3306,
            Username = config.Username ?? "root",
            Password = config.Password ?? "",
            Database = config.Database ?? "",
            Charset = config.Charset ?? "utf8mb4"
        };
    }

    /// <summary>
    ///     解析 MySQL 连接字符串（Server=...;Port=...; 格式或 mysql://user:pass@host:port/db 格式）
    /// </summary>
    private static MySqlConnectOptions ParseMySqlConnectionString(string connectionString)
    {
        if (connectionString.StartsWith("mysql://"))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':', 2);
            return new MySqlConnectOptions
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 3306,
                Username = userInfo.Length > 0 ? userInfo[0] : "root",
                Password = userInfo.Length > 1 ? userInfo[1] : "",
                Database = uri.AbsolutePath.Trim('/'),
                Charset = "utf8mb4"
            };
        }

        var dict = ParseKeyValueConnectionString(connectionString);
        return new MySqlConnectOptions
        {
            Host = dict.GetValueOrDefault("Server") ?? dict.GetValueOrDefault("Host") ?? "localhost",
            Port = int.TryParse(dict.GetValueOrDefault("Port") ?? "3306", out var port) ? port : 3306,
            Username = dict.GetValueOrDefault("User ID") ?? dict.GetValueOrDefault("Username") ?? "root",
            Password = dict.GetValueOrDefault("Password") ?? dict.GetValueOrDefault("Pwd") ?? "",
            Database = dict.GetValueOrDefault("Database") ?? dict.GetValueOrDefault("Initial Catalog") ?? "",
            Charset = dict.GetValueOrDefault("Character Set") ?? dict.GetValueOrDefault("Charset") ?? "utf8mb4"
        };
    }

    private static PostgreSqlConnectOptions BuildPostgreSqlOptions(HermesBackendConfig config)
    {
        if (!string.IsNullOrEmpty(config.ConnectionString))
            return ParsePostgreSqlConnectionString(config.ConnectionString);

        return new PostgreSqlConnectOptions
        {
            Host = config.Host ?? "localhost",
            Port = config.Port ?? 5432,
            Username = config.Username ?? "postgres",
            Password = config.Password ?? "",
            Database = config.Database ?? "postgres"
        };
    }

    /// <summary>
    ///     解析 PostgreSQL 连接字符串（Host=...;Port=...; 格式或 postgresql://user:pass@host:port/db 格式）
    /// </summary>
    private static PostgreSqlConnectOptions ParsePostgreSqlConnectionString(string connectionString)
    {
        if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':', 2);
            return new PostgreSqlConnectOptions
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = userInfo.Length > 0 ? userInfo[0] : "postgres",
                Password = userInfo.Length > 1 ? userInfo[1] : "",
                Database = uri.AbsolutePath.Trim('/')
            };
        }

        var dict = ParseKeyValueConnectionString(connectionString);
        return new PostgreSqlConnectOptions
        {
            Host = dict.GetValueOrDefault("Host") ?? dict.GetValueOrDefault("Server") ?? "localhost",
            Port = int.TryParse(dict.GetValueOrDefault("Port") ?? "5432", out var port) ? port : 5432,
            Username = dict.GetValueOrDefault("Username") ?? dict.GetValueOrDefault("User ID") ?? "postgres",
            Password = dict.GetValueOrDefault("Password") ?? "",
            Database = dict.GetValueOrDefault("Database") ?? dict.GetValueOrDefault("Initial Catalog") ?? "postgres"
        };
    }

    /// <summary>
    ///     解析 Key=Value; 格式的连接字符串
    /// </summary>
    private static Dictionary<string, string> ParseKeyValueConnectionString(string connectionString)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in connectionString.Split(';'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && !string.IsNullOrWhiteSpace(kv[0])) dict[kv[0].Trim()] = kv[1].Trim();
        }

        return dict;
    }

    #endregion
}