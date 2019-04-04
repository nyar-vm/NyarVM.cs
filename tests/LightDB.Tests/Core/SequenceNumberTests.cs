namespace LightDB.Tests.Core;

public sealed class SequenceNumberTests
{
    [Fact]
    public void Zero_ShouldHaveValueZero()
    {
        Assert.Equal(0UL, SequenceNumber.Zero.Value);
    }

    [Fact]
    public void Invalid_ShouldHaveMaxValue()
    {
        Assert.Equal(ulong.MaxValue, SequenceNumber.Invalid.Value);
    }

    [Fact]
    public void Next_ShouldIncrementByOne()
    {
        var seq = new SequenceNumber(5);

        Assert.Equal(6UL, seq.Next.Value);
    }

    [Fact]
    public void Next_ShouldNotMutateOriginal()
    {
        var seq = new SequenceNumber(5);
        var next = seq.Next;

        Assert.Equal(5UL, seq.Value);
        Assert.Equal(6UL, next.Value);
    }

    [Fact]
    public void RecordStructEquality_ShouldWork()
    {
        var s1 = new SequenceNumber(42);
        var s2 = new SequenceNumber(42);

        Assert.Equal(s1, s2);
        Assert.True(s1 == s2);
    }

    [Fact]
    public void ToString_ShouldReturnValueAsString()
    {
        var seq = new SequenceNumber(100);

        Assert.Equal("100", seq.ToString());
    }
}