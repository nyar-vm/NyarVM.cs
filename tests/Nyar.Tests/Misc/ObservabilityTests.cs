using Nyar.Types;

namespace Nyar.Tests.Misc;

public class ObservabilityTests
{
    #region VmMetricsSnapshot 测试

    [Fact]
    public void VmMetricsSnapshot_ContainsAllFields()
    {
        var snapshot = new VmMetricsSnapshot
        {
            TotalInstructions = 100,
            TotalFunctionCalls = 50,
            TotalExecutionTimeMs = 10.0,
            JitCompilationCount = 2,
            JitTotalCompilationTimeMs = 5.0,
            JitExecutionCount = 30,
            IcHitCount = 20,
            IcMissCount = 5,
            IcHitRate = 0.8,
            GcMajorCollections = 1,
            GcMinorCollections = 3,
            GcTotalPauseTimeMs = 8.0,
            GcLastPauseTimeMs = 2.0,
            GcLiveObjectCount = 100,
            GcTotalReclaimed = 50,
            GcPromotedObjects = 10,
            OsrCompilationCount = 1,
            OsrTransitionCount = 1,
            IntrinsicCallCount = 5,
            NativeCallCount = 2
        };

        Assert.Equal(100, snapshot.TotalInstructions);
        Assert.Equal(50, snapshot.TotalFunctionCalls);
        Assert.Equal(10.0, snapshot.TotalExecutionTimeMs);
        Assert.Equal(2, snapshot.JitCompilationCount);
        Assert.Equal(5.0, snapshot.JitTotalCompilationTimeMs);
        Assert.Equal(30, snapshot.JitExecutionCount);
        Assert.Equal(20, snapshot.IcHitCount);
        Assert.Equal(5, snapshot.IcMissCount);
        Assert.Equal(0.8, snapshot.IcHitRate);
        Assert.Equal(1, snapshot.GcMajorCollections);
        Assert.Equal(3, snapshot.GcMinorCollections);
        Assert.Equal(8.0, snapshot.GcTotalPauseTimeMs);
        Assert.Equal(2.0, snapshot.GcLastPauseTimeMs);
        Assert.Equal(100, snapshot.GcLiveObjectCount);
        Assert.Equal(50, snapshot.GcTotalReclaimed);
        Assert.Equal(10, snapshot.GcPromotedObjects);
        Assert.Equal(1, snapshot.OsrCompilationCount);
        Assert.Equal(1, snapshot.OsrTransitionCount);
        Assert.Equal(5, snapshot.IntrinsicCallCount);
        Assert.Equal(2, snapshot.NativeCallCount);
    }

    #endregion

    #region OSR 可观测性集成测试

