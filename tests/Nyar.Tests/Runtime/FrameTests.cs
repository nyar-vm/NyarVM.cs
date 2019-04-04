namespace Nyar.Tests.Runtime;

public class FrameTests
{
    private static NyarFunction create_test_function(string name, int arity, int localCount, int codeOffset = 0,
        int codeLength = 10)
    {
        return new NyarFunction(name, arity, localCount, codeOffset, codeLength);
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var func = create_test_function("test", 2, 3, 100, 50);
        var frame = new Frame(func, 200, 10);

        Assert.Same(func, frame.Function);
        Assert.Equal(200, frame.ReturnPc);
        Assert.Equal(10, frame.StackBase);
        Assert.Equal(100, frame.Pc);
        Assert.Equal(5, frame.LocalCount);
    }

    [Fact]
    public void SetArguments_PopulatesLocals()
    {
        var func = create_test_function("add", 2, 1);
        var frame = new Frame(func, 0, 0);

        frame.SetArguments([Value.from_int(10), Value.from_int(20)]);

        Assert.Equal(10, frame.GetLocal(0).@int);
        Assert.Equal(20, frame.GetLocal(1).@int);
    }

    [Fact]
    public void SetArguments_MoreArgsThanArity_Truncates()
    {
        var func = create_test_function("add", 1, 0);
        var frame = new Frame(func, 0, 0);

        frame.SetArguments([Value.from_int(10), Value.from_int(20), Value.from_int(30)]);

        Assert.Equal(10, frame.GetLocal(0).@int);
    }

    [Fact]
    public void SetArguments_FewerArgsThanArity_PartialFill()
    {
        var func = create_test_function("add", 3, 0);
        var frame = new Frame(func, 0, 0);

        frame.SetArguments([Value.from_int(10)]);

        Assert.Equal(10, frame.GetLocal(0).@int);
    }

    [Fact]
    public void GetLocal_OutOfRange_Throws()
    {
        var func = create_test_function("test", 1, 1);
        var frame = new Frame(func, 0, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetLocal(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.GetLocal(-1));
    }

    [Fact]
    public void SetLocal_UpdatesValue()
    {
        var func = create_test_function("test", 0, 2);
        var frame = new Frame(func, 0, 0);

        frame.SetLocal(0, Value.from_int(42));
        frame.SetLocal(1, Value.from_bool(true));

        Assert.Equal(42, frame.GetLocal(0).@int);
        Assert.True(frame.GetLocal(1).@bool);
    }

    [Fact]
    public void SetLocal_OutOfRange_Throws()
    {
        var func = create_test_function("test", 0, 1);
        var frame = new Frame(func, 0, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => frame.SetLocal(5, Value.from_int(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.SetLocal(-1, Value.from_int(1)));
    }

    [Fact]
    public void Locals_ReturnsAllLocals()
    {
        var func = create_test_function("test", 2, 1);
        var frame = new Frame(func, 0, 0);

        frame.SetArguments([Value.from_int(1), Value.from_int(2)]);
        frame.SetLocal(2, Value.from_int(3));

        var locals = frame.Locals;
        Assert.Equal(3, locals.Length);
        Assert.Equal(1, locals[0].@int);
        Assert.Equal(2, locals[1].@int);
        Assert.Equal(3, locals[2].@int);
    }

    [Fact]
    public void Pc_CanBeModified()
    {
        var func = create_test_function("test", 0, 0, 0, 100);
        var frame = new Frame(func, 0, 0);

        Assert.Equal(0, frame.Pc);

        frame.Pc = 50;
        Assert.Equal(50, frame.Pc);
    }
}