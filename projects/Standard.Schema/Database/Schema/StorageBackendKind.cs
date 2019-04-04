namespace Hermes.Database.Schema;

public enum StorageBackendKind
{
    YYDB,
    SQLite,
    PostgreSQL,
    MySQL,
    S3,
    FileSystem,
    Redis,
    Kafka
}