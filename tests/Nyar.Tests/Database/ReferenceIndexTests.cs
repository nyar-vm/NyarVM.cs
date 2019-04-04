using Nyar.Database;
using Nyar.Database.Index;
using Nyar.Types;

namespace Nyar.Tests.Database;

public sealed class ReferenceIndexTests
{
    #region 辅助方法

    private static ReferenceRecord create_reference(string symbolFileUri, string symbolName, SymbolKind symbolKind,
        string refFileUri, ReferenceKind refKind = ReferenceKind.Read)
    {
        var record = new ReferenceRecord();
        record.SymbolId = SymbolId.Create(symbolFileUri, symbolName, symbolKind);
        record.FileUri = refFileUri;
        record.Location = Loc.Zero;
        record.Kind = refKind;
        return record;
    }

    #endregion

    #region Clear 测试

    [Fact]
    public void Clear_清空所有数�?)
    {
        var index = new ReferenceIndex();
        index.Add(create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts"));

        index.Clear();

        Assert.Equal(0, index.Count);
        Assert.Empty(index.All);
    }

    #endregion

    #region Add 测试

    [Fact]
    public void Add_单条记录_计数�?()
    {
        var index = new ReferenceIndex();
        var reference = create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts");

        index.Add(reference);

        Assert.Equal(1, index.Count);
    }

    [Fact]
    public void AddRange_批量添加_计数正确()
    {
        var index = new ReferenceIndex();
        var references = new[]
        {
            create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts"),
            create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///c.ts"),
            create_reference("file:///d.ts", "Bar", SymbolKind.Function, "file:///b.ts")
        };

        index.AddRange(references);

        Assert.Equal(3, index.Count);
    }

    #endregion

    #region GetBySymbolId 测试

    [Fact]
    public void GetBySymbolId_返回指向该符号的所有引�?)
    {
        var index = new ReferenceIndex();
        var symbolId = SymbolId.Create("file:///a.ts", "Foo", SymbolKind.Class);
        index.Add(create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts"));
        index.Add(create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///c.ts"));
        index.Add(create_reference("file:///d.ts", "Bar", SymbolKind.Function, "file:///b.ts"));

        var result = index.GetBySymbolId(symbolId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetBySymbolId_不存在的符号_返回空列�?)
    {
        var index = new ReferenceIndex();
        var symbolId = SymbolId.Create("file:///a.ts", "Foo", SymbolKind.Class);

        var result = index.GetBySymbolId(symbolId);

        Assert.Empty(result);
    }

    #endregion

    #region GetByFileUri 测试

    [Fact]
    public void GetByFileUri_返回该文件中的所有引�?)
    {
        var index = new ReferenceIndex();
        index.Add(create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts"));
        index.Add(create_reference("file:///d.ts", "Bar", SymbolKind.Function, "file:///b.ts"));
        index.Add(create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///c.ts"));

        var result = index.GetByFileUri("file:///b.ts");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetByFileUri_不存在的文件_返回空列�?)
    {
        var index = new ReferenceIndex();

        var result = index.GetByFileUri("file:///nonexistent.ts");

        Assert.Empty(result);
    }

    #endregion

    #region RemoveByFileUri 测试

    [Fact]
    public void RemoveByFileUri_移除指定文件的所有引�?)
    {
        var index = new ReferenceIndex();
        index.Add(create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts"));
        index.Add(create_reference("file:///d.ts", "Bar", SymbolKind.Function, "file:///b.ts"));
        index.Add(create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///c.ts"));

        var removed = index.RemoveByFileUri("file:///b.ts");

        Assert.Equal(2, removed);
        Assert.Equal(1, index.Count);
    }

    [Fact]
    public void RemoveByFileUri_同时清理符号索引()
    {
        var index = new ReferenceIndex();
        var symbolId = SymbolId.Create("file:///a.ts", "Foo", SymbolKind.Class);
        index.Add(create_reference("file:///a.ts", "Foo", SymbolKind.Class, "file:///b.ts"));

        index.RemoveByFileUri("file:///b.ts");

        Assert.Empty(index.GetBySymbolId(symbolId));
    }

    #endregion
}
