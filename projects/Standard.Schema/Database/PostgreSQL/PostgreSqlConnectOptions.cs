namespace Hermes.Database.PostgreSql;

public sealed record PostgreSqlConnectOptions
{
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5432;

    public string Username { get; init; } = "postgres";

    public string Password { get; init; } = "";

    public string Database { get; init; } = "postgres";

    public int ConnectionTimeoutSeconds { get; init; } = 30;

    public string ApplicationName { get; init; } = "Hermes.Database.PostgreSql";
}