namespace Nyar.Tests.Runtime;

public class ValueStackTests
{
    [Fact]
    public void Push_Pop_BasicRoundTrip()
    {
        var stack = new ValueStack(16);
        stack.Push(Value.from_int(42));
        stack.Push(Value.from_bool(true));

        Assert.Equal(2, stack.Count);

        var b = stack.Pop();
        Assert.Equal(ValueType.@bool, b.type);
        Assert.True(b.@bool);

        var a = stack.Pop();
        Assert.Equal(ValueType.@int, a.type);
        Assert.Equal(42, a.@int);
    }

    [Fact]
    public void Pop_EmptyStack_Throws()
    {
        var stack = new ValueStack(16);
        Assert.Throws<InvalidOperationException>(() => stack.Pop());
    }

    [Fact]
    public void Peek_DoesNotRemove()
    {
        var stack = new ValueStack(16);
        stack.Push(Value.from_int(99));

        var peeked = stack.Peek();
        Assert.Equal(99, peeked.@int);
        Assert.Equal(1, stack.Count);
    }

    [Fact]
    public void Peek_EmptyStack_Throws()
    {
        var stack = new ValueStack(16);
        Assert.Throws<InvalidOperationException>(() => stack.Peek());
    }

    [Fact]
    public void Dup_CopiesTop()
    {
        var stack = new ValueStack(16);
        stack.Push(Value.from_int(7));
        stack.Dup();

        Assert.Equal(2, stack.Count);
        Assert.Equal(7, stack.Pop().@int);
        Assert.Equal(7, stack.Pop().@int);
    }

    [Fact]
    public void Dup_EmptyStack_Throws()
    {
        var stack = new ValueStack(16);
        Assert.Throws<InvalidOperationException>(() => stack.Dup());
    }

    [Fact]
    public void Swap_ExchangesTopTwo()
    {
        var stack = new ValueStack(16);
        stack.Push(Value.from_int(1));
        stack.Push(Value.from_int(2));
        stack.Swap();

        Assert.Equal(1, stack.Pop().@int);
        Assert.Equal(2, stack.Pop().@int);
    }

    [Fact]
    public void Swap_LessThanTwo_Throws()
    {
        var stack = new ValueStack(16);
        stack.Push(Value.from_int(1));
        Assert.Throws<InvalidOperationException>(() => stack.Swap());
    }

    [Fact]
    public void Push_BeyondCapacity_AutoGrows()
    {
        var stack = new ValueStack(2);
        stack.Push(Value.from_int(1));
        stack.Push(Value.from_int(2));
        stack.Push(Value.from_int(3));

        Assert.Equal(3, stack.Count);
        Assert.True(stack.Capacity > 2);
    }

    [Fact]
    public void GetAllValues_ReturnsBottomToTop()
    {
        var stack = new ValueStack(16);
        stack.Push(Value.from_int(10));
        stack.Push(Value.from_int(20));
        stack.Push(Value.from_int(30));

        var values = stack.GetAllValues().ToList();
        Assert.Equal(3, values.Count);
        Assert.Equal(10, values[0].@int);
        Assert.Equal(20, values[1].@int);
        Assert.Equal(30, values[2].@int);
    }

    [Fact]
    public void MixedTypes_IntBoolNull()
    {
        var stack = new ValueStack(16);
        stack.Push(Value.from_int(1));
        stack.Push(Value.from_bool(false));
        stack.Push(Value.@null);

        Assert.Equal(3, stack.Count);

        var n = stack.Pop();
        Assert.Equal(ValueType.@null, n.type);

        var b = stack.Pop();
        Assert.Equal(ValueType.@bool, b.type);
        Assert.False(b.@bool);

        var i = stack.Pop();
        Assert.Equal(ValueType.@int, i.type);
        Assert.Equal(1, i.@int);
    }

    [Fact]
    public void DoubleValue_PreservesBits()
    {
        var stack = new ValueStack(16);
        stack.Push(Value.from_double(2.5));

        var d = stack.Pop();
        Assert.Equal(2.5, d.@double);
    }
}