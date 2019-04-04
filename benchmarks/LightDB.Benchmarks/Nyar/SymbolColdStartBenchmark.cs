using BenchmarkDotNet.Attributes;

namespace LightDB.Benchmarks.Nyar;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class SymbolColdStartBenchmark : IDisposable
{
    private SymbolAnalysisEngine _analysisEngine = null!;
    private bool _disposed;
    private IndexManager _indexManager = null!;
    private StorageEngine _storageEngine = null!;
    private string _tempPath = null!;

    [Params(10000, 50000, 100000)] public int SymbolCount { get; set; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (Directory.Exists(_tempPath)) Directory.Delete(_tempPath, true);
        }
        catch
        {
            // 基准测试清理时忽略
        }
    }

    [GlobalSetup]
    public async Task Setup()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"nyar-coldstart-bench-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempPath);

        _storageEngine = new StorageEngine(_tempPath);
        _indexManager = new IndexManager();

        var operations = new List<(WalOperationType, object)>();

        var fileCount = SymbolCount / 10;
        for (var f = 0; f < fileCount; f++)
        {
            var fileUri = $"file:///project/src/module_{f / 100:D3}/file_{f:D5}.ts";
            var file = new FileRecord
            {
                Uri = fileUri,
                ContentHash = $"sha256_{f:D8}",
                LastModified = DateTime.UtcNow,
                DependencyUris = f > 0 ? [$"file:///project/src/module_{(f - 1) / 100:D3}/file_{f - 1:D5}.ts"] : [],
                LanguageId = "typescript",
                AnalysisStatus = FileAnalysisStatus.Completed
            };
            operations.Add((WalOperationType.UpsertFile, file));

            var symbolsPerFile = 10;
            for (var s = 0; s < symbolsPerFile; s++)
            {
                var symbolName = s switch
                {
                    0 => $"Module{f}",
                    1 => $"Config{s}",
                    _ => $"func_{f}_{s}"
                };
                var kind = s switch
                {
                    0 => SymbolKind.Class,
                    1 => SymbolKind.Variable,
                    _ => SymbolKind.Function
                };

                var symbol = new SymbolRecord
                {
                    Id = SymbolId.Create(fileUri, symbolName, kind),
                    Name = symbolName,
                    Kind = kind,
                    FileUri = fileUri,
                    Location = new Loc(s * 10 + 1, 1),
                    Accessibility = s == 0 ? SymbolAccessibility.Public : SymbolAccessibility.Private
                };
                operations.Add((WalOperationType.UpsertSymbol, symbol));

                if (s > 0 && f > 0)
                {
                    var refFileUri = $"file:///project/src/module_{(f - 1) / 100:D3}/file_{f - 1:D5}.ts";
                    var reference = new ReferenceRecord
                    {
                        SymbolId = symbol.Id,
                        FileUri = refFileUri,
                        Location = new Loc(s * 5, 1),
                        Kind = ReferenceKind.Call
                    };
                    operations.Add((WalOperationType.UpsertReference, reference));
                }
            }
        }

        await _storageEngine.AppendWalBatchAsync(operations);
        await _storageEngine.SaveAsync(_indexManager);
        await _storageEngine.DisposeAsync();
    }

    [Benchmark(Description = "冷启动 - 仅加载文件索引")]
    public async Task FastRestore_FileIndexOnly()
    {
        await using var engine = new StorageEngine(_tempPath);
        var manager = new IndexManager();
        await engine.RestoreFileIndexAsync(manager);
    }

    [Benchmark(Description = "冷启动 - 全量恢复")]
    public async Task FullRestore_AllIndexes()
    {
        await using var engine = new StorageEngine(_tempPath);
        var manager = new IndexManager();
        await engine.RestoreAsync(manager);
    }

    [Benchmark(Description = "冷启动 - 按需加载单文件符号")]
    public async Task OnDemandLoad_SingleFileSymbols()
    {
        await using var engine = new StorageEngine(_tempPath);
        var manager = new IndexManager();
        await engine.LoadSymbolsForFileAsync(manager, "file:///project/src/module_000/file_00000.ts");
    }

    [Benchmark(Description = "SymbolAnalysisEngine 快速冷启动")]
    public async Task SymbolAnalysis_FastRestore()
    {
        var engine = new SymbolAnalysisEngine(_tempPath);
        await engine.FastRestoreAsync();
        await engine.DisposeAsync();
    }

    [Benchmark(Description = "SymbolAnalysisEngine 全量恢复")]
    public async Task SymbolAnalysis_FullRestore()
    {
        var engine = new SymbolAnalysisEngine(_tempPath);
        await engine.FullRestoreAsync();
        await engine.DisposeAsync();
    }

    [Benchmark(Description = "符号查询 - FindReferences")]
    public async Task Query_FindReferences()
    {
        var engine = new SymbolAnalysisEngine(_tempPath);
        await engine.FullRestoreAsync();

        var symbolId = SymbolId.Create("file:///project/src/module_000/file_00000.ts", "func_0_2", SymbolKind.Function);
        engine.FindReferences(symbolId);

        await engine.DisposeAsync();
    }
}