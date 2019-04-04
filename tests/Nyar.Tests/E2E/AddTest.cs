using System;
using System.Collections.Generic;
using Nyar.Binary.Nyar.Data;
using Nyar;
using Nyar.Types;
using Nyar.VM;

namespace Nyar.Tests.E2E;


/// <summary>

///     M2 端到端验证：Add(1, 2) → 字节码 → VM → 输出 3


/// </summary>
public static class AddTest
{
    
/// <summary>
    
///     手动构建 Add(1, 2) 的字节码并执行
    
///     字节码序列：
    
///         Const 0    ; 加载常量 1 (常量池索引 0)
    
///         Const 1    ; 加载常量 2 (常量池索引 1)
    
///         I32Add     ; 相加
    
///         Return     ; 返回
    

/// </summary>
    public static void run()
    {
        var constants = new List<Value> { Value.FromInt(1), Value.FromInt(2) };

        var bytecode = new byte[]
        {
            (byte)NyarHeadCode.Const, 0x00, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.Const, 0x01, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.I32Add,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("add", 0, 0, 0, bytecode.Length);
        var module = new NyarModule("test")
        {
            Constants = constants,
            Functions = [func],
            RawBytecode = bytecode
        };

        var vm = new NyarVM();
        vm.Load(module);

        var result = vm.Run("test", "add");

        Console.WriteLine($"Add(1, 2) = {result}");
        Console.WriteLine($"类型: {result.type}");
        Console.WriteLine($"验证: {(result.Int == 3 ? "✅ 通过" : $"❌ 失败，期望 3，实际 {result.Int}")}");
    }

    
/// <summary>
    
///     测试带参数的函数调用：Add(a, b) = a + b
    
///     字节码序列：
    
///         LoadArg 0  ; 加载参数 a
    
///         LoadArg 1  ; 加载参数 b
    
///         I32Add     ; 相加
    
///         Return     ; 返回
    

/// </summary>
    public static void run_with_args()
    {
        var bytecode = new byte[]
        {
            (byte)NyarHeadCode.LoadArg, 0x00, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.LoadArg, 0x01, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.I32Add,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("add_args", 2, 0, 0, bytecode.Length);
        var module = new NyarModule("test_args")
        {
            Functions = [func],
            RawBytecode = bytecode
        };

        var vm = new NyarVM();
        vm.Load(module);

        var result = vm.Run("test_args", "add_args", Value.FromInt(10), Value.FromInt(20));

        Console.WriteLine($"Add(10, 20) = {result}");
        Console.WriteLine($"验证: {(result.Int == 30 ? "✅ 通过" : $"❌ 失败，期望 30，实际 {result.Int}")}");
    }

    
/// <summary>
    
///     测试比较和条件跳转：Max(a, b)
    
///     字节码序列：
    
///         LoadArg 0      ; a
    
///         LoadArg 1      ; b
    
///         I32GtS         ; a > b ?
    
///         JumpIfFalse 10 ; 跳到 else 分支 (偏移 10 字节)
    
///         LoadArg 0      ; return a
    
///         Jump 5         ; 跳过 else (偏移 5 字节)
    
///         LoadArg 1      ; return b
    
///         Return
    

/// </summary>
    public static void run_max()
    {
        var bytecode = new byte[]
        {
            (byte)NyarHeadCode.LoadArg, 0x00, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.LoadArg, 0x01, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.I32GtS,
            (byte)NyarHeadCode.JumpIfFalse, 0x0F, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.LoadArg, 0x00, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.Jump, 0x0A, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.LoadArg, 0x01, 0x00, 0x00, 0x00,
            (byte)NyarHeadCode.Return
        };

        var func = new NyarFunction("max", 2, 0, 0, bytecode.Length);
        var module = new NyarModule("test_max")
        {
            Functions = [func],
            RawBytecode = bytecode
        };

        var vm = new NyarVM();
        vm.Load(module);

        var result1 = vm.Run("test_max", "max", Value.FromInt(5), Value.FromInt(3));
        Console.WriteLine($"Max(5, 3) = {result1.Int} (验证: {(result1.Int == 5 ? "✅" : "❌")})");

        var result2 = vm.Run("test_max", "max", Value.FromInt(2), Value.FromInt(8));
        Console.WriteLine($"Max(2, 8) = {result2.Int} (验证: {(result2.Int == 8 ? "✅" : "❌")})");
    }
}