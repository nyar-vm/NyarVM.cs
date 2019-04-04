using Nyar.Database;
using Nyar.Database.Index;
using Nyar.Types;

namespace Nyar.Tests.Database;

public sealed class SymbolIndexTests
{
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

    #endregion

    #region Clear 测试

    [Fact]
    public void Clear_清空所有数�?)
    {
        var index = new SymbolIndex();
        index.Add(create_symbol("file:///a.ts", "Foo", SymbolKind.Class));
        index.Add(create_symbol("file:///b.ts", "Bar", SymbolKind.Function));

        index.Clear();

        Assert.Equal(0, index.Count);
        Assert.Empty(index.All);
    }

    #endregion

    #region Add 测试

    [Fact]
    public void Add_单条记录_计数�?()
    {
        var index = new SymbolIndex();
        var symbol = create_symbol("file:///a.ts", "Foo", SymbolKind.Class);

        index.Add(symbol);

        Assert.Equal(1, index.Count);
    }

    [Fact]
    public void Add_相同Id替换_计数不变()
    {
        var index = new SymbolIndex();
        var id = SymbolId.Create("file:///a.ts", "Foo", SymbolKind.Class);
        var symbol1 = new SymbolRecord();
        symbol1.Id = id;
        symbol1.Name = "Foo";
        symbol1.Kind = SymbolKind.Class;
        symbol1.FileUri = "file:///a.ts";
        symbol1.Location = Loc.Zero;
        var symbol2 = new SymbolRecord();
        symbol2.Id = id;
        symbol2.Name = "Foo";
        symbol2.Kind = SymbolKind.Class;
        symbol2.FileUri = "file:///a.ts";
        symbol2.Location = new Loc(10, 5);

        index.Add(symbol1);
        index.Add(symbol2);

        Assert.Equal(1, index.Count);
        Assert.True(index.TryGet(id, out var result));
        Assert.Equal(new Loc(10, 5), result.Location);
    }

    [Fact]
    public void AddRange_批量添加_计数正确()
    {
        var index = new SymbolIndex();
        var symbols = new[]
        {
            create_symbol("file:///a.ts", "Foo", SymbolKind.Class),
            create_symbol("file:///a.ts", "bar", SymbolKind.Method),
            create_symbol("file:///b.ts", "Baz", SymbolKind.Function)
        };

        index.AddRange(symbols);

        Assert.Equal(3, index.Count);
    }

    #endregion

    #region GetByFileUri 测试

    [Fact]
    public void GetByFileUri_返回指定文件的所有符�?)
    {
        var index = new SymbolIndex();
        index.Add(create_symbol("file:///a.ts", "Foo", SymbolKind.Class));
        index.Add(create_symbol("file:///a.ts", "bar", SymbolKind.Method));
        index.Add(create_symbol("file:///b.ts", "Baz", SymbolKind.Function));

        var result = index.GetByFileUri("file:///a.ts");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetByFileUri_不存在的文件_返回空列�?)
    {
        var index = new SymbolIndex();

        var result = index.GetByFileUri("file:///nonexistent.ts");

        Assert.Empty(result);
    }

    #endregion

    #region Remove 测试

    [Fact]
    public void Remove_存在的Id_返回true并移�?)
    {
        var index = new SymbolIndex();
        var id = SymbolId.Create("file:///a.ts", "Foo", SymbolKind.Class);
        index.Add(create_symbol("file:///a.ts", "Foo", SymbolKind.Class));

        var result = index.Remove(id);

        Assert.True(result);
        Assert.Equal(0, index.Count);
    }

    [Fact]
    public void Remove_不存在的Id_返回false()
    {
        var index = new SymbolIndex();
        var id = SymbolId.Create("file:///a.ts", "Foo", SymbolKind.Class);

        var result = index.Remove(id);

        Assert.False(result);
    }

    [Fact]
    public void RemoveByFileUri_移除指定文件所有符�?)
    {
        var index = new SymbolIndex();
        index.Add(create_symbol("file:///a.ts", "Foo", SymbolKind.Class));
        index.Add(create_symbol("file:///a.ts", "bar", SymbolKind.Method));
        index.Add(create_symbol("file:///b.ts", "Baz", SymbolKind.Function));

        var removed = index.RemoveByFileUri("file:///a.ts");

        Assert.Equal(2, removed);
        Assert.Equal(1, index.Count);
        Assert.Empty(index.GetByFileUri("file:///a.ts"));
    }

    #endregion

    #region TryGet 测试

    [Fact]
    public void TryGet_存在的Id_返回true和记�?)
    {
        var index = new SymbolIndex();
        var id = SymbolId.Create("file:///a.ts", "Foo", SymbolKind.Class);
        var symbol = create_symbol("file:///a.ts", "Foo", SymbolKind.Class);
        index.Add(symbol);

        var result = index.TryGet(id, out var record);

        Assert.True(result);
        Assert.Equal("Foo", record.Name);
        Assert.Equal(SymbolKind.Class, record.Kind);
    }

    [Fact]
    public void TryGet_不存在的Id_返回false()
    {
        var index = new SymbolIndex();
        var id = SymbolId.Create("file:///a.ts", "Foo", SymbolKind.Class);

        var result = index.TryGet(id, out _);

        Assert.False(result);
    }

    #endregion
}
