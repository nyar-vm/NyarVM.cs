namespace Atlas.CLI;

public sealed record AtlasConfig
{
    public string database_provider { get; init; } = "inmemory";
    public string database_connection { get; init; } = "";
    public string? main_database_connection { get; init; }
    public string schema_path { get; init; } = "schema.hermes";
    public List<string> generators { get; init; } = ["csharp", "sql-ddl"];
    public string output_path { get; init; } = "generated";
    public string? @namespace { get; init; }

    public string test_database_connection => database_connection;

    public bool has_main_database => main_database_connection is not null;

    public static AtlasConfig load(string? configPath)
    {
        var searchPaths = configPath is not null
            ? new[] { configPath }
            : new[] { "atlas.config.von", "config.von" };

        foreach (var path in searchPaths)
        {
            if (!File.Exists(path)) continue;

            var source = File.ReadAllText(path);
            var parser = new VonParser(new Oak.Diagnostics.DiagnosticSink());
            var value = parser.Deserialize(source);

            if (value.type != SerdeValueType.@object) continue;

            var configDir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";
            var config = from_serde_value(value);

            config = config with
            {
                schema_path = resolve_path(configDir, config.schema_path),
                output_path = resolve_path(configDir, config.output_path)
            };

            return config;
        }

        return new AtlasConfig();
    }

    private static string resolve_path(string baseDir, string path)
    {
        if (Path.IsPathRooted(path)) return path;

        return Path.GetFullPath(Path.Combine(baseDir, path));
    }

    private static AtlasConfig from_serde_value(SerdeValue value)
    {
        var config = new AtlasConfig();

        var dbField = value.get_field("database");
        if (dbField is not null) config = parse_database_field(dbField, config);

        if (value.get_field("schema") is { } schema)
            config = config with { schema_path = schema.get_string() ?? "schema.hermes" };

        if (value.get_field("output") is { } output)
            config = config with { output_path = output.get_string() ?? "generated" };

        if (value.get_field("generators") is { Type: SerdeValueType.array } gens)
            config = config with
            {
                generators = gens.elements?
                    .Select(e => e.get_string() ?? "")
                    .Where(s => s.Length > 0)
                    .ToList() ?? ["csharp", "sql-ddl"]
            };

        if (value.get_field("namespace") is { } ns) config = config with { @namespace = ns.get_string() };

        return config;
    }

    private static AtlasConfig parse_database_field(SerdeValue dbField, AtlasConfig config)
    {
        if (dbField.type == SerdeValueType.@string)
        {
            var dbUrl = dbField.get_string();
            if (dbUrl is not null && try_parse_database_url(dbUrl, out var provider, out var connectionString))
                config = config with
                {
                    database_provider = provider,
                    database_connection = connectionString
                };

            return config;
        }

        if (dbField.type == SerdeValueType.@object)
        {
            var testField = dbField.get_field("test");
            if (testField is not null)
            {
                var testUrl = testField.get_string();
                if (testUrl is not null && try_parse_database_url(testUrl, out var provider, out var connectionString))
                    config = config with
                    {
                        database_provider = provider,
                        database_connection = connectionString
                    };
            }

            var mainField = dbField.get_field("main");
            if (mainField is not null)
            {
                var mainUrl = mainField.get_string();
                if (mainUrl is not null && try_parse_database_url(mainUrl, out _, out var mainConnectionString))
                    config = config with { main_database_connection = mainConnectionString };
            }

            return config;
        }

        return config;
    }

    private static bool try_parse_database_url(string url, out string provider, out string connectionString)
    {
        provider = "inmemory";
        connectionString = "";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        var scheme = uri.Scheme.ToLowerInvariant();

        switch (scheme)
        {
            case "postgresql" or "pgsql":
                provider = "postgresql";
                connectionString = build_npgsql_connection_string(uri);
                return true;
            case "mysql":
                provider = "mysql";
                connectionString = build_my_sql_connection_string(uri);
                return true;
            case "sqlite":
                provider = "sqlite";
                connectionString = $"Data Source={uri.AbsolutePath}";
                return true;
            default:
                return false;
        }
    }

    private static string build_npgsql_connection_string(Uri uri)
    {
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var username = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[0] : uri.UserInfo;
        var password = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[1] : "";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    private static string build_my_sql_connection_string(Uri uri)
    {
        var server = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 3306;
        var database = uri.AbsolutePath.TrimStart('/');
        var userInfo = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':') : [uri.UserInfo, ""];
        var userId = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";

        return $"Server={server};Port={port};Database={database};User ID={userId};Password={password}";
    }
}