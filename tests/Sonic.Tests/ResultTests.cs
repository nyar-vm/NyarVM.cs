using Std.Category;

namespace Sonic.Testing.Core;

/// <summary>
///     Result{T, E} 代数数据类型的单元测试。
/// </summary>
public class ResultTests
{
    /// <summary>
    ///     测试 Result.Ok 创建成功实例。
    /// </summary>
    [Fact]
    public void Ok_CreatesOkInstance()
    {
        var result = Result<int, string>.ok(42);

        Assert.True(result.is_ok);
        Assert.False(result.is_error);
        Assert.Equal(42, result.unwrap());
    }

    /// <summary>
    ///     测试 Result.Error 创建失败实例。
    /// </summary>
    [Fact]
    public void Error_CreatesErrorInstance()
    {
        var result = Result<int, string>.error("错误信息");

        Assert.False(result.is_ok);
        Assert.True(result.is_error);
        Assert.Equal("错误信息", result.unwrap_error());
    }

    /// <summary>
    ///     测试不同值的 Ok 创建不同的实例。
    /// </summary>
    [Fact]
    public void Ok_DifferentValues_CreateDifferentInstances()
    {
        var result1 = Result<string, int>.ok("成功1");
        var result2 = Result<string, int>.ok("成功2");

        Assert.True(result1.is_ok);
        Assert.True(result2.is_ok);
        Assert.Equal("成功1", result1.unwrap());
        Assert.Equal("成功2", result2.unwrap());
    }

    /// <summary>
    ///     测试不同错误的 Error 创建不同的实例。
    /// </summary>
    [Fact]
    public void Error_DifferentErrors_CreateDifferentInstances()
    {
        var result1 = Result<string, int>.error(404);
        var result2 = Result<string, int>.error(500);

        Assert.True(result1.is_error);
        Assert.True(result2.is_error);
        Assert.Equal(404, result1.unwrap_error());
        Assert.Equal(500, result2.unwrap_error());
    }

    /// <summary>
    ///     测试 Ok 和 Error 之间正确区分。
    /// </summary>
    [Fact]
    public void Ok_And_Error_AreMutuallyExclusive()
    {
        var success = Result<double, string>.ok(3.14);
        var failure = Result<double, string>.error("除数不能为零");

        Assert.True(success.is_ok);
        Assert.False(success.is_error);

        Assert.False(failure.is_ok);
        Assert.True(failure.is_error);
    }

    /// <summary>
    ///     测试 Ok 包装引用类型。
    /// </summary>
    [Fact]
    public void Ok_ReferenceType_Works()
    {
        var obj = new object();
        var result = Result<object, string>.ok(obj);

        Assert.True(result.is_ok);
        Assert.Same(obj, result.unwrap());
    }

    /// <summary>
    ///     测试 Map 对 Ok 应用映射函数。
    /// </summary>
    [Fact]
    public void Map_Ok_AppliesMapper()
    {
        var result = Result<int, string>.ok(5);

        var mapped = result.map(x => x * 2);

        Assert.True(mapped.is_ok);
        Assert.Equal(10, mapped.unwrap());
    }

    /// <summary>
    ///     测试 Map 对 Error 保持原错误。
    /// </summary>
    [Fact]
    public void Map_Error_PreservesError()
    {
        var result = Result<int, string>.error("错误");

        var mapped = result.map(x => x * 2);

        Assert.True(mapped.is_error);
        Assert.Equal("错误", mapped.unwrap_error());
    }

    /// <summary>
    ///     测试 MapError 对 Error 应用错误映射函数。
    /// </summary>
    [Fact]
    public void MapError_Error_AppliesMapper()
    {
        var result = Result<int, string>.error("not_found");

        var mapped = result.map_error(e => $"错误码: {e}");

        Assert.True(mapped.is_error);
        Assert.Equal("错误码: not_found", mapped.unwrap_error());
    }

