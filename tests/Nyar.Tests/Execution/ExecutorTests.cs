using Nyar.Types;

namespace Nyar.Tests.Execution;

public class ExecutorTests
{
    #region 控制流

    [Fact]
    public void Jump_UnconditionalForward()
    {
        var constants = new List<Value> { Value.from_int(42), Value.from_int(99) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 0);
        var jumpPc = bc.position;
        var jumpOperandPos = bc.position + 1;
        bc.emit(NyarHeadCode.Jump, 0);
        bc.emit(NyarHeadCode.Const, 1);
        var retPc = bc.position;
        bc.emit(NyarHeadCode.Return);

        var offset = retPc - jumpPc;
        bc.patch(jumpOperandPos, offset);

        var func = new NyarFunction("jump_test", 0, 0, 0, bc.position);
        var module = create_module("jump_test", bc.to_array(), func, constants);

        var result = execute(module, "jump_test");
        Assert.Equal(42, result.@int);
    }

    #endregion

    #region 函数调用

    [Fact]
    public void Call_InvokesFunction()
    {
        var addBc = new BytecodeBuilder();
        addBc.emit(NyarHeadCode.LoadArg, 0);
        addBc.emit(NyarHeadCode.LoadArg, 1);
        addBc.emit(NyarHeadCode.I32Add);
        addBc.emit(NyarHeadCode.Return);
        var addBytes = addBc.to_array();
        var addFunc = new NyarFunction("add", 2, 0, 0, addBytes.Length);

        var mainBc = new BytecodeBuilder();
        mainBc.emit(NyarHeadCode.Const, 0);
        mainBc.emit(NyarHeadCode.Const, 1);
        mainBc.emit(NyarHeadCode.Call, 1);
        mainBc.emit(NyarHeadCode.Return);
        var mainBytes = mainBc.to_array();

        var constants = new List<Value> { Value.from_int(3), Value.from_int(4) };
        var mainFunc = new NyarFunction("main", 0, 0, addBytes.Length, mainBytes.Length);

        var allBytecode = new byte[addBytes.Length + mainBytes.Length];
        Array.Copy(addBytes, 0, allBytecode, 0, addBytes.Length);
        Array.Copy(mainBytes, 0, allBytecode, addBytes.Length, mainBytes.Length);

        var module = create_module("call_test", allBytecode, mainFunc, constants);
        addFunc.Module = module;
        module.functions.Add(addFunc);

        var result = execute(module, "main");
        Assert.Equal(7, result.@int);
    }

    #endregion

    #region 类型转换

