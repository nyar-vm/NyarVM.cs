using BenchmarkDotNet.Attributes;
using LightDB.Core;

namespace LightDB.Benchmarks.Core;

/// <summary>
///     崩溃恢复基准测试，验证 <1s（100MB WAL）的目标
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class CrashRecoveryBenchmark
{
    private string _tempPath = default!;

    [Params(1000, 10000, 50000)] public int WalRecordCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"lightdb-recover-bench-{Guid.NewGuid():N}");
    }

    private async Task<(string Path, long WalSize)> PrepareDatabaseAsync()
    {
        var dbPath = Path.Combine(_tempPath, $"recover-{Guid.NewGuid():N}");

        await using var db = new LightDatabase(new LightOptions
        {
            Path = dbPath,
            AutoCheckpoint = false
        });

        var writeValue = LightValue.FromString(new string('x', 200));

        for (var i = 0; i < WalRecordCount; i++)
        {
            var key = LightKey.FromString($"recover:key:{i:D8}");
            await db.Put(key, writeValue);
        }

        var walPath = Path.Combine(dbPath, "light.wal");
        var walSize = File.Exists(walPath) ? new FileInfo(walPath).Length : 0;

        return (dbPath, walSize);
    }

    [Benchmark(Description = "崩溃恢复时间 (从 WAL)")]
    public async Task RecoverFromWal()
    {
        var (dbPath, walSize) = await PrepareDatabaseAsync();

        var metaPath = Path.Combine(dbPath, "light.meta");
        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }

        await using var db = new LightDatabase(new LightOptions
        {
            Path = dbPath,
            AutoCheckpoint = false
        });

        await Task.CompletedTask;
    }

    [Benchmark(Description = "崩溃恢复时间 (元数据 + WAL 增量)")]
    public async Task RecoverFromMetadata()
    {
        var (dbPath, _) = await PrepareDatabaseAsync();

        {
            await using var prepareDb = new LightDatabase(new LightOptions
            {
                Path = dbPath,
                AutoCheckpoint = false
            });
            await prepareDb.CheckPoint();
        }

        {
            await using var appendDb = new LightDatabase(new LightOptions
            {
                Path = dbPath,
                AutoCheckpoint = false
            });

            var writeValue = LightValue.FromString(new string('y', 200));
            for (var i = 0; i < WalRecordCount / 2; i++)
            {
                var key = LightKey.FromString($"recover:extra:{i:D8}");
                await appendDb.Put(key, writeValue);
            }
        }

        await using var recoverDb = new LightDatabase(new LightOptions
        {
            Path = dbPath,
            AutoCheckpoint = false
        });

        await Task.CompletedTask;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_tempPath))
            {
                Directory.Delete(_tempPath, true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
