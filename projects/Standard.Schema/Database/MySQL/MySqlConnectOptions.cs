namespace Hermes.Database.MySql;

public sealed class MySqlConnectOptions
{
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 3306;

    public string Username { get; init; } = "root";

    public string Password { get; init; } = "";

    public string Database { get; init; } = "";

    public int ConnectionTimeoutSeconds { get; init; } = 30;

    public int MaxPacketSize { get; init; } = 16 * 1024 * 1024;

    public string Charset { get; init; } = "utf8mb4";
}