using Std.Category;

namespace Sonic.Testing.Core;

/// <summary>
///     Option{T} 代数数据类型的单元测试。
/// </summary>
public class OptionTests
{
    /// <summary>
    ///     测试 Option.Some 创建包含值的实例。
    /// </summary>
    [Fact]
    public void Some_CreatesSomeInstance()
    {
        var option = Option<int>.some(42);

        Assert.True(option.is_some);
        Assert.False(option.is_none);
        Assert.Equal(42, option.value);
    }

    /// <summary>
    ///     测试 Option.None 创建空实例。
    /// </summary>
    [Fact]
    public void None_CreatesNoneInstance()
    {
        var option = Option<int>.none;

        Assert.False(option.is_some);
        Assert.True(option.is_none);
    }

    /// <summary>
    ///     测试不同值的 Some 创建不同的实例。
    /// </summary>
    [Fact]
    public void Some_DifferentValues_CreateDifferentInstances()
    {
        var option1 = Option<string>.some("hello");
        var option2 = Option<string>.some("world");

        Assert.True(option1.is_some);
        Assert.True(option2.is_some);
        Assert.Equal("hello", option1.value);
        Assert.Equal("world", option2.value);
    }

    /// <summary>
    ///     测试 None 对于不同类型是相同的。
    /// </summary>
    [Fact]
    public void None_DifferentTypes_AreAllNone()
    {
        var intNone = Option<int>.none;
        var stringNone = Option<string>.none;

        Assert.True(intNone.is_none);
        Assert.True(stringNone.is_none);
    }

    /// <summary>
    ///     测试 Some 包装引用类型。
    /// </summary>
    [Fact]
    public void Some_ReferenceType_Works()
    {
        var obj = new object();
        var option = Option<object>.some(obj);

        Assert.True(option.is_some);
        Assert.Same(obj, option.value);
    }

    /// <summary>
    ///     测试 Match 对 Some 调用 some 函数。
    /// </summary>
    [Fact]
    public void Match_Some_CallsSomeFunc()
    {
        var option = Option<int>.some(42);

        var result = option.match(v => v * 2, () => -1);

        Assert.Equal(84, result);
    }

    /// <summary>
    ///     测试 Match 对 None 调用 none 函数。
    /// </summary>
    [Fact]
    public void Match_None_CallsNoneFunc()
    {
        var option = Option<int>.none;

        var result = option.match(v => v * 2, () => -1);

        Assert.Equal(-1, result);
    }

    /// <summary>
    ///     测试 Map 组合子。
    /// </summary>
    [Fact]
    public void Map_Some_AppliesMapper()
    {
        var option = Option<int>.some(5);

        var result = option.map(x => x * 2);

        Assert.True(result.is_some);
        Assert.Equal(10, result.value);
    }

    /// <summary>
    ///     测试 Map 对 None 返回 None。
    /// </summary>
    [Fact]
    public void Map_None_ReturnsNone()
    {
        var option = Option<int>.none;

        var result = option.map(x => x * 2);

        Assert.True(result.is_none);
    }

    /// <summary>
    ///     测试 Bind 对 Some 应用绑定函数。
    /// </summary>
    [Fact]
    public void Bind_Some_AppliesBinder()
    {
        var option = Option<int>.some(10);

        var result = option.bind(x => Option<string>.some($"值: {x}"));

        Assert.True(result.is_some);
        Assert.Equal("值: 10", result.value);
    }

    /// <summary>
    ///     测试 Bind 对 None 返回 None。
    /// </summary>
    [Fact]
    public void Bind_None_ReturnsNone()
    {
        var option = Option<int>.none;

        var result = option.bind(x => Option<string>.some($"值: {x}"));

        Assert.True(result.is_none);
    }

    /// <summary>
    ///     测试 UnwrapOr 对 Some 返回值。
    /// </summary>
    [Fact]
    public void UnwrapOr_Some_ReturnsValue()
    {
        var option = Option<int>.some(99);

        var result = option.unwrap_or(0);

        Assert.Equal(99, result);
    }

    /// <summary>
    ///     测试 UnwrapOr 对 None 返回默认值。
    /// </summary>
    [Fact]
    public void UnwrapOr_None_ReturnsDefault()
    {
        var option = Option<int>.none;

        var result = option.unwrap_or(42);

        Assert.Equal(42, result);
    }

    /// <summary>
    ///     测试 UnwrapOrDefault 对 Some 返回值。
    /// </summary>
    [Fact]
    public void UnwrapOrDefault_Some_ReturnsValue()
    {
        var option = Option<string>.some("hello");

        var result = option.unwrap_or_default();

        Assert.Equal("hello", result);
    }

    /// <summary>
    ///     测试 UnwrapOrDefault 对 None 返回类型默认值。
    /// </summary>
    [Fact]
    public void UnwrapOrDefault_None_ReturnsTypeDefault()
    {
        var option = Option<int>.none;

        var result = option.unwrap_or_default();

        Assert.Equal(0, result);
    }

    /// <summary>
    ///     测试 Filter 对满足条件的 Some 保留原值。
    /// </summary>
    [Fact]
    public void Filter_Some_PredicateMatch_ReturnsSome()
    {
        var option = Option<int>.some(10);

        var result = option.filter(x => x > 5);

        Assert.True(result.is_some);
        Assert.Equal(10, result.value);
    }

    /// <summary>
    ///     测试 Filter 对不满足条件的 Some 返回 None。
    /// </summary>
    [Fact]
    public void Filter_Some_PredicateMismatch_ReturnsNone()
    {
        var option = Option<int>.some(3);

        var result = option.filter(x => x > 5);

        Assert.True(result.is_none);
    }