    /// <summary>
    ///     测试 MapError 对 Ok 保持原值。
    /// </summary>
    [Fact]
    public void MapError_Ok_PreservesValue()
    {
        var result = Result<int, string>.ok(100);

        var mapped = result.map_error(e => $"错误码: {e}");

        Assert.True(mapped.is_ok);
        Assert.Equal(100, mapped.unwrap());
    }

    /// <summary>
    ///     测试 AndThen 对 Ok 应用绑定函数。
    /// </summary>
    [Fact]
    public void AndThen_Ok_AppliesBinder()
    {
        var result = Result<int, string>.ok(10);

        var bound = result.and_then(x => Result<string, string>.ok($"值: {x}"));

        Assert.True(bound.is_ok);
        Assert.Equal("值: 10", bound.unwrap());
    }

    /// <summary>
    ///     测试 AndThen 对 Error 保持原错误。
    /// </summary>
    [Fact]
    public void AndThen_Error_PreservesError()
    {
        var result = Result<int, string>.error("失败");

        var bound = result.and_then(x => Result<string, string>.ok($"值: {x}"));

        Assert.True(bound.is_error);
        Assert.Equal("失败", bound.unwrap_error());
    }

    /// <summary>
    ///     测试 UnwrapOr 对 Ok 返回值。
    /// </summary>
    [Fact]
    public void UnwrapOr_Ok_ReturnsValue()
    {
        var result = Result<int, string>.ok(99);

        var value = result.unwrap_or(0);

        Assert.Equal(99, value);
    }

    /// <summary>
    ///     测试 UnwrapOr 对 Error 返回默认值。
    /// </summary>
    [Fact]
    public void UnwrapOr_Error_ReturnsDefault()
    {
        var result = Result<int, string>.error("错误");

        var value = result.unwrap_or(42);

        Assert.Equal(42, value);
    }

    /// <summary>
    ///     测试 UnwrapOrDefault 对 Error 返回类型默认值。
    /// </summary>
    [Fact]
    public void UnwrapOrDefault_Error_ReturnsTypeDefault()
    {
        var result = Result<int, string>.error("错误");

        var value = result.unwrap_or_default();

        Assert.Equal(0, value);
    }

    /// <summary>
    ///     测试 UnwrapOrElse 对 Ok 返回值。
    /// </summary>
    [Fact]
    public void UnwrapOrElse_Ok_ReturnsValue()
    {
        var result = Result<int, string>.ok(7);

        var value = result.unwrap_or_else(e => e.Length);

        Assert.Equal(7, value);
    }

    /// <summary>
    ///     测试 UnwrapOrElse 对 Error 调用工厂函数。
    /// </summary>
    [Fact]
    public void UnwrapOrElse_Error_UsesFactory()
    {
        var result = Result<int, string>.error("错误信息");

        var value = result.unwrap_or_else(e => e.Length);

        Assert.Equal(4, value);
    }

    /// <summary>
    ///     测试 Fold 对 Ok 调用 onOk。
    /// </summary>
    [Fact]
    public void Fold_Ok_CallsOnOk()
    {
        var result = Result<int, string>.ok(5);

        var folded = result.fold(
            v => $"成功: {v}",
            e => $"失败: {e}");

        Assert.Equal("成功: 5", folded);
    }

    /// <summary>
    ///     测试 Fold 对 Error 调用 onError。
    /// </summary>
    [Fact]
    public void Fold_Error_CallsOnError()
    {
        var result = Result<int, string>.error("超时");

        var folded = result.fold(
            v => $"成功: {v}",
            e => $"失败: {e}");

        Assert.Equal("失败: 超时", folded);
    }

    /// <summary>
    ///     测试 OrElse 对 Error 使用备选 Result。
    /// </summary>
    [Fact]
    public void OrElse_Error_ReturnsAlternative()
    {
        var result = Result<int, string>.error("原始错误");

        var alternative = result.or_else(() => Result<int, string>.ok(55));

        Assert.True(alternative.is_ok);
        Assert.Equal(55, alternative.unwrap());
    }

