using Nyar.Types;

namespace Nyar.Tests.Execution;

public class FfiTests
{
    #region CallIntrinsic 测试

    [Fact]
    public void CallIntrinsic_RegisteredFunction_ReturnsResult()
    {
        var vm = new NyarVM(new JitOptions { Enabled = false });

        var intrinsicModule = new NyarModule("math");
        intrinsicModule.native_functions.Add(new NyarNativeFunction("add", "math", args =>
        {
            var a = args[0].@int;
            var b = args[1].@int;
            return Value.from_int(a + b);
        }));
        vm.Intrinsics.RegisterModule(intrinsicModule);

        var module = new NyarModule("test");
        var func = new NyarFunction("call_add", 0, 0, 0, 20);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[20];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.CallIntrinsic;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 2);
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 5), 2);
        offset += 9;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_int(10));
        module.constants.Add(Value.from_int(20));
        module.constants.Add(Value.from_string("math.add"));
        module.raw_bytecode = bytecode;
        vm.Load(module);

        var result = vm.Run("test", "call_add");

        Assert.Equal(30, result.@int);
    }

    [Fact]
    public void CallIntrinsic_UnknownFunction_ReturnsNull()
    {
        var vm = new NyarVM(new JitOptions { Enabled = false });

        var module = new NyarModule("test");
        var func = new NyarFunction("call_unknown", 0, 0, 0, 10);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[10];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.CallIntrinsic;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 5), 0);
        offset += 9;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_string("unknown.func"));
        module.raw_bytecode = bytecode;
        vm.Load(module);

        var result = vm.Run("test", "call_unknown");

        Assert.Equal(ValueType.@null, result.type);
    }

    [Fact]
    public void CallIntrinsic_StringOperation_ReturnsString()
    {
        var vm = new NyarVM(new JitOptions { Enabled = false });

        var intrinsicModule = new NyarModule("str");
        intrinsicModule.native_functions.Add(new NyarNativeFunction("concat", "str", args =>
        {
            var a = args[0].@string as string ?? "";
            var b = args[1].@string as string ?? "";
            return Value.from_string(string.Concat(a, b));
        }));
        vm.Intrinsics.RegisterModule(intrinsicModule);

        var module = new NyarModule("test");
        var func = new NyarFunction("concat_str", 0, 0, 0, 20);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[20];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.CallIntrinsic;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 2);
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 5), 2);
        offset += 9;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_string("hello"));
        module.constants.Add(Value.from_string(" world"));
        module.constants.Add(Value.from_string("str.concat"));
        module.raw_bytecode = bytecode;
        vm.Load(module);

        var result = vm.Run("test", "concat_str");

        Assert.Equal(ValueType.@string, result.type);
        Assert.Equal("hello world", result.@string as string);
    }

    #endregion

    #region FFI 基础设施测试

    [Fact]
    public void Ffi_LoadLibrary_InvalidPath_ThrowsDllNotFound()
    {
        var ffi = new FFI();
        Assert.Throws<DllNotFoundException>(() => ffi.LoadLibrary("nonexistent_library_12345.dll"));
        ffi.Dispose();
    }

    [Fact]
    public void Ffi_RegisterAndResolveFunction()
    {
        var ffi = new FFI();
        var funcPtr = new IntPtr(0x1234);
        ffi.RegisterFunction("test_func", funcPtr);

        var resolved = ffi.ResolveFunction("test_func");
        Assert.Equal(funcPtr, resolved);

        ffi.Dispose();
    }

    [Fact]
    public void Ffi_ResolveUnknownFunction_ReturnsZero()
    {
        var ffi = new FFI();
        var resolved = ffi.ResolveFunction("unknown_func");
        Assert.Equal(IntPtr.Zero, resolved);
        ffi.Dispose();
    }

    [Fact]
    public void Intrinsics_RegisterModuleAndFind()
    {
        var intrinsics = new Intrinsics();
        var module = new NyarModule("io");
        module.native_functions.Add(new NyarNativeFunction("println", "io", args => { return Value.@null; }));

        intrinsics.RegisterModule(module);

        var func = intrinsics.Find("io.println");
        Assert.NotNull(func);

        var result = func([Value.from_string("hello")]);
        Assert.Equal(ValueType.@null, result.type);
    }

    [Fact]
    public void Intrinsics_FindUnknown_ReturnsNull()
    {
        var intrinsics = new Intrinsics();
        var func = intrinsics.Find("unknown.func");
        Assert.Null(func);
    }

    [Fact]
    public void Intrinsics_Call_InvokesFunction()
    {
        var intrinsics = new Intrinsics();
        var module = new NyarModule("math");
        module.native_functions.Add(new NyarNativeFunction("double", "math",
            args => { return Value.from_int(args[0].@int * 2); }));

        intrinsics.RegisterModule(module);

        var result = intrinsics.Call("math.double", [Value.from_int(21)]);
        Assert.Equal(42, result.@int);
    }

    #endregion

    #region NyarHeadCode 定义测试

    [Fact]
    public void Opcode_CallIntrinsic_HasCorrectValue()
    {
        Assert.Equal(0xD0, (int)NyarHeadCode.CallIntrinsic);
    }

    [Fact]
    public void Opcode_CallNative_HasCorrectValue()
    {
        Assert.Equal(0xD1, (int)NyarHeadCode.CallNative);
    }

    [Fact]
    public void Opcode_LoadNativeLib_HasCorrectValue()
    {
        Assert.Equal(0xD2, (int)NyarHeadCode.LoadNativeLib);
    }

    [Fact]
    public void Opcode_GetNativeFunc_HasCorrectValue()
    {
        Assert.Equal(0xD3, (int)NyarHeadCode.GetNativeFunc);
    }

    #endregion
}