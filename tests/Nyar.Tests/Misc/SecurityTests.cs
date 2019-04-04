using System.Text;
using Nyar.Types;

namespace Nyar.Tests.Misc;

public class SecurityTests
{
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

    #endregion

    #region 辅助方法

    private static string format_diagnostics(EnhancedBytecodeValidator.ValidationResult result)
    {
        var sb = new StringBuilder();
        foreach (var d in result.Diagnostics)
        {
            sb.AppendLine($"[{d.Level}] {d.Code}: {d.Message} (func={d.FunctionName}, offset={d.Offset})");
        }

        return sb.ToString();
    }

    #endregion

    #region EnhancedBytecodeValidator 测试

    [Fact]
    public void Validate_ValidModule_Passes()
    {
        var constants = new List<Value> { Value.from_int(42) };
        var bc = new byte[]
        {
            (byte)NyarNyarHeadCode.Const, 0, 0, 0, 0,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("valid_mod", bc, func, constants);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.True(result.IsValid, format_diagnostics(result));
    }

    [Fact]
    public void Validate_EmptyModuleName_Fails()
    {
        var bc = new[] { (byte)NyarHeadCode.Return };
        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "MOD001");
    }

    [Fact]
    public void Validate_UndefinedOpcode_Fails()
    {
        var bc = new byte[] { 0xFF, (byte)NyarHeadCode.Return };
        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("undef_op", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "INS002");
    }

