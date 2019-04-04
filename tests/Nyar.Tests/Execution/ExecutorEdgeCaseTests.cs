using Nyar.Assembler;
using Nyar.Types;

namespace Nyar.Tests.Execution;

public class ExecutorEdgeCaseTests
{
    #region 大参数列的

    [Fact]
    public void LargeArgList_SumEightArgs()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.LoadArg, 0);
        bc.emit(NyarHeadCode.LoadArg, 1);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.LoadArg, 2);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.LoadArg, 3);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.LoadArg, 4);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.LoadArg, 5);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.LoadArg, 6);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.LoadArg, 7);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("sum8", 8, 0, 0, bc.position);
        var module = create_module("large_args_test", bc.to_array(), func);

        var result = execute(module, "sum8",
            Value.from_int(1), Value.from_int(2), Value.from_int(3), Value.from_int(4),
            Value.from_int(5), Value.from_int(6), Value.from_int(7), Value.from_int(8));
        Assert.Equal(36, result.@int);
    }

    #endregion

    #region 跨模块调的

    [Fact]
    public void CrossModule_TwoModules_IndependentExecution()
    {
        var module1Bc = new BytecodeBuilder();
        module1Bc.emit(NyarHeadCode.LoadArg, 0);
        module1Bc.emit(NyarHeadCode.LoadArg, 1);
        module1Bc.emit(NyarHeadCode.I32Add);
        module1Bc.emit(NyarHeadCode.Return);
        var func1 = new NyarFunction("add", 2, 0, 0, module1Bc.position);
        var module1 = create_module("math_mod", module1Bc.to_array(), func1);

        var module2Bc = new BytecodeBuilder();
        module2Bc.emit(NyarHeadCode.LoadArg, 0);
        module2Bc.emit(NyarHeadCode.LoadArg, 1);
        module2Bc.emit(NyarHeadCode.I32Mul);
        module2Bc.emit(NyarHeadCode.Return);
        var func2 = new NyarFunction("mul", 2, 0, 0, module2Bc.position);
        var module2 = create_module("mul_mod", module2Bc.to_array(), func2);

        var vm = new NyarVM();
        vm.Load(module1);
        vm.Load(module2);

        var addResult = vm.Run("math_mod", "add", Value.from_int(3), Value.from_int(4));
        Assert.Equal(7, addResult.@int);

        var mulResult = vm.Run("mul_mod", "mul", Value.from_int(3), Value.from_int(4));
        Assert.Equal(12, mulResult.@int);
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

    #region 异常处理

    [Fact]
    public void Throw_Uncaught_Propagates()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Throw);

        var func = new NyarFunction("thrower", 0, 0, 0, bc.position);
        var module = create_module("throw_test", bc.to_array(), func);

        Assert.Throws<NyarRuntimeException>(() => execute(module, "thrower"));
    }

    [Fact]
    public void Throw_InCalledFunction_Propagates()
    {
        var throwerBc = new BytecodeBuilder();
        throwerBc.emit(NyarHeadCode.Throw);
        var throwerBytes = throwerBc.to_array();
        var throwerFunc = new NyarFunction("thrower", 0, 0, 0, throwerBytes.Length);

        var callerBc = new BytecodeBuilder();
        callerBc.emit(NyarHeadCode.Call, 1);
        callerBc.emit(NyarHeadCode.Return);
        var callerBytes = callerBc.to_array();
        var callerFunc = new NyarFunction("caller", 0, 0, throwerBytes.Length, callerBytes.Length);

        var allBytecode = new byte[throwerBytes.Length + callerBytes.Length];
        Array.Copy(throwerBytes, 0, allBytecode, 0, throwerBytes.Length);
        Array.Copy(callerBytes, 0, allBytecode, throwerBytes.Length, callerBytes.Length);

        var module = create_module("throw_call_test", allBytecode, callerFunc);
        throwerFunc.Module = module;
        module.functions.Add(throwerFunc);

        Assert.Throws<NyarRuntimeException>(() => execute(module, "caller"));
    }

    #endregion

    #region 热加的卸载循环

    [Fact]
    public void HotReload_LoadUnloadReload()
    {
        var vm = new NyarVM();

        var bc1 = new BytecodeBuilder();
        bc1.emit(NyarHeadCode.Const, 0);
        bc1.emit(NyarHeadCode.Return);
        var constants1 = new List<Value> { Value.from_int(1) };
        var func1 = new NyarFunction("getValue", 0, 0, 0, bc1.position);
        var module1 = create_module("hot_mod", bc1.to_array(), func1, constants1);

        vm.Load(module1);
        var result1 = vm.Run("hot_mod", "getValue");
        Assert.Equal(1, result1.@int);

        var unloaded = vm.UnloadModule("hot_mod");
        Assert.True(unloaded);
        Assert.False(vm.HasModule("hot_mod"));

        var bc2 = new BytecodeBuilder();
        bc2.emit(NyarHeadCode.Const, 0);
        bc2.emit(NyarHeadCode.Return);
        var constants2 = new List<Value> { Value.from_int(2) };
        var func2 = new NyarFunction("getValue", 0, 0, 0, bc2.position);
        var module2 = new NyarModule("hot_mod")
        {
            constants = constants2,
            functions = [func2],
            version = 2,
            raw_bytecode = bc2.to_array()
        };
        func2.Module = module2;

        vm.Load(module2);
        var result2 = vm.Run("hot_mod", "getValue");
        Assert.Equal(2, result2.@int);
    }

    [Fact]
    public void HotReload_ReloadModule_ReplacesFunction()
    {
        var vm = new NyarVM();

        var bcV1 = new BytecodeBuilder();
        bcV1.emit(NyarHeadCode.Const, 0);
        bcV1.emit(NyarHeadCode.Return);
        var constantsV1 = new List<Value> { Value.from_int(10) };
        var funcV1 = new NyarFunction("calc", 0, 0, 0, bcV1.position);
        var moduleV1 = create_module("reload_mod", bcV1.to_array(), funcV1, constantsV1);

        vm.Load(moduleV1);
        Assert.Equal(10, vm.Run("reload_mod", "calc").@int);

        var bcV2 = new BytecodeBuilder();
        bcV2.emit(NyarHeadCode.Const, 0);
        bcV2.emit(NyarHeadCode.Return);
        var constantsV2 = new List<Value> { Value.from_int(20) };
        var funcV2 = new NyarFunction("calc", 0, 0, 0, bcV2.position);
        var moduleV2 = new NyarModule("reload_mod")
        {
            constants = constantsV2,
            functions = [funcV2],
            version = 2,
            raw_bytecode = bcV2.to_array()
        };
        funcV2.Module = moduleV2;

        var result = vm.HotReloader.ReloadModule(moduleV2);
        Assert.True(result.IsReload);
        Assert.Equal(1u, result.OldVersion);
        Assert.Equal(2u, result.NewVersion);

        Assert.Equal(20, vm.Run("reload_mod", "calc").@int);
    }

    #endregion

    #region 未找到模的函数

    [Fact]
    public void Run_NonexistentModule_Throws()
    {
        var vm = new NyarVM();
        Assert.Throws<NyarRuntimeException>(() => vm.Run("nonexistent", "func"));
    }

    [Fact]
    public void Run_NonexistentFunction_Throws()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Return);
        var func = new NyarFunction("existing", 0, 0, 0, bc.position);
        var module = create_module("mod", bc.to_array(), func);

        var vm = new NyarVM();
        vm.Load(module);

        Assert.Throws<NyarRuntimeException>(() => vm.Run("mod", "nonexistent"));
    }

    #endregion

    #region 字节码编解码往的

    [Fact]
    public void NyarModuleConverter_RoundTrip()
    {
        var originalModule = new NyarModule("roundtrip_test")
        {
            version = 1,
            constants = [Value.from_int(42), Value.from_bool(true), Value.@null, Value.from_string("hello")],
            functions = [new NyarFunction("main", 0, 2, 0, 10)],
            imports = [new ModuleImport("other_mod", "some_func", ImportKind.function)],
            exports = [new ModuleExport("main", ExportKind.function, 0)],
            raw_bytecode = new byte[10]
        };

        var encoded = NyarModuleConverter.Encode(originalModule);

        Assert.NotNull(encoded);
        Assert.True(encoded.Length > 0);

        var decodedModule = NyarModuleConverter.Decode(encoded);

        Assert.Equal("roundtrip_test", decodedModule.name);
        Assert.Equal(1u, decodedModule.version);
        Assert.Equal(4, decodedModule.constants.Count);
        Assert.Equal(42, decodedModule.constants[0].@int);
        Assert.True(decodedModule.constants[1].@bool);
        Assert.Equal(ValueType.@null, decodedModule.constants[2].type);
        Assert.Equal("hello", decodedModule.constants[3].@string);
        Assert.Single(decodedModule.functions);
        Assert.Equal("main", decodedModule.functions[0].name);
        Assert.Single(decodedModule.imports);
        Assert.Equal("other_mod", decodedModule.imports[0].module_name);
        Assert.Equal("some_func", decodedModule.imports[0].symbol_name);
        Assert.Single(decodedModule.exports);
        Assert.Equal("main", decodedModule.exports[0].name);
    }

    [Fact]
    public void CompilationUnitSerializer_And_NyarModuleConverter_Should_Preserve_WitnessEntries()
    {
        var unit = new GenerateModule("witness_roundtrip");
        var main = new GenerateFunction("main", "void");
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        var impl = new GenerateFunction("app.Buffer.Map.insert", "void");
        impl.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        unit.AddFunction(main);
        unit.AddFunction(impl);
        unit.AddExport(new GenerateModuleExport("main", GenerateExportKind.Function, 0));
        unit.AddWitnessEntry(new GenerateWitnessDispatchEntry(
            "std.collections.Map",
            3,
            "insert",
            "app.Buffer",
            "app.Buffer.Map.insert"));

        var runtimeModule = CompilationUnitSerializer.Serialize(unit);

        Assert.Single(runtimeModule.witness_entries);
        Assert.Equal(unit.WitnessEntries[0].MethodId, runtimeModule.witness_entries[0].method_id);
        Assert.Equal(unit.WitnessEntries[0].TypeId, runtimeModule.witness_entries[0].type_id);
        Assert.Equal("insert", runtimeModule.witness_entries[0].method_name);
        Assert.Equal(1, runtimeModule.witness_entries[0].function_index);
        Assert.Equal(unit.WitnessEntries[0].InterfaceId, runtimeModule.witness_entries[0].interface_id);
        Assert.Equal(3, runtimeModule.witness_entries[0].interface_method_index);

        var encoded = NyarModuleConverter.Encode(runtimeModule);
        var decoded = NyarModuleConverter.Decode(encoded);

        Assert.Single(decoded.witness_entries);
        Assert.Equal(runtimeModule.witness_entries[0].method_id, decoded.witness_entries[0].method_id);
        Assert.Equal(runtimeModule.witness_entries[0].type_id, decoded.witness_entries[0].type_id);
        Assert.Equal("insert", decoded.witness_entries[0].method_name);
        Assert.Equal(1, decoded.witness_entries[0].function_index);
        Assert.Equal(runtimeModule.witness_entries[0].interface_id, decoded.witness_entries[0].interface_id);
        Assert.Equal(3, decoded.witness_entries[0].interface_method_index);
    }

    [Fact]
    public void GenerateWitnessDispatchEntry_Should_Normalize_SymbolIdentity_Before_Computing_StableIds()
    {
        var entry = new GenerateWitnessDispatchEntry(
            "std::collections::Map",
            3,
            "insert",
            "app::Buffer",
            "app.Buffer.Map.insert");

        Assert.Equal("std.collections.Map", entry.NormalizedTraitName);
        Assert.Equal("app.Buffer", entry.NormalizedTargetTypeName);
        Assert.Equal(GenerateWitnessIdentity.ComputeMethodId("std.collections.Map", "app.Buffer", "insert"),
            entry.MethodId);
        Assert.Equal(GenerateWitnessIdentity.ComputeTypeId("app.Buffer"), entry.TypeId);
        Assert.Equal(GenerateWitnessIdentity.ComputeInterfaceId("std.collections.Map"), entry.InterfaceId);
        Assert.Equal(3, entry.InterfaceMethodIndex);
    }

    [Fact]
    public void BytecodeValidator_NegativeArity_Fails()
    {
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Return);
        var func = new NyarFunction("bad_arity", -1, 0, 0, bc.position);
        var module = create_module("bad_arity_mod", bc.to_array(), func);

        var validator = new BytecodeValidator();
        var result = validator.Validate(module, bc.to_array(), out var diagnostics);

        Assert.False(result);
        Assert.True(diagnostics.Count > 0);
    }

    [Fact]
    public void NyarModuleConverter_InvalidMagic_Throws()
    {
        var badBytecode = new byte[20];
        BitConverter.TryWriteBytes(badBytecode.AsSpan(0, 4), 0xDEADBEEFu);

        Assert.Throws<InvalidNyarDataException>(() =>
            NyarModuleConverter.Decode(badBytecode));
    }

    #endregion
}
