using Nyar.Database;
using Nyar.Database.Index;
using Nyar.Database.Integration.Storage;
using Nyar.Database.Storage;
using Nyar.Types;

namespace Nyar.Tests.Database;

public sealed class StorageEngineTests : IAsyncLifetime
{
    #region AppendWalBatchAsync 测试

    [Fact]
    public async Task AppendWalBatchAsync_批量操作_全部持久�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");
        var symbols = new[]
        {
            CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class),
            CreateSymbol("file:///a.ts", "bar", SymbolKind.Method)
        };
        var references = new[]
        {
            CreateReference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts")
        };

        var operations = new List<(WalOperationType, object)>
        {
            (WalOperationType.UpsertFile, file),
            (WalOperationType.UpsertSymbol, symbols[0]),
            (WalOperationType.UpsertSymbol, symbols[1]),
            (WalOperationType.UpsertReference, references[0])
        };

        await _engine.AppendWalBatchAsync(operations);

        var newManager = new IndexManager();
        await _engine.RestoreAsync(newManager);

        Assert.Equal(1, newManager.Files.Count);
        Assert.Equal(2, newManager.Symbols.Count);
        Assert.Equal(1, newManager.References.Count);
    }

    #endregion

    #region RestoreFileIndexAsync 测试

    [Fact]
    public async Task RestoreFileIndexAsync_仅加载文件索�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");
        var symbol = CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class);
        await _engine.AppendWalAsync(WalOperationType.UpsertFile, file);
        await _engine.AppendWalAsync(WalOperationType.UpsertSymbol, symbol);

        var newManager = new IndexManager();
        await _engine.RestoreFileIndexAsync(newManager);

        Assert.Equal(1, newManager.Files.Count);
        Assert.Equal(0, newManager.Symbols.Count);
    }

    #endregion

    #region LoadSymbolsForFileAsync 测试

    [Fact]
    public async Task LoadSymbolsForFileAsync_按需加载指定文件的符�?)
    {
        await _engine.AppendWalAsync(WalOperationType.UpsertSymbol,
            CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class));
        await _engine.AppendWalAsync(WalOperationType.UpsertSymbol,
            CreateSymbol("file:///b.ts", "Bar", SymbolKind.Function));

        var newManager = new IndexManager();
        await _engine.LoadSymbolsForFileAsync(newManager, "file:///a.ts");

        Assert.Equal(1, newManager.Symbols.Count);
        Assert.Equal("Foo", newManager.Symbols.GetByFileUri("file:///a.ts")[0].Name);
    }

    #endregion

    #region LoadReferencesForFileAsync 测试

    [Fact]
    public async Task LoadReferencesForFileAsync_按需加载指定文件的引�?)
    {
        await _engine.AppendWalAsync(WalOperationType.UpsertReference,
            CreateReference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts"));
        await _engine.AppendWalAsync(WalOperationType.UpsertReference,
            CreateReference("file:///c.ts", "Bar", SymbolKind.Function, "file:///d.ts"));

        var newManager = new IndexManager();
        await _engine.LoadReferencesForFileAsync(newManager, "file:///b.ts");

        Assert.Equal(1, newManager.References.Count);
    }

    #endregion

    #region SaveAsync 测试

    [Fact]
    public async Task SaveAsync_检查点后数据可恢复()
    {
        var file = CreateFile("file:///a.ts", "hash1");
        await _engine.AppendWalAsync(WalOperationType.UpsertFile, file);
        await _engine.SaveAsync(_index_manager);

        await _engine.DisposeAsync();

        using var engine2 = new StorageEngine(_temp_path);
        var newManager = new IndexManager();
        await engine2.RestoreFileIndexAsync(newManager);

        Assert.Equal(1, newManager.Files.Count);
    }

    #endregion

    #region 字段

    private string _temp_path = null!;
    private StorageEngine _engine = null!;
    private IndexManager _index_manager = null!;

    #endregion

    #region IAsyncLifetime

    public async Task InitializeAsync()
    {
        _temp_path = Path.Combine(Path.GetTempPath(), $"nyar-test-{Guid.NewGuid():N}");
        _engine = new StorageEngine(_temp_path);
        _index_manager = new IndexManager();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _engine.DisposeAsync();
        try
        {
            if (Directory.Exists(_temp_path))
            {
                Directory.Delete(_temp_path, true);
            }
        }
        catch
        {
            // 测试清理时忽略目录删除失�?        }
    }

    #endregion

    #region 辅助方法

    private static SymbolRecord CreateSymbol(string fileUri, string name, SymbolKind kind)
    {
        var symbol = new SymbolRecord();
        symbol.Id = SymbolId.Create(fileUri, name, kind);
        symbol.Name = name;
        symbol.Kind = kind;
        symbol.FileUri = fileUri;
        symbol.Location = Loc.Zero;
        symbol.Accessibility = SymbolAccessibility.Public;
        return symbol;
    }

    private static ReferenceRecord CreateReference(string symbolFileUri, string symbolName, SymbolKind symbolKind,
        string refFileUri)
    {
        var record = new ReferenceRecord();
        record.SymbolId = SymbolId.Create(symbolFileUri, symbolName, symbolKind);
        record.FileUri = refFileUri;
        record.Location = Loc.Zero;
        record.Kind = ReferenceKind.Read;
        return record;
    }

    private static FileRecord CreateFile(string uri, string hash, string[]? deps = null)
    {
        var file = new FileRecord();
        file.Uri = uri;
        file.ContentHash = hash;
        file.LastModified = DateTime.UtcNow;
        file.DependencyUris = deps ?? [];
        file.LanguageId = "typescript";
        file.AnalysisStatus = FileAnalysisStatus.Completed;
        return file;
    }

    #endregion

    #region AppendWalAsync 测试

    [Fact]
    public async Task AppendWalAsync_UpsertSymbol_持久化成�?)
    {
        var symbol = CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class);

        await _engine.AppendWalAsync(WalOperationType.UpsertSymbol, symbol);

        var newManager = new IndexManager();
        await _engine.RestoreAsync(newManager);

        Assert.Equal(1, newManager.Symbols.Count);
        Assert.True(newManager.Symbols.TryGet(symbol.Id, out var restored));
        Assert.Equal("Foo", restored.Name);
    }

    [Fact]
    public async Task AppendWalAsync_UpsertFile_持久化成�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");

        await _engine.AppendWalAsync(WalOperationType.UpsertFile, file);

        var newManager = new IndexManager();
        await _engine.RestoreFileIndexAsync(newManager);

        Assert.Equal(1, newManager.Files.Count);
        var found = newManager.Files.Find("file:///a.ts");
        Assert.NotNull(found);
        Assert.Equal("hash1", found.ContentHash);
    }

    [Fact]
    public async Task AppendWalAsync_UpsertReference_持久化成�?)
    {
        var reference = CreateReference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts");

        await _engine.AppendWalAsync(WalOperationType.UpsertReference, reference);

        var newManager = new IndexManager();
        await _engine.RestoreAsync(newManager);

        Assert.Equal(1, newManager.References.Count);
    }

    [Fact]
    public async Task AppendWalAsync_RemoveSymbol_删除成功()
    {
        var symbol = CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class);
        await _engine.AppendWalAsync(WalOperationType.UpsertSymbol, symbol);
        await _engine.AppendWalAsync(WalOperationType.RemoveSymbol, symbol.Id);

        var newManager = new IndexManager();
        await _engine.RestoreAsync(newManager);

        Assert.Equal(0, newManager.Symbols.Count);
    }

    [Fact]
    public async Task AppendWalAsync_RemoveFile_删除成功()
    {
        var file = CreateFile("file:///a.ts", "hash1");
        await _engine.AppendWalAsync(WalOperationType.UpsertFile, file);
        await _engine.AppendWalAsync(WalOperationType.RemoveFile, "file:///a.ts");

        var newManager = new IndexManager();
        await _engine.RestoreFileIndexAsync(newManager);

        Assert.Equal(0, newManager.Files.Count);
    }

    #endregion
}