    [Fact]
    public void I32ToF64S_Conversion()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.I32ToF64S);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("i32tof64", 1, 0, 0, bc.position);
        var module = create_module("conv_test", bc.to_array(), func);

        var result = execute(module, "i32tof64", Value.from_int(42));
        Assert.Equal(42.0, result.@double);
    }

    #endregion

    #region 内置函数

    [Fact]
    public void MathSqrt_ComputesSquareRoot()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.I32ToF64S);
        bc.emit(NyarHeadCode.F64Sqrt);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("sqrt", 1, 0, 0, bc.position);
        var module = create_module("sqrt_test", bc.to_array(), func);

        var result = execute(module, "sqrt", Value.from_int(16));
        Assert.Equal(4.0, result.@double);
    }

    #endregion

    #region 全局变量

    [Fact]
    public void LoadGlobal_StoreGlobal_RoundTrip()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.StoreGlobal, 0);
        bc.emit(NyarHeadCode.LoadGlobal, 0);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("global_test", 1, 0, 0, bc.position);
        var module = create_module("global_test", bc.to_array(), func);

        var result = execute(module, "global_test", Value.from_int(100));
        Assert.Equal(100, result.@int);
    }

    #endregion

    #region 异常处理

    [Fact]
    public void Throw_Uncaught_ThrowsRuntimeException()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Throw);

        var func = new NyarFunction("throw_test", 0, 0, 0, bc.position);
        var module = create_module("throw_test", bc.to_array(), func);

        Assert.Throws<NyarRuntimeException>(() => execute(module, "throw_test"));
    }

    #endregion

    #region 边界情况

    [Fact]
    public void EmptyFunction_ReturnsNull()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("empty", 0, 0, 0, bc.position);
        var module = create_module("empty_test", bc.to_array(), func);

        var result = execute(module, "empty");
        Assert.Equal(ValueType.@null, result.type);
    }

    #endregion

    #region BytecodeBuilder

    private sealed class BytecodeBuilder
    {
        private readonly List<byte> _bytes = [];

        public int position => _bytes.Count;

        public BytecodeBuilder emit(NyarHeadCode op)
        {
            _bytes.Add((byte)op);
            return this;
        }

        public BytecodeBuilder emit(NyarHeadCode op, int operand)
        {
            _bytes.Add((byte)op);
            _bytes.AddRange(BitConverter.GetBytes(operand));
            return this;
        }

        public byte[] to_array()
        {
            return _bytes.ToArray();
        }

        public void patch(int offset, int value)
        {
            var patched = BitConverter.GetBytes(value);
            for (var i = 0; i < 4; i++) _bytes[offset + i] = patched[i];
        }
    }

    #endregion

    #region 辅助方法

    private static NyarModule create_module(string name, byte[] bytecode, NyarFunction func,
        List<Value>? constants = null)
    {
        var module = new NyarModule(name)
        {
            constants = constants ?? [],
            functions = [func],
            raw_bytecode = bytecode
        };
        func.Module = module;
        return module;
    }

    private static Value execute(NyarModule module, string functionName, params Value[] args)
    {
        var vm = new NyarVM();
        vm.Load(module);
        return vm.Run(module.name, functionName, args);
    }

    #endregion

    #region 栈操作

    [Fact]
    public void Const_LoadsConstant()
    {
        var constants = new List<Value> { Value.from_int(42), Value.from_int(99) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 1);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("test", 0, 0, 0, bc.position);
        var module = create_module("const_test", bc.to_array(), func, constants);

        var result = execute(module, "test");
        Assert.Equal(99, result.@int);
    }

    [Fact]
    public void Pop_RemovesTop()
    {
        var constants = new List<Value> { Value.from_int(10), Value.from_int(20) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 0);
        bc.emit(NyarHeadCode.Const, 1);
        bc.emit(NyarHeadCode.Pop);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("test", 0, 0, 0, bc.position);
        var module = create_module("pop_test", bc.to_array(), func, constants);

        var result = execute(module, "test");
        Assert.Equal(10, result.@int);
    }

    [Fact]
    public void Dup_DuplicatesTop()
    {
        var constants = new List<Value> { Value.from_int(5) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 0);
        bc.emit(NyarHeadCode.Dup);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("test", 0, 0, 0, bc.position);
        var module = create_module("dup_test", bc.to_array(), func, constants);

        var result = execute(module, "test");
        Assert.Equal(10, result.@int);
    }

    [Fact]
    public void Swap_ExchangesTopTwo()
    {
        var constants = new List<Value> { Value.from_int(10), Value.from_int(20) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 0);
        bc.emit(NyarHeadCode.Const, 1);
        bc.emit(NyarHeadCode.Swap);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("test", 0, 0, 0, bc.position);
        var module = create_module("swap_test", bc.to_array(), func, constants);

        var result = execute(module, "test");
        Assert.Equal(10, result.@int);
    }

    #endregion

    #region i32 算术

    [Fact]
    public void I32Add_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("add", 2, 0, 0, bc.position);
        var module = create_module("i32add_test", bc.to_array(), func);

        var result = execute(module, "add", Value.from_int(3), Value.from_int(4));
        Assert.Equal(7, result.@int);
    }

    [Fact]
    public void I32Sub_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.I32Sub);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("sub", 2, 0, 0, bc.position);
        var module = create_module("i32sub_test", bc.to_array(), func);

        var result = execute(module, "sub", Value.from_int(10), Value.from_int(3));
        Assert.Equal(7, result.@int);
    }

    [Fact]
    public void I32Mul_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.I32Mul);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("mul", 2, 0, 0, bc.position);
        var module = create_module("i32mul_test", bc.to_array(), func);

        var result = execute(module, "mul", Value.from_int(6), Value.from_int(7));
        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void I32DivS_SignedDivision()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.I32DivS);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("div", 2, 0, 0, bc.position);
        var module = create_module("i32divs_test", bc.to_array(), func);

        var result = execute(module, "div", Value.from_int(20), Value.from_int(4));
        Assert.Equal(5, result.@int);
    }

    [Fact]
    public void I32And_BitwiseAnd()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.I32And);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("and", 2, 0, 0, bc.position);
        var module = create_module("i32and_test", bc.to_array(), func);

        var result = execute(module, "and", Value.from_int(0b1100), Value.from_int(0b1010));
        Assert.Equal(0b1000, result.@int);
    }

    [Fact]
    public void I32Or_BitwiseOr()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.I32Or);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("or", 2, 0, 0, bc.position);
        var module = create_module("i32or_test", bc.to_array(), func);

        var result = execute(module, "or", Value.from_int(0b1100), Value.from_int(0b1010));
        Assert.Equal(0b1110, result.@int);
    }

    #endregion

    #region i32 比较

    [Fact]
    public void I32Eq_Equal()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.I32Eq);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("eq", 2, 0, 0, bc.position);
        var module = create_module("i32eq_test", bc.to_array(), func);

        var resultTrue = execute(module, "eq", Value.from_int(5), Value.from_int(5));
        Assert.True(resultTrue.@bool);

        var resultFalse = execute(module, "eq", Value.from_int(5), Value.from_int(3));
        Assert.False(resultFalse.@bool);
    }

    [Fact]
    public void I32GtS_SignedGreaterThan()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.I32GtS);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("gt", 2, 0, 0, bc.position);
        var module = create_module("i32gts_test", bc.to_array(), func);

        var resultTrue = execute(module, "gt", Value.from_int(5), Value.from_int(3));
        Assert.True(resultTrue.@bool);

        var resultFalse = execute(module, "gt", Value.from_int(3), Value.from_int(5));
        Assert.False(resultFalse.@bool);
    }

    #endregion

    #region 局部变量

    [Fact]
    public void LoadLocal_StoreLocal_RoundTrip()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.StoreLocal, 0);
        bc.emit(NyarHeadCode.LoadLocal, 0);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("local_test", 1, 1, 0, bc.position);
        var module = create_module("local_test", bc.to_array(), func);

        var result = execute(module, "local_test", Value.from_int(77));
        Assert.Equal(77, result.@int);
    }

    [Fact]
    public void LoadArg_MultipleArgs()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.LoadArg, 2);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("sum3", 3, 0, 0, bc.position);
        var module = create_module("loadarg_test", bc.to_array(), func);

        var result = execute(module, "sum3", Value.from_int(10), Value.from_int(20), Value.from_int(30));
        Assert.Equal(60, result.@int);
    }

    #endregion
}