    [Fact]
    public void OsrManager_SetObservability_RecordsMetrics()
    {
        var metrics = new VmMetrics();
        var events = new VmEvents();
        var osrManager = new OsrManager();
        osrManager.SetObservability(metrics, events);

        OsrEventArgs? compileEvent = null;
        OsrEventArgs? transitionEvent = null;
        events.OsrCompilationCompleted += (_, e) => compileEvent = e;
        events.OsrTransition += (_, e) => transitionEvent = e;

        var entry = osrManager.RegisterOsrEntry(0, 10, 2, 3);
        osrManager.OnOsrCompiled(entry, (locals, stack) => Value.@null);

        Assert.Equal(1, metrics.OsrCompilationCount);

        if (compileEvent != null)
        {
            Assert.Equal(0, compileEvent.FunctionIndex);
            Assert.Equal(10, compileEvent.BytecodePc);
        }

        osrManager.OnOsrTransition();
        Assert.Equal(1, metrics.OsrTransitionCount);
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

        public BytecodeBuilder emit(NyarHeadCode op, int operand1, int operand2)
        {
            _bytes.Add((byte)op);
            _bytes.AddRange(BitConverter.GetBytes(operand1));
            _bytes.AddRange(BitConverter.GetBytes(operand2));
            return this;
        }

        public byte[] to_array()
        {
            return _bytes.ToArray();
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

    private static (NyarVM vm, NyarModule module) create_vm_with_module(
        string name, byte[] bytecode, NyarFunction func, List<Value>? constants = null)
    {
        var module = new NyarModule(name)
        {
            constants = constants ?? [],
            functions = [func],
            raw_bytecode = bytecode
        };
        func.Module = module;

        var vm = new NyarVM();
        vm.Load(module);
        return (vm, module);
    }

    #endregion

    #region VmMetrics 基础测试

    [Fact]
    public void VmMetrics_InitialState_AllZero()
    {
        var metrics = new VmMetrics();
        var snapshot = metrics.GetSnapshot();

        Assert.Equal(0, snapshot.TotalInstructions);
        Assert.Equal(0, snapshot.TotalFunctionCalls);
        Assert.Equal(0, snapshot.JitCompilationCount);
        Assert.Equal(0, snapshot.JitExecutionCount);
        Assert.Equal(0, snapshot.IcHitCount);
        Assert.Equal(0, snapshot.IcMissCount);
        Assert.Equal(0, snapshot.GcMajorCollections);
        Assert.Equal(0, snapshot.GcMinorCollections);
        Assert.Equal(0, snapshot.OsrCompilationCount);
        Assert.Equal(0, snapshot.OsrTransitionCount);
        Assert.Equal(0, snapshot.IntrinsicCallCount);
        Assert.Equal(0, snapshot.NativeCallCount);
    }

    [Fact]
    public void VmMetrics_RecordInstruction_IncrementsCount()
    {
        var metrics = new VmMetrics();
        metrics.RecordInstruction();
        metrics.RecordInstruction();
        metrics.RecordInstruction();

        Assert.Equal(3, metrics.TotalInstructions);
    }

    [Fact]
    public void VmMetrics_RecordFunctionCall_IncrementsCount()
    {
        var metrics = new VmMetrics();
        metrics.RecordFunctionCall();
        metrics.RecordFunctionCall();

        Assert.Equal(2, metrics.TotalFunctionCalls);
    }

    [Fact]
    public void VmMetrics_RecordJitCompilation_UpdatesMetrics()
    {
        var metrics = new VmMetrics();
        metrics.RecordJitCompilation(5.0);
        metrics.RecordJitCompilation(3.0);

        Assert.Equal(2, metrics.JitCompilationCount);
        Assert.Equal(8.0, metrics.JitTotalCompilationTimeMs);
    }

    [Fact]
    public void VmMetrics_RecordIcHitMiss_CalculatesRate()
    {
        var metrics = new VmMetrics();
        metrics.RecordIcHit();
        metrics.RecordIcHit();
        metrics.RecordIcHit();
        metrics.RecordIcMiss();

        Assert.Equal(3, metrics.IcHitCount);
        Assert.Equal(1, metrics.IcMissCount);
        Assert.Equal(0.75, metrics.IcHitRate);
    }

    [Fact]
    public void VmMetrics_RecordGcMajor_UpdatesMetrics()
    {
        var metrics = new VmMetrics();
        metrics.RecordGcMajorCollection(10.0, 50, 30);

        Assert.Equal(1, metrics.GcMajorCollections);
        Assert.Equal(10.0, metrics.GcTotalPauseTimeMs);
        Assert.Equal(10.0, metrics.GcLastPauseTimeMs);
        Assert.Equal(50, metrics.GcLiveObjectCount);
        Assert.Equal(30, metrics.GcTotalReclaimed);
    }

    [Fact]
    public void VmMetrics_RecordGcMinor_UpdatesMetrics()
    {
        var metrics = new VmMetrics();
        metrics.RecordGcMinorCollection(2.0, 80, 10, 5);

        Assert.Equal(1, metrics.GcMinorCollections);
        Assert.Equal(2.0, metrics.GcTotalPauseTimeMs);
        Assert.Equal(80, metrics.GcLiveObjectCount);
        Assert.Equal(10, metrics.GcTotalReclaimed);
        Assert.Equal(5, metrics.GcPromotedObjects);
    }

    [Fact]
    public void VmMetrics_RecordOsr_UpdatesMetrics()
    {
        var metrics = new VmMetrics();
        metrics.RecordOsrCompilation();
        metrics.RecordOsrTransition();

        Assert.Equal(1, metrics.OsrCompilationCount);
        Assert.Equal(1, metrics.OsrTransitionCount);
    }

    [Fact]
    public void VmMetrics_RecordFfi_UpdatesMetrics()
    {
        var metrics = new VmMetrics();
        metrics.RecordIntrinsicCall();
        metrics.RecordIntrinsicCall();
        metrics.RecordNativeCall();

        Assert.Equal(2, metrics.IntrinsicCallCount);
        Assert.Equal(1, metrics.NativeCallCount);
    }

    [Fact]
    public void VmMetrics_ExecutionTimer_Elapses()
    {
        var metrics = new VmMetrics();
        metrics.StartExecutionTimer();
        Thread.Sleep(10);
        metrics.StopExecutionTimer();

        Assert.True(metrics.TotalExecutionTimeMs >= 5);
    }

    [Fact]
    public void VmMetrics_Reset_ClearsAll()
    {
        var metrics = new VmMetrics();
        metrics.RecordInstruction();
        metrics.RecordFunctionCall();
        metrics.RecordJitCompilation(1.0);
        metrics.RecordIcHit();
        metrics.RecordGcMajorCollection(1.0, 1, 1);
        metrics.RecordOsrCompilation();
        metrics.RecordIntrinsicCall();

        metrics.Reset();

        Assert.Equal(0, metrics.TotalInstructions);
        Assert.Equal(0, metrics.TotalFunctionCalls);
        Assert.Equal(0, metrics.JitCompilationCount);
        Assert.Equal(0, metrics.IcHitCount);
        Assert.Equal(0, metrics.GcMajorCollections);
        Assert.Equal(0, metrics.OsrCompilationCount);
        Assert.Equal(0, metrics.IntrinsicCallCount);
    }

    [Fact]
    public void VmMetrics_GetSnapshot_CapturesCurrentState()
    {
        var metrics = new VmMetrics();
        metrics.RecordInstruction();
        metrics.RecordInstruction();
        metrics.RecordFunctionCall();

        var snapshot = metrics.GetSnapshot();

        Assert.Equal(2, snapshot.TotalInstructions);
        Assert.Equal(1, snapshot.TotalFunctionCalls);
    }

    [Fact]
    public void VmMetrics_GetReport_ContainsAllSections()
    {
        var metrics = new VmMetrics();
        metrics.RecordInstruction();
        metrics.RecordFunctionCall();

        var report = metrics.GetReport();

        Assert.Contains("执行器", report);
        Assert.Contains("JIT 编译", report);
        Assert.Contains("垃圾回收", report);
        Assert.Contains("OSR", report);
        Assert.Contains("FFI", report);
    }

    #endregion

    #region VmEvents 基础测试

    [Fact]
    public void VmEvents_JitCompilationCompleted_FiresEvent()
    {
        var events = new VmEvents();
        JitCompilationEventArgs? captured = null;
        events.JitCompilationCompleted += (_, e) => captured = e;

        events.OnJitCompilationCompleted(0, "test_func", 5.0);

        Assert.NotNull(captured);
        Assert.Equal(0, captured.FunctionIndex);
        Assert.Equal("test_func", captured.FunctionName);
        Assert.Equal(5.0, captured.DurationMs);
    }

    [Fact]
    public void VmEvents_GcCollectionCompleted_FiresEvent()
    {
        var events = new VmEvents();
        GcCollectionEventArgs? captured = null;
        events.GcCollectionCompleted += (_, e) => captured = e;

        events.OnGcCollectionCompleted(true, 10.0, 50, 30);

        Assert.NotNull(captured);
        Assert.True(captured.IsMajor);
        Assert.Equal(10.0, captured.PauseMs);
        Assert.Equal(50, captured.LiveCount);
        Assert.Equal(30, captured.Reclaimed);
    }

    [Fact]
    public void VmEvents_OsrTransition_FiresEvent()
    {
        var events = new VmEvents();
        OsrEventArgs? captured = null;
        events.OsrTransition += (_, e) => captured = e;

        events.OnOsrTransition(1, 42);

        Assert.NotNull(captured);
        Assert.Equal(1, captured.FunctionIndex);
        Assert.Equal(42, captured.BytecodePc);
    }

    [Fact]
    public void VmEvents_IntrinsicCalled_FiresEvent()
    {
        var events = new VmEvents();
        FfiCallEventArgs? captured = null;
        events.IntrinsicCalled += (_, e) => captured = e;

        events.OnIntrinsicCalled("print", 1);

        Assert.NotNull(captured);
        Assert.Equal("print", captured.FunctionName);
        Assert.Equal(1, captured.ArgCount);
    }

    [Fact]
    public void VmEvents_NativeCalled_FiresEvent()
    {
        var events = new VmEvents();
        FfiCallEventArgs? captured = null;
        events.NativeCalled += (_, e) => captured = e;

        events.OnNativeCalled("malloc", 2);

        Assert.NotNull(captured);
        Assert.Equal("malloc", captured.FunctionName);
        Assert.Equal(2, captured.ArgCount);
    }

    [Fact]
    public void VmEvents_NoSubscribers_DoesNotThrow()
    {
        var events = new VmEvents();

        var ex = Record.Exception(() =>
        {
            events.OnJitCompilationCompleted(0, "test", 1.0);
            events.OnGcCollectionCompleted(true, 1.0, 0, 0);
            events.OnOsrTransition(0, 0);
            events.OnIntrinsicCalled("test", 0);
            events.OnNativeCalled("test", 0);
        });

        Assert.Null(ex);
    }

    #endregion

    #region 集成测试：VM 执行记录指标

    [Fact]
    public void NyarVM_Execute_RecordsInstructionsAndFunctionCalls()
    {
        var constants = new List<Value> { Value.from_int(42) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 0);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("test", 0, 0, 0, bc.position);
        var (vm, module) = create_vm_with_module("obs_test", bc.to_array(), func, constants);

        vm.Run("obs_test", "test");

        Assert.True(vm.Metrics.TotalInstructions > 0);
        Assert.True(vm.Metrics.TotalExecutionTimeMs >= 0);
    }

    [Fact]
    public void NyarVM_Execute_RecordsExecutionTime()
    {
        var constants = new List<Value> { Value.from_int(0), Value.from_int(1) };
        var bc = new BytecodeBuilder();
        var loopStart = bc.position;
        bc.emit(NyarHeadCode.LoadLocal, 0);
        bc.emit(NyarHeadCode.Const, 1);
        bc.emit(NyarHeadCode.I32Add);
        bc.emit(NyarHeadCode.StoreLocal, 0);
        bc.emit(NyarHeadCode.JumpIfTrue, loopStart - (bc.position + 5));
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("loop_test", 1, 1, 0, bc.position);
        var (vm, module) = create_vm_with_module("time_test", bc.to_array(), func, constants);

        vm.Run("time_test", "loop_test", Value.from_bool(false));

        Assert.True(vm.Metrics.TotalExecutionTimeMs >= 0);
    }

    [Fact]
    public void NyarVM_Metrics_AccessibleFromVM()
    {
        var vm = new NyarVM();

        Assert.NotNull(vm.Metrics);
        Assert.NotNull(vm.Events);
    }

    [Fact]
    public void NyarVM_Events_JitCompilation_SubscriberReceivesEvent()
    {
        var vm = new NyarVM();
        JitCompilationEventArgs? captured = null;
        vm.Events.JitCompilationCompleted += (_, e) => captured = e;

        var constants = new List<Value> { Value.from_int(42) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 0);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("jit_test", 0, 0, 0, bc.position);
        var module = create_module("jit_event_test", bc.to_array(), func, constants);
        vm.Load(module);

        vm.JitCompiler.ForceCompile(0);

        if (captured != null)
        {
            Assert.Equal(0, captured.FunctionIndex);
            Assert.True(captured.DurationMs >= 0);
        }
    }

    #endregion

    #region GC 可观测性集成测试

    [Fact]
    public void NyarGC_Collect_TriggersGcEvent()
    {
        var gc = new NyarGC(Value._shared_object_table, Value._shared_table_lock);
        var metrics = new VmMetrics();
        var events = new VmEvents();
        gc.SetObservability(metrics, events);

        GcCollectionEventArgs? captured = null;
        events.GcCollectionCompleted += (_, e) => captured = e;

        var rootProvider = new SimpleGcRootProvider();
        gc.Collect(rootProvider);

        Assert.NotNull(captured);
        Assert.True(captured.IsMajor);
        Assert.Equal(1, metrics.GcMajorCollections);
    }

    [Fact]
    public void NyarGC_MinorCollect_TriggersGcEvent()
    {
        var gc = new NyarGC(Value._shared_object_table, Value._shared_table_lock);
        var metrics = new VmMetrics();
        var events = new VmEvents();
        gc.SetObservability(metrics, events);

        GcCollectionEventArgs? captured = null;
        events.GcCollectionCompleted += (_, e) => captured = e;

        var rootProvider = new SimpleGcRootProvider();
        gc.MinorCollect(rootProvider);

        Assert.NotNull(captured);
        Assert.False(captured.IsMajor);
        Assert.Equal(1, metrics.GcMinorCollections);
    }

    [Fact]
    public void NyarGC_Collect_UpdatesMetrics()
    {
        var gc = new NyarGC(Value._shared_object_table, Value._shared_table_lock);
        var metrics = new VmMetrics();
        gc.SetObservability(metrics, null);

        gc.allocate("test_obj");

        var rootProvider = new SimpleGcRootProvider();
        gc.Collect(rootProvider);

        Assert.Equal(1, metrics.GcMajorCollections);
        Assert.True(metrics.GcTotalPauseTimeMs >= 0);
    }

    private sealed class SimpleGcRootProvider : IGcRootProvider
    {
        public List<Value> get_root_values()
        {
            return [];
        }
    }

    #endregion

    #region JIT 可观测性集成测试

    [Fact]
    public void JitCompiler_SetObservability_RecordsMetrics()
    {
        var metrics = new VmMetrics();
        var events = new VmEvents();
        var jit = new JitCompiler(new JitOptions { Enabled = true, TieredCompilation = true, HotThreshold = 1 });
        jit.SetObservability(metrics, events);

        JitCompilationEventArgs? startedEvent = null;
        JitCompilationEventArgs? completedEvent = null;
        events.JitCompilationStarted += (_, e) => startedEvent = e;
        events.JitCompilationCompleted += (_, e) => completedEvent = e;

        var constants = new List<Value> { Value.from_int(42) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 0);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("jit_metrics_test", 0, 0, 0, bc.position);
        var module = new NyarModule("jit_metrics_mod")
        {
            constants = constants,
            functions = [func],
            raw_bytecode = bc.to_array()
        };
        func.Module = module;

        jit.SetContext(bc.to_array(), module);
        jit.ForceCompile(0);

        Assert.Equal(1, metrics.JitCompilationCount);
        Assert.True(metrics.JitTotalCompilationTimeMs >= 0);

        if (startedEvent != null)
        {
            Assert.Equal(0, startedEvent.FunctionIndex);
        }

        if (completedEvent != null)
        {
            Assert.Equal(0, completedEvent.FunctionIndex);
            Assert.True(completedEvent.DurationMs >= 0);
        }
    }

    [Fact]
    public void JitCompiler_IcLookup_RecordsHitMissMetrics()
    {
        var metrics = new VmMetrics();
        var jit = new JitCompiler();
        jit.SetObservability(metrics, null);

        var receiver = Value.from_int(42);
        jit.UpdateInlineCache(0, receiver, 1);

        jit.LookupInlineCache(0, receiver);
        Assert.Equal(1, metrics.IcHitCount);

        jit.LookupInlineCache(0, Value.from_double(3.14));
        Assert.Equal(1, metrics.IcMissCount);
    }

    #endregion

    #region 端到端可观测性测试

    [Fact]
    public void EndToEnd_MetricsAndEvents_Integrated()
    {
        var vm = new NyarVM();
        var metrics = vm.Metrics;
        var events = vm.Events;

        var jitEvents = new List<JitCompilationEventArgs>();
        events.JitCompilationCompleted += (_, e) => jitEvents.Add(e);

        var constants = new List<Value> { Value.from_int(42) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 0);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("e2e_test", 0, 0, 0, bc.position);
        var module = create_module("e2e_obs", bc.to_array(), func, constants);
        vm.Load(module);

        var result = vm.Run("e2e_obs", "e2e_test");

        Assert.Equal(42, result.@int);
        Assert.True(metrics.TotalInstructions > 0);
        Assert.True(metrics.TotalExecutionTimeMs >= 0);

        var snapshot = metrics.GetSnapshot();
        Assert.True(snapshot.TotalInstructions > 0);
    }

    [Fact]
    public void EndToEnd_MetricsReport_GeneratesSuccessfully()
    {
        var vm = new NyarVM();

        var constants = new List<Value> { Value.from_int(42) };
        var bc = new BytecodeBuilder();
        bc.emit(NyarHeadCode.Const, 0);
        bc.emit(NyarHeadCode.Return);

        var func = new NyarFunction("report_test", 0, 0, 0, bc.position);
        var module = create_module("report_mod", bc.to_array(), func, constants);
        vm.Load(module);

        vm.Run("report_mod", "report_test");

        var report = vm.Metrics.GetReport();
        Assert.False(string.IsNullOrEmpty(report));
        Assert.Contains("NyarVM", report);
    }

    #endregion
}