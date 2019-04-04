using Nyar.Database;
using Nyar.Database.Index;
using Nyar.Types;

namespace Nyar.Tests.Database;

public sealed class IndexManagerTests
{
    #region RemoveFile 测试

    [Fact]
    public void RemoveFile_移除文件的所有关联数据()
    {
        var manager = new IndexManager();
        var file = create_file("file:///a.ts", "hash1");
        manager.UpdateFile(file,
            [create_symbol("file:///a.ts", "Foo", SymbolKind.Class)],
            [create_reference("file:///b.ts", "Bar", SymbolKind.Function, "file:///a.ts")]);

        manager.RemoveFile("file:///a.ts");

        Assert.Equal(0, manager.Symbols.Count);
        Assert.Equal(0, manager.References.Count);
        Assert.Equal(0, manager.Files.Count);
    }

    #endregion

    #region Clear 测试

    [Fact]
    public void Clear_清空所有索引()
    {
        var manager = new IndexManager();
        manager.UpdateFile(create_file("file:///a.ts", "hash1"),
            [create_symbol("file:///a.ts", "Foo", SymbolKind.Class)], []);
        manager.UpdateFile(create_file("file:///b.ts", "hash2"),
            [create_symbol("file:///b.ts", "Bar", SymbolKind.Function)], []);

        manager.Clear();

        Assert.Equal(0, manager.Symbols.Count);
        Assert.Equal(0, manager.References.Count);
        Assert.Equal(0, manager.Files.Count);
    }

    #endregion

    #region 辅助方法

    private static SymbolRecord create_symbol(string fileUri, string name, SymbolKind kind)
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

    private static ReferenceRecord create_reference(string symbolFileUri, string symbolName, SymbolKind symbolKind,
        string refFileUri)
    {
        var record = new ReferenceRecord();
        record.SymbolId = SymbolId.Create(symbolFileUri, symbolName, symbolKind);
        record.FileUri = refFileUri;
        record.Location = Loc.Zero;
        record.Kind = ReferenceKind.Read;
        return record;
    }

    private static FileRecord create_file(string uri, string hash, string[]? deps = null)
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

    #region UpdateFile 测试

    [Fact]
    public void UpdateFile_添加文件和符号引�?)
    {
        var manager = new IndexManager();
        var file = create_file("file:///a.ts", "hash1");
        var symbols = new[] { create_symbol("file:///a.ts", "Foo", SymbolKind.Class) };
        var references = new[] { create_reference("file:///b.ts", "Bar", SymbolKind.Function, "file:///a.ts") };

        manager.UpdateFile(file, symbols, references);

        Assert.Equal(1, manager.Symbols.Count);
        Assert.Equal(1, manager.References.Count);
        Assert.Equal(1, manager.Files.Count);
    }

    [Fact]
    public void UpdateFile_重复更新替换旧数�?)
    {
        var manager = new IndexManager();
        var file = create_file("file:///a.ts", "hash1");
        manager.UpdateFile(file, [create_symbol("file:///a.ts", "Foo", SymbolKind.Class)], []);

        manager.UpdateFile(file with { ContentHash = "hash2" },
            [create_symbol("file:///a.ts", "Bar", SymbolKind.Function)], []);

        Assert.Equal(1, manager.Symbols.Count);
        Assert.Empty(manager.Symbols.GetByFileUri("file:///a.ts").Where(s => s.Name == "Foo"));
        Assert.Single(manager.Symbols.GetByFileUri("file:///a.ts").Where(s => s.Name == "Bar"));
    }

    #endregion
}
