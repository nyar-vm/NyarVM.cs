// 临时脚本：编译 legion.tools 到 JVM 并保存 class 文件用于 javap 分析
// 用 dotnet-script 或直接嵌入测试运行

using System.Diagnostics;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Language.Valkyrie.Compiler;

var stdRoot = Path.GetFullPath(
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "valkyrie.v", "projects"));

var files = new List<string>
{
    Path.Combine(stdRoot, "legion.tools", "source", "_.v"),
    Path.Combine(stdRoot, "legion.tools", "source", "manifest.v"),
    Path.Combine(stdRoot, "legion.tools", "source", "build_context.v"),
    Path.Combine(stdRoot, "std", "source", "command", "_.v"),
    Path.Combine(stdRoot, "std", "source", "command", "model.v"),
    Path.Combine(stdRoot, "std", "source", "command", "app.v"),
    Path.Combine(stdRoot, "std", "source", "command", "help.v"),
    Path.Combine(stdRoot, "std", "source", "command", "convert.v"),
    Path.Combine(stdRoot, "std", "source", "io", "print.v"),
    Path.Combine(stdRoot, "std", "source", "io", "fs.v"),
    Path.Combine(stdRoot, "std.adaptor.jvm", "source", "console.v"),
    Path.Combine(stdRoot, "std.adaptor.jvm", "source", "io.v"),
    Path.Combine(stdRoot, "std.adaptor.jvm", "source", "system.v"),
};

var compiler = new ValkyrieCompiler();
var plan = new Nyar.Language.Valkyrie.Compiler.Pipeline.BuildPlan("legion.tools", "jvm-openjdk-linux-managed",
    Path.Combine(stdRoot, "legion.tools", "source", "_.v"));

var artifacts = compiler.compile_files_to_target([.. files], plan);
var classBytes = artifacts.primary_artifact.content;

var outputPath = Path.Combine(Path.GetTempPath(), "legion_tools_jvm.class");
File.WriteAllBytes(outputPath, classBytes);
Console.WriteLine($"Class file saved to: {outputPath}");