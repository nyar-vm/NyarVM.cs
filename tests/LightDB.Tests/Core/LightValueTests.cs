using Std.Database.Core;

namespace LightDB.Tests.Core;

public sealed class DatabaseValueTests
{
    [Fact]
    public void FromString_ShouldCreateValueWithUtf8Bytes()
    {
        var value = DatabaseValue.from_string("hello");

        Assert.Equal(5, value.Length);
        Assert.False(value.is_empty);
    }

    [Fact]
    public void FromInt32_ShouldCreateValueWith4Bytes()
    {
        var value = DatabaseValue.from_int32(42);

        Assert.Equal(4, value.Length);
        Assert.False(value.is_empty);
    }

    [Fact]
    public void FromInt64_ShouldCreateValueWith8Bytes()
    {
        var value = DatabaseValue.from_int64(123456L);

        Assert.Equal(8, value.Length);
        Assert.False(value.is_empty);
    }

    [Fact]
    public void FromDouble_ShouldCreateValueWith8Bytes()
    {
        var value = DatabaseValue.from_double(3.14);

        Assert.Equal(8, value.Length);
        Assert.False(value.is_empty);
    }

    [Fact]
    public void FromObject_ShouldSerializeToJson()
    {
        var obj = new TestData { Name = "test", Age = 25 };
        var value = DatabaseValue.from_object(obj);

        Assert.False(value.is_empty);
    }

    [Fact]
    public void ToObject_ShouldDeserializeFromJson()
    {
        var obj = new TestData { Name = "test", Age = 25 };
        var value = DatabaseValue.from_object(obj);

        var result = value.to_object<TestData>();

        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(25, result.Age);
    }

    [Fact]
    public void ToObject_WithEmptyValue_ShouldReturnDefault()
    {
        var result = DatabaseValue.empty.to_object<TestData>();

        Assert.Null(result);
    }

    [Fact]
    public void Empty_ShouldHaveZeroLength()
    {
        Assert.Equal(0, DatabaseValue.empty.Length);
        Assert.True(DatabaseValue.empty.IsEmpty);
    }

    [Fact]
    public void ToString_ShouldReturnUtf8DecodedString()
    {
        var value = DatabaseValue.from_string("hello");

        Assert.Equal("hello", value.ToString());
    }

    [Fact]
    public void Roundtrip_WithComplexObject_ShouldPreserveData()
    {
        var obj = new ComplexData
        {
            Id = Guid.NewGuid(),
            Name = "测试中文",
            Tags = ["a", "b", "c"],
            Metadata = new Dictionary<string, string> { { "key1", "value1" } }
        };

        var value = DatabaseValue.from_object(obj);
        var result = value.to_object<ComplexData>();

        Assert.NotNull(result);
        Assert.Equal(obj.Id, result.Id);
        Assert.Equal(obj.Name, result.Name);
        Assert.Equal(obj.Tags, result.Tags);
    }

    [Fact]
    public void RecordStructEquality_ShouldCompareByValue()
    {
        var v1 = DatabaseValue.from_string("test");
        var v2 = DatabaseValue.from_string("test");

        Assert.Equal(v1.ToString(), v2.ToString());
        Assert.True(v1.bytes.Span.SequenceEqual(v2.bytes.Span));
    }

    private sealed class TestData
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private sealed class ComplexData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public List<string> Tags { get; set; } = [];
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}