    /// <summary>
    ///     测试 OrElse 对 Ok 返回自身。
    /// </summary>
    [Fact]
    public void OrElse_Ok_ReturnsSelf()
    {
        var result = Result<int, string>.ok(33);

        var alternative = result.or_else(() => Result<int, string>.ok(55));

        Assert.True(alternative.is_ok);
        Assert.Equal(33, alternative.unwrap());
    }

    /// <summary>
    ///     测试 Inspect 对 Ok 执行副作用。
    /// </summary>
    [Fact]
    public void Inspect_Ok_ExecutesAction()
    {
        var result = Result<int, string>.ok(5);
        var captured = 0;

        var returned = result.inspect(x => captured = x);

        Assert.Equal(result, returned);
        Assert.Equal(5, captured);
    }

    /// <summary>
    ///     测试 InspectError 对 Error 执行副作用。
    /// </summary>
    [Fact]
    public void InspectError_Error_ExecutesAction()
    {
        var result = Result<int, string>.error("出错");
        var captured = string.Empty;

        var returned = result.inspect_error(e => captured = e);

        Assert.Equal(result, returned);
        Assert.Equal("出错", captured);
    }

    /// <summary>
    ///     测试 Ok 方法将成功状态转换为 Some。
    /// </summary>
    [Fact]
    public void OkMethod_Ok_ReturnsSome()
    {
        var result = Result<int, string>.ok(42);

        var option = result.ok();

        Assert.True(option.is_some);
        Assert.Equal(42, option.value);
    }

    /// <summary>
    ///     测试 Ok 方法将错误状态转换为 None。
    /// </summary>
    [Fact]
    public void OkMethod_Error_ReturnsNone()
    {
        var result = Result<int, string>.error("错误");

        var option = result.ok();

        Assert.True(option.is_none);
    }

    /// <summary>
    ///     测试 Err 将错误状态转换为 Some。
    /// </summary>
    [Fact]
    public void Err_Error_ReturnsSome()
    {
        var result = Result<int, string>.error("错误码");

        var option = result.err();

        Assert.True(option.is_some);
        Assert.Equal("错误码", option.value);
    }

    /// <summary>
    ///     测试 Err 将成功状态转换为 None。
    /// </summary>
    [Fact]
    public void Err_Ok_ReturnsNone()
    {
        var result = Result<int, string>.ok(42);

        var option = result.err();

        Assert.True(option.is_none);
    }

    /// <summary>
    ///     测试 Unwrap 对 Error 抛出异常。
    /// </summary>
    [Fact]
    public void Unwrap_Error_ThrowsInvalidOperationException()
    {
        var result = Result<int, string>.error("错误");

        var ex = Assert.Throws<InvalidOperationException>(() => result.unwrap());
        Assert.Contains("错误", ex.Message);
    }

    /// <summary>
    ///     测试 UnwrapError 对 Ok 抛出异常。
    /// </summary>
    [Fact]
    public void UnwrapError_Ok_ThrowsInvalidOperationException()
    {
        var result = Result<int, string>.ok(42);

        Assert.Throws<InvalidOperationException>(() => result.unwrap_error());
    }

    /// <summary>
    ///     测试相等性比较。
    /// </summary>
    [Fact]
    public void Equality_SameOkValues_AreEqual()
    {
        var a = Result<int, string>.ok(42);
        var b = Result<int, string>.ok(42);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    /// <summary>
    ///     测试不相等性比较。
    /// </summary>
    [Fact]
    public void Equality_DifferentOkValues_AreNotEqual()
    {
        var a = Result<int, string>.ok(42);
        var b = Result<int, string>.ok(99);

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    /// <summary>
    ///     测试 ToString 输出。
    /// </summary>
    [Fact]
    public void ToString_Ok_ReturnsOkWithValue()
    {
        var result = Result<int, string>.ok(42);

        Assert.Equal("Ok(42)", result.ToString());
    }

    /// <summary>
    ///     测试 ToString 对 Error 输出。
    /// </summary>
    [Fact]
    public void ToString_Error_ReturnsErrorWithError()
    {
        var result = Result<int, string>.error("失败");

        Assert.Equal("Error(失败)", result.ToString());
    }
}