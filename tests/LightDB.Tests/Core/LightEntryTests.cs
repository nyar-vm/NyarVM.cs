using Std.Database.Core;

namespace LightDB.Tests.Core;

public sealed class DatabaseEntryTests
{
    [Fact]
    public void Empty_ShouldHaveEmptyKeyAndValue()
    {
        var entry = DatabaseEntry.empty;

        Assert.True(entry.is_empty);
        Assert.True(entry.key.is_empty);
        Assert.True(entry.value.is_empty);
    }

    [Fact]
    public void IsEmpty_WithNonEmptyKeyAndValue_ShouldReturnFalse()
    {
        var entry = new DatabaseEntry(
            DatabaseKey.from_string("key"),
            DatabaseValue.from_string("value"));

        Assert.False(entry.is_empty);
    }

    [Fact]
    public void GetValue_ShouldDeserializeValue()
    {
        var obj = new SampleObj { X = 10, Y = 20 };
        var entry = new DatabaseEntry(
            DatabaseKey.from_string("point"),
            DatabaseValue.from_object(obj));

        var result = entry.to_value<SampleObj>();

        Assert.NotNull(result);
        Assert.Equal(10, result.X);
        Assert.Equal(20, result.Y);
    }

    [Fact]
    public void RecordStructEquality_ShouldCompareByValue()
    {
        var e1 = new DatabaseEntry(DatabaseKey.from_string("k"), DatabaseValue.from_string("v"));
        var e2 = new DatabaseEntry(DatabaseKey.from_string("k"), DatabaseValue.from_string("v"));

        Assert.Equal(e1.Key.ToString(), e2.Key.ToString());
        Assert.Equal(e1.Value.ToString(), e2.Value.ToString());
    }

    private sealed class SampleObj
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}