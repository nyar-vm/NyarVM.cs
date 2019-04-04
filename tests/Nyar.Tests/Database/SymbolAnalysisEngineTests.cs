using Nyar.Database;
using Nyar.Database.Index;
using Nyar.Database.Integration;
using Nyar.Types;

namespace Nyar.Tests.Database;

public sealed class SymbolAnalysisEngineTests : IAsyncLifetime
{
    #region FastRestoreAsync 测试

    [Fact]
    public async Task FastRestoreAsync_仅加载文件索�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");
        var symbols = new[] { CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class) };
        await _engine.UpdateFileAsync(file, symbols, []);

        await _engine.DisposeAsync();

        var engine2 = new SymbolAnalysisEngine(_temp_path);
        await using (engine2)
        {
            await engine2.FastRestoreAsync();

            Assert.True(engine2.IsRestored);
            Assert.Equal(1, engine2.IndexManager.Files.Count);
            Assert.Equal(0, engine2.IndexManager.Symbols.Count);
        }
    }

    #endregion

    #region FullRestoreAsync 测试

    [Fact]
    public async Task FullRestoreAsync_加载所有索�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");
        var symbols = new[] { CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class) };
        var references = new[] { CreateReference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts") };
        await _engine.UpdateFileAsync(file, symbols, references);
        await _engine.SaveAsync();

        await _engine.DisposeAsync();

        var engine2 = new SymbolAnalysisEngine(_temp_path);
        await using (engine2)
        {
            await engine2.FullRestoreAsync();

            Assert.True(engine2.IsRestored);
            Assert.Equal(1, engine2.IndexManager.Files.Count);
            Assert.Equal(1, engine2.IndexManager.Symbols.Count);
            Assert.Equal(1, engine2.IndexManager.References.Count);
        }
    }

    #endregion

    #region RemoveFileAsync 测试

    [Fact]
    public async Task RemoveFileAsync_移除文件的所有关联数�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");
        var symbols = new[] { CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class) };
        var references = new[] { CreateReference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts") };
        await _engine.UpdateFileAsync(file, symbols, references);

        await _engine.RemoveFileAsync("file:///a.ts");

        Assert.Equal(0, _engine.IndexManager.Symbols.Count);
        Assert.Equal(0, _engine.IndexManager.References.Count);
        Assert.Equal(0, _engine.IndexManager.Files.Count);
    }

    #endregion

    #region LoadFileAsync 测试

    [Fact]
    public async Task LoadFileAsync_按需加载符号和引�?)
    {
        var fileA = CreateFile("file:///a.ts", "hash1");
        var fileB = CreateFile("file:///b.ts", "hash2");
        await _engine.UpdateFileAsync(fileA, [CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class)], []);
        await _engine.UpdateFileAsync(fileB, [CreateSymbol("file:///b.ts", "Bar", SymbolKind.Function)], []);
        await _engine.SaveAsync();

        await _engine.DisposeAsync();

        var engine2 = new SymbolAnalysisEngine(_temp_path);
        await using (engine2)
        {
            await engine2.FastRestoreAsync();
            Assert.Equal(0, engine2.IndexManager.Symbols.Count);

            await engine2.LoadFileAsync("file:///a.ts");
            Assert.Equal(1, engine2.IndexManager.Symbols.Count);
            Assert.Equal("Foo", engine2.IndexManager.Symbols.GetByFileUri("file:///a.ts")[0].Name);
        }
    }

    #endregion

    #region 增量查询缓存失效测试

    [Fact]
    public async Task UpdateFileAsync_使相关查询缓存失�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");
        await _engine.UpdateFileAsync(file, [CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class)], []);

        var symbols1 = _engine.GetFileSymbols("file:///a.ts");
        Assert.Single(symbols1);

        await _engine.UpdateFileAsync(file with { ContentHash = "hash2" },
            [CreateSymbol("file:///a.ts", "Bar", SymbolKind.Function)], []);

        var symbols2 = _engine.GetFileSymbols("file:///a.ts");
        Assert.Single(symbols2);
        Assert.Equal("Bar", symbols2[0].Name);
    }

    #endregion

    #region 字段

    private string _temp_path = null!;
    private SymbolAnalysisEngine _engine = null!;

    #endregion

    #region IAsyncLifetime

    public async Task InitializeAsync()
    {
        _temp_path = Path.Combine(Path.GetTempPath(), $"nyar-sae-test-{Guid.NewGuid():N}");
        _engine = new SymbolAnalysisEngine(_temp_path);
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
        string refFileUri, ReferenceKind refKind = ReferenceKind.Read)
    {
        var record = new ReferenceRecord();
        record.SymbolId = SymbolId.Create(symbolFileUri, symbolName, symbolKind);
        record.FileUri = refFileUri;
        record.Location = Loc.Zero;
        record.Kind = refKind;
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

    #region UpdateFileAsync 测试

    [Fact]
    public async Task UpdateFileAsync更新索引和持久化()
    {
        var file = CreateFile("file:///a.ts", "hash1");
        var symbols = new[] { CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class) };

        await _engine.UpdateFileAsync(file, symbols, []);

        Assert.Equal(1, _engine.IndexManager.Symbols.Count);
        Assert.Equal(1, _engine.IndexManager.Files.Count);
    }

    [Fact]
    public async Task UpdateFileAsync_增量更新替换旧数�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");
        await _engine.UpdateFileAsync(file, [CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class)], []);

        await _engine.UpdateFileAsync(file with { ContentHash = "hash2" },
            [CreateSymbol("file:///a.ts", "Bar", SymbolKind.Function)], []);

        Assert.Equal(1, _engine.IndexManager.Symbols.Count);
        Assert.Equal("Bar", _engine.IndexManager.Symbols.GetByFileUri("file:///a.ts")[0].Name);
    }

    #endregion

    #region 查询 API 测试

    [Fact]
    public async Task FindReferences_返回指定符号的引�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");
        var symbols = new[] { CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class) };
        var references = new[]
        {
            CreateReference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts"),
            CreateReference("file:///a.ts", "Foo", SymbolKind.Class, "file:///c.ts")
        };
        await _engine.UpdateFileAsync(file, symbols, references);

        var symbolId = SymbolId.Create("file:///a.ts", "Foo", SymbolKind.Class);
        var result = _engine.FindReferences(symbolId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetFileSymbols_返回指定文件的符�?)
    {
        var file = CreateFile("file:///a.ts", "hash1");
        var symbols = new[]
        {
            CreateSymbol("file:///a.ts", "Foo", SymbolKind.Class),
            CreateSymbol("file:///a.ts", "bar", SymbolKind.Method)
        };
        await _engine.UpdateFileAsync(file, symbols, []);

        var result = _engine.GetFileSymbols("file:///a.ts");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetFileDependencies_返回文件的依�?)
    {
        var fileA = CreateFile("file:///a.ts", "hash1");
        var fileB = CreateFile("file:///b.ts", "hash2", ["file:///a.ts"]);
        await _engine.UpdateFileAsync(fileA, [], []);
        await _engine.UpdateFileAsync(fileB, [], []);

        var deps = _engine.GetFileDependencies("file:///b.ts");

        Assert.Contains("file:///a.ts", deps);
    }

    [Fact]
    public async Task GetFileDependents_返回传递反向依�?)
    {
        var fileA = CreateFile("file:///a.ts", "hash1");
        var fileB = CreateFile("file:///b.ts", "hash2", ["file:///a.ts"]);
        var fileC = CreateFile("file:///c.ts", "hash3", ["file:///b.ts"]);
        await _engine.UpdateFileAsync(fileA, [], []);
        await _engine.UpdateFileAsync(fileB, [], []);
        await _engine.UpdateFileAsync(fileC, [], []);

        var dependents = _engine.GetFileDependents("file:///a.ts");

        Assert.Contains("file:///b.ts", dependents);
        Assert.Contains("file:///c.ts", dependents);
    }

    #endregion
}
