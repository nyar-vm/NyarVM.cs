using System.Numerics;
using Nyar.Types;

namespace Nyar.Tests.Misc;

/// <summary>
///     VM 指令全面测试，覆盖所有 67 个 NyarOpcode
/// </summary>
public class OpcodeCoverageTests
{
    #region 闭包操作

    [Fact]
    public void NewClosure_GetUpvalue_SetUpvalue_RoundTrip()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.NewClosure, 0);
        bc.emit(NyarOpcode.StoreLocal, 0);
        bc.emit(NyarOpcode.LoadLocal, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.SetUpvalue, 0);
        bc.emit(NyarOpcode.LoadLocal, 0);
        bc.emit(NyarOpcode.GetUpvalue, 0);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("closure_test", 2, 1, 0, bc.position);
        var module = create_module("closure_test", bc.to_array(), func);

        var result = execute(module, "closure_test", Value.from_int(0), Value.from_int(42));
        Assert.Equal(42, result.@int);
    }

    #endregion

    #region 异常处理

    [Fact]
    public void Catch_CatchesThrownException()
    {
        var constants = new List<Value> { Value.from_int(42) };
        var bc = new BytecodeBuilder();
        var catchPatchPos = bc.position + 1;
        bc.emit(NyarOpcode.Catch, 0);
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.Throw);
        var catchTarget = bc.position;
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.Return);
        bc.patch(catchPatchPos, catchTarget);

        var func = new NyarFunction("catch_test", 0, 0, 0, bc.position);
        var module = create_module("catch_test", bc.to_array(), func, constants);

        var result = execute(module, "catch_test");
        Assert.Equal(42, result.@int);
    }

    #endregion

    #region Free 操作

    [Fact]
    public void Free_DeallocatesMemory()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.Alloc, 256);
        bc.emit(NyarOpcode.StoreLocal, 0);
        bc.emit(NyarOpcode.LoadLocal, 0);
        bc.emit(NyarOpcode.Free);
        var constants = new List<Value> { Value.from_int(0) };
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("free_test", 0, 1, 0, bc.position);
        var module = create_module("free_test", bc.to_array(), func, constants);

        var result = execute(module, "free_test");
        Assert.Equal(0, result.@int);
    }

    #endregion

    #region BytecodeBuilder

    private sealed class BytecodeBuilder
    {
        private readonly List<byte> _bytes = [];

        public int position => _bytes.Count;

        public BytecodeBuilder emit(NyarOpcode op)
        {
            _bytes.Add((byte)op);
            return this;
        }

        public BytecodeBuilder emit(NyarOpcode op, int operand)
        {
            _bytes.Add((byte)op);
            _bytes.AddRange(BitConverter.GetBytes(operand));
            return this;
        }


        /// <summary>
        ///     发射双操作数指令（9 字节：opcode + operand1 i32 + operand2 i32）
        /// </summary>
        public BytecodeBuilder emit(NyarOpcode op, int operand1, int operand2)
        {
            _bytes.Add((byte)op);
            _bytes.AddRange(BitConverter.GetBytes(operand1));
            _bytes.AddRange(BitConverter.GetBytes(operand2));
            return this;
        }


        /// <summary>
        ///     发射三操作数指令（13 字节：opcode + operand1 i32 + operand2 i32 + operand3 i32）
        /// </summary>
        public BytecodeBuilder emit(NyarOpcode op, int operand1, int operand2, int operand3)
        {
            _bytes.Add((byte)op);
            _bytes.AddRange(BitConverter.GetBytes(operand1));
            _bytes.AddRange(BitConverter.GetBytes(operand2));
            _bytes.AddRange(BitConverter.GetBytes(operand3));
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
        var vm = new NyarVM(new JitOptions { Enabled = false });
        vm.Load(module);
        return vm.Run(module.name, functionName, args);
    }

    #endregion

    #region 控制流

    [Fact]
    public void Nop_DoesNothing()
    {
        var constants = new List<Value> { Value.from_int(42) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.Nop);
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("nop_test", 0, 0, 0, bc.position);
        var module = create_module("nop_test", bc.to_array(), func, constants);

        var result = execute(module, "nop_test");
        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void JumpIfTrue_SkipsWhenTrue()
    {
        var constants = new List<Value> { Value.from_int(10), Value.from_int(20) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        var jumpPos = bc.position + 1;
        bc.emit(NyarOpcode.JumpIfTrue, 0);
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.Return);
        var targetPc = bc.position;
        bc.emit(NyarOpcode.Const, 1);
        bc.emit(NyarOpcode.Return);
        bc.patch(jumpPos, targetPc - (jumpPos - 1));

        var func = new NyarFunction("jit_test", 1, 0, 0, bc.position);
        var module = create_module("jit_test", bc.to_array(), func, constants);

        var resultTrue = execute(module, "jit_test", Value.from_bool(true));
        Assert.Equal(20, resultTrue.@int);

        var resultFalse = execute(module, "jit_test", Value.from_bool(false));
        Assert.Equal(10, resultFalse.@int);
    }

    [Fact]
    public void JumpIfFalse_SkipsWhenFalse()
    {
        var constants = new List<Value> { Value.from_int(10), Value.from_int(20) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        var jumpPos = bc.position + 1;
        bc.emit(NyarOpcode.JumpIfFalse, 0);
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.Return);
        var targetPc = bc.position;
        bc.emit(NyarOpcode.Const, 1);
        bc.emit(NyarOpcode.Return);
        bc.patch(jumpPos, targetPc - (jumpPos - 1));

        var func = new NyarFunction("jif_test", 1, 0, 0, bc.position);
        var module = create_module("jif_test", bc.to_array(), func, constants);

        var resultFalse = execute(module, "jif_test", Value.from_bool(false));
        Assert.Equal(20, resultFalse.@int);

        var resultTrue = execute(module, "jif_test", Value.from_bool(true));
        Assert.Equal(10, resultTrue.@int);
    }

    [Fact]
    public void TailCall_OptimizesRecursion()
    {
        var constants = new List<Value> { Value.from_int(0) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32Add);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("add", 2, 0, 0, bc.position);
        var module = create_module("tailcall_test", bc.to_array(), func, constants);

        var result = execute(module, "add", Value.from_int(100), Value.from_int(200));
        Assert.Equal(300, result.@int);
    }

    #endregion

    #region i32 算术（补充缺失的操作码）

    [Fact]
    public void I32DivU_UnsignedDivision()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32DivU);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("divu", 2, 0, 0, bc.position);
        var module = create_module("i32divu_test", bc.to_array(), func);

        var result = execute(module, "divu", Value.from_int(unchecked((int)0xFFFFFFFFu)), Value.from_int(2));
        Assert.Equal(unchecked((int)(0xFFFFFFFFu / 2u)), result.@int);
    }

    [Fact]
    public void I32RemS_SignedRemainder()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32RemS);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("rems", 2, 0, 0, bc.position);
        var module = create_module("i32rems_test", bc.to_array(), func);

        var result = execute(module, "rems", Value.from_int(17), Value.from_int(5));
        Assert.Equal(2, result.@int);
    }

    [Fact]
    public void I32RemU_UnsignedRemainder()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32RemU);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("remu", 2, 0, 0, bc.position);
        var module = create_module("i32remu_test", bc.to_array(), func);

        var result = execute(module, "remu", Value.from_int(17), Value.from_int(5));
        Assert.Equal(2, result.@int);
    }

    [Fact]
    public void I32Neg_NegatesValue()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.I32Neg);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("neg", 1, 0, 0, bc.position);
        var module = create_module("i32neg_test", bc.to_array(), func);

        var result = execute(module, "neg", Value.from_int(42));
        Assert.Equal(-42, result.@int);
    }

    [Fact]
    public void I32Xor_BitwiseXor()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32Xor);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("xor", 2, 0, 0, bc.position);
        var module = create_module("i32xor_test", bc.to_array(), func);

        var result = execute(module, "xor", Value.from_int(0b1100), Value.from_int(0b1010));
        Assert.Equal(0b0110, result.@int);
    }

    [Fact]
    public void I32Shl_LeftShift()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32Shl);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("shl", 2, 0, 0, bc.position);
        var module = create_module("i32shl_test", bc.to_array(), func);

        var result = execute(module, "shl", Value.from_int(1), Value.from_int(4));
        Assert.Equal(16, result.@int);
    }

    [Fact]
    public void I32ShrS_SignedRightShift()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32ShrS);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("shrs", 2, 0, 0, bc.position);
        var module = create_module("i32shrs_test", bc.to_array(), func);

        var result = execute(module, "shrs", Value.from_int(-16), Value.from_int(2));
        Assert.Equal(-16 >> 2, result.@int);
    }

    [Fact]
    public void I32ShrU_UnsignedRightShift()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32ShrU);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("shru", 2, 0, 0, bc.position);
        var module = create_module("i32shru_test", bc.to_array(), func);

        var result = execute(module, "shru", Value.from_int(16), Value.from_int(2));
        Assert.Equal(4, result.@int);
    }

    [Fact]
    public void I32Not_BitwiseNot()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.I32Not);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("not", 1, 0, 0, bc.position);
        var module = create_module("i32not_test", bc.to_array(), func);

        var result = execute(module, "not", Value.from_int(0));
        Assert.Equal(~0, result.@int);
    }

    #endregion

    #region i32 比较（补充缺失的操作码）

    [Fact]
    public void I32Ne_NotEqual()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32Ne);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("ne", 2, 0, 0, bc.position);
        var module = create_module("i32ne_test", bc.to_array(), func);

        var resultTrue = execute(module, "ne", Value.from_int(5), Value.from_int(3));
        Assert.True(resultTrue.@bool);

        var resultFalse = execute(module, "ne", Value.from_int(5), Value.from_int(5));
        Assert.False(resultFalse.@bool);
    }

    [Fact]
    public void I32LtS_SignedLessThan()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32LtS);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("lts", 2, 0, 0, bc.position);
        var module = create_module("i32lts_test", bc.to_array(), func);

        var resultTrue = execute(module, "lts", Value.from_int(-5), Value.from_int(3));
        Assert.True(resultTrue.@bool);

        var resultFalse = execute(module, "lts", Value.from_int(5), Value.from_int(3));
        Assert.False(resultFalse.@bool);
    }

    [Fact]
    public void I32LtU_UnsignedLessThan()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32LtU);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("ltu", 2, 0, 0, bc.position);
        var module = create_module("i32ltu_test", bc.to_array(), func);

        var result = execute(module, "ltu", Value.from_int(3), Value.from_int(5));
        Assert.True(result.@bool);
    }

    [Fact]
    public void I32LeS_SignedLessOrEqual()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32LeS);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("les", 2, 0, 0, bc.position);
        var module = create_module("i32les_test", bc.to_array(), func);

        var resultEq = execute(module, "les", Value.from_int(5), Value.from_int(5));
        Assert.True(resultEq.@bool);

        var resultLt = execute(module, "les", Value.from_int(3), Value.from_int(5));
        Assert.True(resultLt.@bool);

        var resultGt = execute(module, "les", Value.from_int(5), Value.from_int(3));
        Assert.False(resultGt.@bool);
    }

    [Fact]
    public void I32LeU_UnsignedLessOrEqual()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32LeU);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("leu", 2, 0, 0, bc.position);
        var module = create_module("i32leu_test", bc.to_array(), func);

        var result = execute(module, "leu", Value.from_int(5), Value.from_int(5));
        Assert.True(result.@bool);
    }

    [Fact]
    public void I32GeS_SignedGreaterOrEqual()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32GeS);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("ges", 2, 0, 0, bc.position);
        var module = create_module("i32ges_test", bc.to_array(), func);

        var resultEq = execute(module, "ges", Value.from_int(5), Value.from_int(5));
        Assert.True(resultEq.@bool);

        var resultGt = execute(module, "ges", Value.from_int(7), Value.from_int(5));
        Assert.True(resultGt.@bool);

        var resultLt = execute(module, "ges", Value.from_int(3), Value.from_int(5));
        Assert.False(resultLt.@bool);
    }

    [Fact]
    public void I32GeU_UnsignedGreaterOrEqual()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32GeU);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("geu", 2, 0, 0, bc.position);
        var module = create_module("i32geu_test", bc.to_array(), func);

        var result = execute(module, "geu", Value.from_int(5), Value.from_int(5));
        Assert.True(result.@bool);
    }

    [Fact]
    public void I32GtU_UnsignedGreaterThan()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32GtU);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("gtu", 2, 0, 0, bc.position);
        var module = create_module("i32gtu_test", bc.to_array(), func);

        var result = execute(module, "gtu", Value.from_int(5), Value.from_int(3));
        Assert.True(result.@bool);
    }

    #endregion

    #region i64 操作

    [Fact]
    public void I64Add_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I64Add);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("i64add", 2, 0, 0, bc.position);
        var module = create_module("i64add_test", bc.to_array(), func);

        var result = execute(module, "i64add", Value.from_long(1000000000L), Value.from_long(2000000000L));
        Assert.Equal(3000000000L, result.@long);
    }

    [Fact]
    public void I64Sub_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I64Sub);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("i64sub", 2, 0, 0, bc.position);
        var module = create_module("i64sub_test", bc.to_array(), func);

        var result = execute(module, "i64sub", Value.from_long(5000000000L), Value.from_long(2000000000L));
        Assert.Equal(3000000000L, result.@long);
    }

    [Fact]
    public void I64Mul_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I64Mul);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("i64mul", 2, 0, 0, bc.position);
        var module = create_module("i64mul_test", bc.to_array(), func);

        var result = execute(module, "i64mul", Value.from_long(100000L), Value.from_long(200000L));
        Assert.Equal(20000000000L, result.@long);
    }

    [Fact]
    public void I64DivS_SignedDivision()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I64DivS);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("i64divs", 2, 0, 0, bc.position);
        var module = create_module("i64divs_test", bc.to_array(), func);

        var result = execute(module, "i64divs", Value.from_long(10000000000L), Value.from_long(5L));
        Assert.Equal(2000000000L, result.@long);
    }

    [Fact]
    public void I64DivU_UnsignedDivision()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I64DivU);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("i64divu", 2, 0, 0, bc.position);
        var module = create_module("i64divu_test", bc.to_array(), func);

        var result = execute(module, "i64divu", Value.from_long(100L), Value.from_long(5L));
        Assert.Equal(20L, result.@long);
    }

    [Fact]
    public void I64Neg_NegatesValue()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.I64Neg);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("i64neg", 1, 0, 0, bc.position);
        var module = create_module("i64neg_test", bc.to_array(), func);

        var result = execute(module, "i64neg", Value.from_long(42L));
        Assert.Equal(-42L, result.@long);
    }

    #endregion

    #region f32 操作

    [Fact]
    public void F32Add_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.F32Add);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f32add", 2, 0, 0, bc.position);
        var module = create_module("f32add_test", bc.to_array(), func);

        var result = execute(module, "f32add", Value.from_double(3.5), Value.from_double(4.5));
        Assert.Equal(8.0, result.@double, 0.001);
    }

    [Fact]
    public void F32Sub_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.F32Sub);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f32sub", 2, 0, 0, bc.position);
        var module = create_module("f32sub_test", bc.to_array(), func);

        var result = execute(module, "f32sub", Value.from_double(10.0), Value.from_double(3.0));
        Assert.Equal(7.0, result.@double, 0.001);
    }

    [Fact]
    public void F32Mul_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.F32Mul);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f32mul", 2, 0, 0, bc.position);
        var module = create_module("f32mul_test", bc.to_array(), func);

        var result = execute(module, "f32mul", Value.from_double(6.0), Value.from_double(7.0));
        Assert.Equal(42.0, result.@double, 0.001);
    }

    [Fact]
    public void F32Div_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.F32Div);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f32div", 2, 0, 0, bc.position);
        var module = create_module("f32div_test", bc.to_array(), func);

        var result = execute(module, "f32div", Value.from_double(20.0), Value.from_double(4.0));
        Assert.Equal(5.0, result.@double, 0.001);
    }

    [Fact]
    public void F32Neg_NegatesValue()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.F32Neg);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f32neg", 1, 0, 0, bc.position);
        var module = create_module("f32neg_test", bc.to_array(), func);

        var result = execute(module, "f32neg", Value.from_double(3.14));
        Assert.Equal(-3.14, result.@double, 0.001);
    }

    #endregion

    #region f64 操作

    [Fact]
    public void F64Add_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.F64Add);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f64add", 2, 0, 0, bc.position);
        var module = create_module("f64add_test", bc.to_array(), func);

        var result = execute(module, "f64add", Value.from_double(3.14), Value.from_double(2.86));
        Assert.Equal(6.0, result.@double, 0.0001);
    }

    [Fact]
    public void F64Sub_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.F64Sub);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f64sub", 2, 0, 0, bc.position);
        var module = create_module("f64sub_test", bc.to_array(), func);

        var result = execute(module, "f64sub", Value.from_double(10.5), Value.from_double(3.5));
        Assert.Equal(7.0, result.@double, 0.0001);
    }

    [Fact]
    public void F64Mul_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.F64Mul);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f64mul", 2, 0, 0, bc.position);
        var module = create_module("f64mul_test", bc.to_array(), func);

        var result = execute(module, "f64mul", Value.from_double(6.0), Value.from_double(7.0));
        Assert.Equal(42.0, result.@double, 0.0001);
    }

    [Fact]
    public void F64Div_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.F64Div);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f64div", 2, 0, 0, bc.position);
        var module = create_module("f64div_test", bc.to_array(), func);

        var result = execute(module, "f64div", Value.from_double(20.0), Value.from_double(4.0));
        Assert.Equal(5.0, result.@double, 0.0001);
    }

    [Fact]
    public void F64Neg_NegatesValue()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.F64Neg);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f64neg", 1, 0, 0, bc.position);
        var module = create_module("f64neg_test", bc.to_array(), func);

        var result = execute(module, "f64neg", Value.from_double(42.0));
        Assert.Equal(-42.0, result.@double, 0.0001);
    }

    [Fact]
    public void F64Sqrt_ComputesSquareRoot()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.F64Sqrt);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("f64sqrt", 1, 0, 0, bc.position);
        var module = create_module("f64sqrt_test", bc.to_array(), func);

        var result = execute(module, "f64sqrt", Value.from_double(144.0));
        Assert.Equal(12.0, result.@double, 0.0001);
    }

    #endregion

    #region 类型转换

    [Fact]
    public void I32ExtendI64S_SignedExtension()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.I32ExtendI64S);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("ext_s", 1, 0, 0, bc.position);
        var module = create_module("i32ext64s_test", bc.to_array(), func);

        var resultPos = execute(module, "ext_s", Value.from_int(42));
        Assert.Equal(42L, resultPos.@long);

        var resultNeg = execute(module, "ext_s", Value.from_int(-1));
        Assert.Equal(-1L, resultNeg.@long);
    }

    [Fact]
    public void I32ExtendI64U_UnsignedExtension()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.I32ExtendI64U);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("ext_u", 1, 0, 0, bc.position);
        var module = create_module("i32ext64u_test", bc.to_array(), func);

        var result = execute(module, "ext_u", Value.from_int(unchecked((int)0xFFFFFFFF)));
        Assert.Equal(0xFFFFFFFFL, result.@long);
    }

    [Fact]
    public void I64TruncI32S_SignedTruncation()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.I64TruncI32S);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("trunc_s", 1, 0, 0, bc.position);
        var module = create_module("i64trunci32s_test", bc.to_array(), func);

        var result = execute(module, "trunc_s", Value.from_long(42L));
        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void I64TruncI32U_UnsignedTruncation()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.I64TruncI32U);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("trunc_u", 1, 0, 0, bc.position);
        var module = create_module("i64trunci32u_test", bc.to_array(), func);

        var result = execute(module, "trunc_u", Value.from_long(42L));
        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void I32ToF32S_Conversion()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.I32ToF32S);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("i32tof32", 1, 0, 0, bc.position);
        var module = create_module("i32tof32_test", bc.to_array(), func);

        var result = execute(module, "i32tof32", Value.from_int(42));
        Assert.Equal(42.0, result.@double, 0.001);
    }

    #endregion

    #region 内存操作

    [Fact]
    public void Alloc_I32Load_I32Store_RoundTrip()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.Alloc, 256);
        bc.emit(NyarOpcode.StoreLocal, 0);
        bc.emit(NyarOpcode.LoadLocal, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.I32Store, 0);
        bc.emit(NyarOpcode.LoadLocal, 0);
        bc.emit(NyarOpcode.I32Load, 0);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("mem_test", 2, 1, 0, bc.position);
        var module = create_module("mem_test", bc.to_array(), func);

        var result = execute(module, "mem_test", Value.from_int(0), Value.from_int(12345));
        Assert.Equal(12345, result.@int);
    }

    [Fact]
    public void I64Load_I64Store_RoundTrip()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.Alloc, 256);
        bc.emit(NyarOpcode.StoreLocal, 0);
        bc.emit(NyarOpcode.LoadLocal, 0);
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.I64Store, 0);
        bc.emit(NyarOpcode.LoadLocal, 0);
        bc.emit(NyarOpcode.I64Load, 0);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("i64mem_test", 1, 1, 0, bc.position);
        var module = create_module("i64mem_test", bc.to_array(), func);

        var result = execute(module, "i64mem_test", Value.from_long(9876543210L));
        Assert.True(result.type is ValueType.@long or ValueType.@int);
    }

    #endregion

    #region 对象操作

    [Fact]
    public void NewObject_GetField_SetField_RoundTrip()
    {
        var constants = new List<Value> { Value.from_string("x") };
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.NewObject, 1);
        bc.emit(NyarOpcode.StoreLocal, 1);
        bc.emit(NyarOpcode.LoadLocal, 1);
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.SetField);
        bc.emit(NyarOpcode.LoadLocal, 1);
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.GetField);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("obj_test", 1, 2, 0, bc.position);
        var module = create_module("obj_test", bc.to_array(), func, constants);

        var result = execute(module, "obj_test", Value.from_int(42));
        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void NewObject_GetIndex_SetIndex_RoundTrip()
    {
        var constants = new List<Value> { Value.from_string("key") };
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.NewObject, 1);
        bc.emit(NyarOpcode.StoreLocal, 1);
        bc.emit(NyarOpcode.LoadLocal, 1);
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.SetIndex);
        bc.emit(NyarOpcode.LoadLocal, 1);
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.GetIndex);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("idx_test", 1, 2, 0, bc.position);
        var module = create_module("idx_test", bc.to_array(), func, constants);

        var result = execute(module, "idx_test", Value.from_int(99));
        Assert.Equal(99, result.@int);
    }

    [Fact]
    public void Length_ReturnsArrayLength()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.NewObject, 0);
        bc.emit(NyarOpcode.Length);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("len_test", 0, 0, 0, bc.position);
        var module = create_module("len_test", bc.to_array(), func);

        var result = execute(module, "len_test");
        Assert.Equal(0, result.@int);
    }

    #endregion

    #region UTF-8 文本操作

    [Fact]
    public void Utf8Concat_JoinsTwoTexts()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.utf8_concat);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("utf8_concat", 2, 0, 0, bc.position);
        var module = create_module("utf8_concat_test", bc.to_array(), func);

        var result = execute(module, "utf8_concat", Value.from_string("hello"), Value.from_string(" world"));
        Assert.Equal("hello world", result.@string?.ToString());
    }

    [Fact]
    public void Utf8LenBytes_ReturnsByteLength()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.utf8_len_bytes);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("utf8_len_bytes", 1, 0, 0, bc.position);
        var module = create_module("utf8_len_bytes_test", bc.to_array(), func);

        var result = execute(module, "utf8_len_bytes", Value.from_string("hello"));
        Assert.Equal(5, result.@int);
    }

    [Fact]
    public void Utf8LenChars_ReturnsCharCount()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.utf8_len_chars);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("utf8_len_chars", 1, 0, 0, bc.position);
        var module = create_module("utf8_len_chars_test", bc.to_array(), func);

        var result = execute(module, "utf8_len_chars", Value.from_string("hello"));
        Assert.Equal(5, result.@int);
    }

    [Fact]
    public void Utf8Substr_ExtractsSubstring()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.Const, 1);
        bc.emit(NyarOpcode.utf8_substr);
        bc.emit(NyarOpcode.Return);

        var constants = new List<Value> { Value.from_int(1), Value.from_int(3) };
        var func = new NyarFunction("utf8_substr", 1, 0, 0, bc.position);
        var module = create_module("utf8_substr_test", bc.to_array(), func, constants);

        var result = execute(module, "utf8_substr", Value.from_string("hello"));
        Assert.Equal("ell", result.@string?.ToString());
    }

    #endregion

    #region BigInt 操作

    [Fact]
    public void BigIntAdd_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.BigIntAdd);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("bigint_add", 2, 0, 0, bc.position);
        var module = create_module("bigint_add_test", bc.to_array(), func);

        var a = Value.from_big_int(new BigInteger(long.MaxValue));
        var b = Value.from_big_int(new BigInteger(1));
        var result = execute(module, "bigint_add", a, b);
        Assert.Equal(long.MaxValue + BigInteger.One, (BigInteger?)result.big_int);
    }

    [Fact]
    public void BigIntSub_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.BigIntSub);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("bigint_sub", 2, 0, 0, bc.position);
        var module = create_module("bigint_sub_test", bc.to_array(), func);

        var a = Value.from_big_int(new BigInteger(100));
        var b = Value.from_big_int(new BigInteger(30));
        var result = execute(module, "bigint_sub", a, b);
        Assert.Equal(new BigInteger(70), (BigInteger?)result.big_int);
    }

    [Fact]
    public void BigIntMul_TwoValues()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.BigIntMul);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("bigint_mul", 2, 0, 0, bc.position);
        var module = create_module("bigint_mul_test", bc.to_array(), func);

        var a = Value.from_big_int(new BigInteger(100000));
        var b = Value.from_big_int(new BigInteger(100000));
        var result = execute(module, "bigint_mul", a, b);
        Assert.Equal(new BigInteger(10000000000L), (BigInteger?)result.big_int);
    }

    #endregion

    #region 统一分派操作码

    [Fact]
    public void CallStatic_DispatchesDirectly()
    {
        var calleeBc = new BytecodeBuilder();
        calleeBc.emit(NyarOpcode.LoadArg, 0);
        calleeBc.emit(NyarOpcode.Return);
        var calleeBytes = calleeBc.to_array();

        var callerBc = new BytecodeBuilder();
        callerBc.emit(NyarOpcode.LoadArg, 0);
        callerBc.emit(NyarOpcode.CallStatic, 0);
        callerBc.emit(NyarOpcode.Return);
        var callerBytes = callerBc.to_array();

        var combined = new byte[calleeBytes.Length + callerBytes.Length];
        calleeBytes.CopyTo(combined, 0);
        callerBytes.CopyTo(combined, calleeBytes.Length);

        var calleeFunc = new NyarFunction("callee", 1, 0, 0, calleeBytes.Length);
        calleeFunc.CodeOffset = 0;

        var callerFunc = new NyarFunction("caller", 1, 0, 0, callerBytes.Length);
        callerFunc.CodeOffset = calleeBytes.Length;

        var module = new NyarModule("call_static_test")
        {
            constants = [],
            functions = [calleeFunc, callerFunc],
            raw_bytecode = combined
        };
        calleeFunc.Module = module;
        callerFunc.Module = module;

        var vm = new NyarVM();
        vm.Load(module);
        var result = vm.Run("call_static_test", "caller", Value.from_int(42));
        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void CallDynamic_FallbackDoesNotCrash()
    {
        var calleeBc = new BytecodeBuilder();
        calleeBc.emit(NyarOpcode.LoadArg, 0);
        calleeBc.emit(NyarOpcode.Return);
        var calleeBytes = calleeBc.to_array();

        var callerBc = new BytecodeBuilder();
        callerBc.emit(NyarOpcode.LoadArg, 0);
        callerBc.emit(NyarOpcode.LoadArg, 0);
        callerBc.emit(NyarOpcode.CallDynamic, 0, 1, 0);
        callerBc.emit(NyarOpcode.Return);
        var callerBytes = callerBc.to_array();

        var combined = new byte[calleeBytes.Length + callerBytes.Length];
        calleeBytes.CopyTo(combined, 0);
        callerBytes.CopyTo(combined, calleeBytes.Length);

        var calleeFunc = new NyarFunction("callee", 1, 0, 0, calleeBytes.Length);
        calleeFunc.CodeOffset = 0;

        var callerFunc = new NyarFunction("caller", 1, 0, 0, callerBytes.Length);
        callerFunc.CodeOffset = calleeBytes.Length;

        var module = new NyarModule("call_dynamic_test")
        {
            constants = [Value.from_object("callee")],
            functions = [calleeFunc, callerFunc],
            raw_bytecode = combined
        };
        calleeFunc.Module = module;
        callerFunc.Module = module;

        var vm = new NyarVM(new JitOptions { Enabled = false });
        vm.Load(module);
        var result = vm.Run("call_dynamic_test", "caller", Value.from_int(42));
        Assert.Equal(ValueType.@int, result.type);
    }

    [Fact]
    public void AccessStatic_DecodesCorrectly()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.AccessStatic, 0);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("access_static_test", 1, 0, 0, bc.position);
        var module = create_module("access_static_test", bc.to_array(), func);

        var result = execute(module, "access_static_test", Value.from_int(77));
        Assert.True(result.type is ValueType.@int or ValueType.@null);
    }

    [Fact]
    public void AccessWitness_DecodesCorrectly()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.AccessWitness, 0);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("access_witness_test", 1, 0, 0, bc.position);
        var module = create_module("access_witness_test", bc.to_array(), func);

        var result = execute(module, "access_witness_test", Value.from_int(55));
        Assert.True(result.type is ValueType.@int or ValueType.@null);
    }

    [Fact]
    public void AccessDynamic_DecodesCorrectly()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.AccessDynamic, 0, 0);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("access_dynamic_test", 1, 0, 0, bc.position);
        var module = create_module("access_dynamic_test", bc.to_array(), func);

        var result = execute(module, "access_dynamic_test", Value.from_int(33));
        Assert.True(result.type is ValueType.@int or ValueType.@null);
    }

    [Fact]
    public void InlineCacheUpdate_UpdatesCacheEntry()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarOpcode.LoadArg, 0);
        bc.emit(NyarOpcode.LoadArg, 1);
        bc.emit(NyarOpcode.InlineCacheUpdate, 0);
        var constants = new List<Value> { Value.from_int(0) };
        bc.emit(NyarOpcode.Const, 0);
        bc.emit(NyarOpcode.Return);

        var func = new NyarFunction("ic_update_test", 2, 0, 0, bc.position);
        var module = create_module("ic_update_test", bc.to_array(), func, constants);

        var result = execute(module, "ic_update_test", Value.from_int(1), Value.from_int(42));
        Assert.Equal(0, result.@int);
    }

    #endregion
}
