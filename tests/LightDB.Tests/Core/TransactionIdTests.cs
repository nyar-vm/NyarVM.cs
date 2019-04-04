namespace LightDB.Tests.Core;

public sealed class TransactionIdTests
{
    [Fact]
    public void Min_ShouldHaveValueZero()
    {
        Assert.Equal(0UL, TransactionId.Min.Value);
    }

    [Fact]
    public void New_ShouldReturnUniqueIds()
    {
        var id1 = TransactionId.New();
        var id2 = TransactionId.New();

        Assert.NotEqual(id1, id2);
        Assert.True(id2.Value > id1.Value);
    }

    [Fact]
    public void New_ShouldReturnIncreasingIds()
    {
        var ids = new List<TransactionId>();
        for (var i = 0; i < 100; i++) ids.Add(TransactionId.New());

        for (var i = 1; i < ids.Count; i++) Assert.True(ids[i].Value > ids[i - 1].Value);
    }

    [Fact]
    public void RecordStructEquality_ShouldWork()
    {
        var id = new TransactionId(42);
        var same = new TransactionId(42);

        Assert.Equal(id, same);
        Assert.True(id == same);
    }

    [Fact]
    public void ToString_ShouldReturnValueAsString()
    {
        var id = new TransactionId(99);

        Assert.Equal("99", id.ToString());
    }
}