    /// <summary>
    ///     测试 Filter 对 None 返回 None。
    /// </summary>
    [Fact]
    public void Filter_None_ReturnsNone()
    {
        var option = Option<int>.none;

        var result = option.filter(x => x > 5);

        Assert.True(result.is_none);
    }

    /// <summary>
    ///     测试 UnwrapOrElse 对 Some 返回值。
    /// </summary>
    [Fact]
    public void UnwrapOrElse_Some_ReturnsValue()
    {
        var option = Option<int>.some(7);

        var result = option.unwrap_or_else(() => 100);

        Assert.Equal(7, result);
    }

    /// <summary>
    ///     测试 UnwrapOrElse 对 None 调用工厂函数。
    /// </summary>
    [Fact]
    public void UnwrapOrElse_None_UsesFactory()
    {
        var option = Option<int>.none;

        var result = option.unwrap_or_else(() => 100);

        Assert.Equal(100, result);
    }

    /// <summary>
    ///     测试 OrElse 对 None 使用备选 Option。
    /// </summary>
    [Fact]
    public void OrElse_None_ReturnsAlternative()
    {
        var option = Option<int>.none;

        var result = option.or_else(() => Option<int>.some(55));

        Assert.True(result.is_some);
        Assert.Equal(55, result.value);
    }

    /// <summary>
    ///     测试 OrElse 对 Some 返回自身。
    /// </summary>
    [Fact]
    public void OrElse_Some_ReturnsSelf()
    {
        var option = Option<int>.some(33);

        var result = option.or_else(() => Option<int>.some(55));

        Assert.True(result.is_some);
        Assert.Equal(33, result.value);
    }

    /// <summary>
    ///     测试 Or 对 None 返回备选。
    /// </summary>
    [Fact]
    public void Or_None_ReturnsAlternative()
    {
        var option = Option<int>.none;

        var result = option.or(Option<int>.some(88));

        Assert.True(result.is_some);
        Assert.Equal(88, result.value);
    }

    /// <summary>
    ///     测试 Zip 合并两个 Some。
    /// </summary>
    [Fact]
    public void Zip_BothSome_CombinesValues()
    {
        var a = Option<int>.some(3);
        var b = Option<string>.some("items");

        var result = a.zip(b, (x, y) => $"{x} {y}");

        Assert.True(result.is_some);
        Assert.Equal("3 items", result.value);
    }

    /// <summary>
    ///     测试 Zip 任一为 None 返回 None。
    /// </summary>
    [Fact]
    public void Zip_OneNone_ReturnsNone()
    {
        var a = Option<int>.some(3);
        var b = Option<string>.none;

        var result = a.zip(b, (x, y) => $"{x} {y}");

        Assert.True(result.is_none);
    }

    /// <summary>
    ///     测试 Flatten 展平嵌套 Option。
    /// </summary>
    [Fact]
    public void Flatten_SomeOfSome_ReturnsInner()
    {
        var nested = Option<Option<int>>.some(Option<int>.some(42));

        var result = Option<int>.flatten(nested);

        Assert.True(result.is_some);
        Assert.Equal(42, result.value);
    }

    /// <summary>
    ///     测试 Flatten 对 Some(None) 返回 None。
    /// </summary>
    [Fact]
    public void Flatten_SomeOfNone_ReturnsNone()
    {
        var nested = Option<Option<int>>.some(Option<int>.none);

        var result = Option<int>.flatten(nested);

        Assert.True(result.is_none);
    }

    /// <summary>
    ///     测试 Inspect 对 Some 执行副作用。
    /// </summary>
    [Fact]
    public void Inspect_Some_ExecutesAction()
    {
        var option = Option<int>.some(5);
        var captured = 0;

        var result = option.inspect(x => captured = x);

        Assert.Equal(option, result);
        Assert.Equal(5, captured);
    }

    /// <summary>
    ///     测试 Inspect 对 None 不执行副作用。
    /// </summary>
    [Fact]
    public void Inspect_None_DoesNotExecuteAction()
    {
        var option = Option<int>.none;
        var captured = 0;

        var result = option.inspect(x => captured = x);

        Assert.True(result.is_none);
        Assert.Equal(0, captured);
    }

    /// <summary>
    ///     测试 Value 属性对 None 抛出异常。
    /// </summary>
    [Fact]
    public void Value_None_ThrowsInvalidOperationException()
    {
        var option = Option<int>.none;

        Assert.Throws<InvalidOperationException>(() => option.value);
    }

    /// <summary>
    ///     测试相等性比较。
    /// </summary>
    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = Option<int>.some(42);
        var b = Option<int>.some(42);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    /// <summary>
    ///     测试不相等性比较。
    /// </summary>
    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = Option<int>.some(42);
        var b = Option<int>.some(99);

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    /// <summary>
    ///     测试 None 之间的相等性。
    /// </summary>
    [Fact]
    public void Equality_BothNone_AreEqual()
    {
        var a = Option<int>.none;
        var b = Option<int>.none;

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    /// <summary>
    ///     测试 ToString 输出。
    /// </summary>
    [Fact]
    public void ToString_Some_ReturnsSomeWithValue()
    {
        var option = Option<int>.some(42);

        Assert.Equal("Some(42)", option.ToString());
    }

    /// <summary>
    ///     测试 ToString 对 None 输出。
    /// </summary>
    [Fact]
    public void ToString_None_ReturnsNone()
    {
        var option = Option<int>.none;

        Assert.Equal("None", option.ToString());
    }
}