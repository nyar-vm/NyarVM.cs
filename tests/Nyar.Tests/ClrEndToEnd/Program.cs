using System.Diagnostics;
using Nyar.Assembler;
using Nyar.Binary.Clr;
using Nyar.Binary.Clr.Data;
using Nyar.Binary.Clr.Encode;
using Nyar.Binary.Nyar.Data;
using Nyar.Types;

namespace Nyar.Tests.ClrEndToEnd;

public static class Program
{
    public static int Main()
    {
        Console.WriteLine("=== CLR 后端独立端到端测试 ===");
        Console.WriteLine();

        var module = new GenerateModule("hello_world");

        var mainFunc = new GenerateFunction("main", "void");

        // print("Hello World!")
        mainFunc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Str("Hello World!")));
        mainFunc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallStatic,
            new GenerateOperand.FuncRef("print",
                new GenerateFunctionType
                {
                    Parameters = [GenerateValueType.String],
                    Results = [GenerateValueType.Void]
                })));

        // print("你好，世界！")
        mainFunc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Str("你好，世界！")));
        mainFunc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallStatic,
            new GenerateOperand.FuncRef("print",
                new GenerateFunctionType
                {
                    Parameters = [GenerateValueType.String],
                    Results = [GenerateValueType.Void]
                })));

        // print_line("=== CLR Backend works! ===")
        mainFunc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Str("=== CLR Backend works! ===")));
        mainFunc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallStatic,
            new GenerateOperand.FuncRef("print_line",
                new GenerateFunctionType
                {
                    Parameters = [GenerateValueType.String],
                    Results = [GenerateValueType.Void]
                })));

        mainFunc.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));

        module.AddFunction(mainFunc);

        module.AddExport(new GenerateModuleExport("main", GenerateExportKind.Function, 0));

        Console.WriteLine("正在编译...");
        var options = new CompilationOptions
        {
            Target = CompilationTarget.Clr
        };
        var compiler = new CodeGenCompiler([new ClrBackend()]);
        var compilation = compiler.Compile<ClrModuleData>(module, options);

        if (compilation.Output is null)
        {
            Console.Error.WriteLine("编译失败：");
            foreach (var diagnostic in compilation.Diagnostics)
            {
                Console.Error.WriteLine(diagnostic);
            }

            return 1;
        }

        var output = compilation.Output;

        Console.WriteLine("正在编码为 PE 文件...");
        var encoder = new ClrEncoder();
        var peBytes = encoder.encode(output.Data);

        var outputPath = Path.Combine(
            Path.GetTempPath(),
            "nyar_clr_test_hello_world.exe");
        File.WriteAllBytes(outputPath, peBytes);
        Console.WriteLine($"已写入: {outputPath}");

        foreach (var asset in output.Assets)
        {
            if (asset.Name.EndsWith(".msil", StringComparison.OrdinalIgnoreCase))
            {
                var msil = System.Text.Encoding.UTF8.GetString(asset.Content);
                Console.WriteLine();
                Console.WriteLine("=== MSIL 输出 ===");
                Console.WriteLine(msil);
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== 运行生成的 EXE ===");
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = outputPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process != null)
            {
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(30000);

                Console.Write(stdout);
                if (!string.IsNullOrEmpty(stderr))
                {
                    Console.Error.Write(stderr);
                }

                Console.WriteLine();
                Console.WriteLine($"退出代码: {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"运行失败: {ex.Message}");
        }

        return 0;
    }
}