    [Fact]
    public void Validate_InvalidJumpTarget_Fails()
    {
        var bc = new byte[]
        {
            (byte)NyarHeadCode.Jump, 0xFF, 0xFF, 0xFF, 0x7F,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("bad_jump", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "INS008");
    }

    [Fact]
    public void Validate_InvalidCallTarget_Fails()
    {
        var bc = new byte[]
        {
            (byte)NyarHeadCode.Call, 0x10, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("bad_call", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "INS010");
    }

    [Fact]
    public void Validate_InvalidConstIndex_Fails()
    {
        var bc = new byte[]
        {
            (byte)NyarHeadCode.Const, 0x10, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("bad_const", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "INS011");
    }

    [Fact]
    public void Validate_NegativeArity_Fails()
    {
        var bc = new[] { (byte)NyarHeadCode.Return };
        var func = new NyarFunction("test", -1, 0, 0, bc.Length);
        var module = create_module("neg_arity", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "FN002");
    }

    [Fact]
    public void Validate_ExcessiveLocals_Fails()
    {
        var bc = new[] { (byte)NyarHeadCode.Return };
        var func = new NyarFunction("test", 0, 300, 0, bc.Length);
        var module = create_module("too_many_locals", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc, new EnhancedBytecodeValidator.ValidationOptions
        {
            MaxLocals = 256
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "FN004");
    }

    [Fact]
    public void Validate_StackUnderflow_Fails()
    {
        var bc = new[]
        {
            (byte)NyarHeadCode.I32Add,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("stack_underflow", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "INS005");
    }

    [Fact]
    public void Validate_StackOverflowLimit_Fails()
    {
        var bc = new byte[2048];
        for (var i = 0; i < 2048; i += 5)
        {
            bc[i] = (byte)NyarHeadCode.Const;
            bc[i + 1] = 0;
            bc[i + 2] = 0;
            bc[i + 3] = 0;
            bc[i + 4] = 0;
        }

        var constants = new List<Value> { Value.from_int(1) };
        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("stack_overflow", bc, func, constants);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc, new EnhancedBytecodeValidator.ValidationOptions
        {
            MaxStackDepth = 64
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "INS006");
    }

    [Fact]
    public void Validate_CodeRangeExceedsBytecode_Fails()
    {
        var bc = new[] { (byte)NyarHeadCode.Return };
        var func = new NyarFunction("test", 0, 0, 0, 100);
        var module = create_module("bad_range", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "FN006");
    }

    [Fact]
    public void Validate_TooManyFunctions_Fails()
    {
        var bc = new[] { (byte)NyarHeadCode.Return };
        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("too_many_funcs", bc, func);

        for (var i = 0; i < 10; i++)
        {
            var f = new NyarFunction($"func_{i}", 0, 0, 0, bc.Length);
            f.Module = module;
            module.functions.Add(f);
        }

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc, new EnhancedBytecodeValidator.ValidationOptions
        {
            MaxFunctions = 5
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "MOD002");
    }

    [Fact]
    public void Validate_InstructionCrossesBoundary_Fails()
    {
        var bc = new[]
        {
            (byte)NyarHeadCode.Const
        };

        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("cross_boundary", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "INS003");
    }

    [Fact]
    public void Validate_JumpNotAlignedToInstruction_Fails()
    {
        var bc = new byte[]
        {
            (byte)NyarHeadCode.Const, 0, 0, 0, 0,
            (byte)NyarHeadCode.Jump, 0x01, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("misaligned_jump", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "INS009");
    }

    [Fact]
    public void Validate_WithStackValidationDisabled_SkipsStackChecks()
    {
        var bc = new[]
        {
            (byte)NyarHeadCode.I32Add,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("test", 0, 0, 0, bc.Length);
        var module = create_module("no_stack_check", bc, func);

        var validator = new EnhancedBytecodeValidator();
        var result = validator.Validate(module, bc, new EnhancedBytecodeValidator.ValidationOptions
        {
            EnableStackValidation = false
        });

        Assert.True(result.IsValid);
    }

    #endregion

    #region ResourceLimits 测试

    [Fact]
    public void ResourceLimits_Default_HasNoLimits()
    {
        var limits = ResourceLimits.Default;

        Assert.Equal(512, limits.MaxFrameDepth);
        Assert.Equal(1024, limits.MaxStackDepth);
        Assert.Equal(0, limits.MaxInstructions);
        Assert.Equal(0, limits.MaxAllocations);
        Assert.Equal(0, limits.MaxExecutionTimeMs);
    }

    [Fact]
    public void ResourceLimits_Strict_HasModerateLimits()
    {
        var limits = ResourceLimits.Strict;

        Assert.Equal(128, limits.MaxFrameDepth);
        Assert.Equal(256, limits.MaxStackDepth);
        Assert.Equal(10_000_000, limits.MaxInstructions);
        Assert.Equal(100_000, limits.MaxAllocations);
        Assert.Equal(5000, limits.MaxExecutionTimeMs);
    }

    [Fact]
    public void ResourceLimits_Sandbox_HasTightLimits()
    {
        var limits = ResourceLimits.Sandbox;

        Assert.Equal(64, limits.MaxFrameDepth);
        Assert.Equal(128, limits.MaxStackDepth);
        Assert.Equal(1_000_000, limits.MaxInstructions);
        Assert.Equal(10_000, limits.MaxAllocations);
        Assert.Equal(1000, limits.MaxExecutionTimeMs);
    }

    [Fact]
    public void ResourceLimitExceededException_ContainsInfo()
    {
        var ex = new ResourceLimitExceededException("帧栈深度", 100, 64);

        Assert.Equal("帧栈深度", ex.ResourceType);
        Assert.Equal(100, ex.CurrentValue);
        Assert.Equal(64, ex.LimitValue);
        Assert.Contains("帧栈深度", ex.Message);
    }

    #endregion

    #region 运行时资源限制集成测试

    [Fact]
    public void NyarVM_WithStrictLimits_ThrowsOnInstructionLimit()
    {
        var limits = new ResourceLimits
        {
            MaxInstructions = 10
        };

        var vm = new NyarVM(new JitOptions { Enabled = false }, limits);

        var bc = new byte[100];
        for (var i = 0; i < 100; i += 5)
        {
            bc[i] = (byte)NyarHeadCode.Const;
            bc[i + 1] = 0;
            bc[i + 2] = 0;
            bc[i + 3] = 0;
            bc[i + 4] = 0;
        }

        var constants = new List<Value> { Value.from_int(1) };
        var func = new NyarFunction("infinite", 0, 0, 0, bc.Length);
        var module = create_module("limit_test", bc, func, constants);
        vm.Load(module);

        Assert.Throws<ResourceLimitExceededException>(() =>
            vm.Run("limit_test", "infinite"));
    }

    [Fact]
    public void NyarVM_WithStrictLimits_ThrowsOnFrameDepthLimit()
    {
        var limits = new ResourceLimits
        {
            MaxFrameDepth = 3
        };

        var vm = new NyarVM(new JitOptions { Enabled = false }, limits);

        var bc = new byte[]
        {
            (byte)NyarHeadCode.LoadArg, 0, 0, 0, 0,
            (byte)NyarHeadCode.Const, 0, 0, 0, 0,
            (byte)NyarHeadCode.I32GtS,
            (byte)NyarHeadCode.JumpIfFalse, 0x0D, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.LoadArg, 0, 0, 0, 0,
            (byte)NyarHeadCode.Const, 1, 0, 0, 0,
            (byte)NyarHeadCode.I32Sub,
            (byte)NyarHeadCode.Call, 0, 0, 0, 0,
            (byte)NyarHeadCode.Return,
            (byte)NyarHeadCode.Const, 2, 0, 0, 0,
            (byte)NyarHeadCode.Return
        };

        var constants = new List<Value> { Value.from_int(0), Value.from_int(1), Value.from_int(0) };
        var func = new NyarFunction("deep", 1, 0, 0, bc.Length);
        var module = create_module("depth_test", bc, func, constants);
        vm.Load(module);

        Assert.Throws<ResourceLimitExceededException>(() =>
            vm.Run("depth_test", "deep", Value.from_int(100)));
    }

    [Fact]
    public void NyarVM_WithDefaultLimits_RunsNormally()
    {
        var vm = new NyarVM(new JitOptions { Enabled = false });

        var constants = new List<Value> { Value.from_int(42) };
        var bc = new byte[]
        {
            (byte)NyarHeadCode.Const, 0, 0, 0, 0,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("normal", 0, 0, 0, bc.Length);
        var module = create_module("normal_test", bc, func, constants);
        vm.Load(module);

        var result = vm.Run("normal_test", "normal");
        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void NyarVM_SandboxLimits_CanRunSmallPrograms()
    {
        var limits = ResourceLimits.Sandbox;
        var vm = new NyarVM(new JitOptions { Enabled = false }, limits);

        var constants = new List<Value> { Value.from_int(42) };
        var bc = new byte[]
        {
            (byte)NyarHeadCode.Const, 0, 0, 0, 0,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("small", 0, 0, 0, bc.Length);
        var module = create_module("sandbox_test", bc, func, constants);
        vm.Load(module);

        var result = vm.Run("sandbox_test", "small");
        Assert.Equal(42, result.@int);
    }

    #endregion

    #region 恶意字节码拦截测试

    [Fact]
    public void MaliciousBytecode_InfiniteLoop_StoppedByInstructionLimit()
    {
        var limits = new ResourceLimits { MaxInstructions = 1000 };
        var vm = new NyarVM(new JitOptions { Enabled = false }, limits);

        var bc = new byte[]
        {
            (byte)NyarHeadCode.Jump, 0xFB, 0xFF, 0xFF, 0xFF
        };

        var func = new NyarFunction("infinite_loop", 0, 0, 0, bc.Length);
        var module = create_module("malicious_loop", bc, func);
        vm.Load(module);

        Assert.Throws<ResourceLimitExceededException>(() =>
            vm.Run("malicious_loop", "infinite_loop"));
    }

    [Fact]
    public void MaliciousBytecode_StackBomb_StoppedByStackLimit()
    {
        var limits = new ResourceLimits { MaxStackDepth = 16 };
        var vm = new NyarVM(new JitOptions { Enabled = false }, limits);

        var bc = new byte[200];
        for (var i = 0; i < 200; i += 5)
        {
            bc[i] = (byte)NyarHeadCode.Const;
            bc[i + 1] = 0;
            bc[i + 2] = 0;
            bc[i + 3] = 0;
            bc[i + 4] = 0;
        }

        var constants = new List<Value> { Value.from_int(1) };
        var func = new NyarFunction("stack_bomb", 0, 0, 0, bc.Length);
        var module = create_module("malicious_stack", bc, func, constants);
        vm.Load(module);

        Assert.Throws<ResourceLimitExceededException>(() =>
            vm.Run("malicious_stack", "stack_bomb"));
    }

    [Fact]
    public void MaliciousBytecode_DeepRecursion_StoppedByFrameDepth()
    {
        var limits = new ResourceLimits { MaxFrameDepth = 5 };
        var vm = new NyarVM(new JitOptions { Enabled = false }, limits);

        var bc = new byte[]
        {
            (byte)NyarHeadCode.Call, 0, 0, 0, 0,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("recurse", 0, 0, 0, bc.Length);
        var module = create_module("malicious_recurse", bc, func);
        vm.Load(module);

        Assert.Throws<ResourceLimitExceededException>(() =>
            vm.Run("malicious_recurse", "recurse"));
    }

    #endregion
}