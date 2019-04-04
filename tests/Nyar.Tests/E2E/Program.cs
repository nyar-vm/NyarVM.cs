using System;
using Nyar.Tests.E2E;

Console.WriteLine("=== NyarVM M2 端到端验证 ===");
Console.WriteLine();

Console.WriteLine("--- 测试 1: Add(1, 2) 常量加法 ---");
AddTest.run();
Console.WriteLine();

Console.WriteLine("--- 测试 2: Add(10, 20) 参数加法 ---");
AddTest.run_with_args();
Console.WriteLine();

Console.WriteLine("--- 测试 3: Max(5, 3) / Max(2, 8) 比较与条件跳转 ---");
AddTest.run_max();
Console.WriteLine();

Console.WriteLine("=== 验证完成 ===");