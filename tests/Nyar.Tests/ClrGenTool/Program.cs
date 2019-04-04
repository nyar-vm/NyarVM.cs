using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyar.Language.Valkyrie;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;
using Std.Data.Text.Valkyrie.AST;
using Nyar.Assembler;
using Nyar.Assembler.Backends.Clr;

class Program
{
    static int Main()
    {
        var stdRoot = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..", "..", "..", "valkyrie.v", "projects"));

        var outputDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "clr-output");

        var msilPath = Path.Combine(outputDir, "legion.msil");

        Console.Error.WriteLine($"stdRoot: {stdRoot}");
        Console.Error.WriteLine($"outputDir: {outputDir}");

        var sourceFiles = new List<string>
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
            Path.Combine(stdRoot, "std", "source", "math", "graph_theory", "directed_graph.v"),
            Path.Combine(stdRoot, "std", "source", "math", "graph_theory", "cycle.v"),
            Path.Combine(stdRoot, "std", "source", "math", "graph_theory", "topological_sort.v"),
            Path.Combine(stdRoot, "std", "source", "math", "graph_theory", "traversal.v"),
            Path.Combine(stdRoot, "std", "source", "math", "graph_theory", "transitive_closure.v"),
            Path.Combine(stdRoot, "std.data.text.von", "source", "_.v"),
            Path.Combine(stdRoot, "std.data.text.von", "source", "token.v"),
            Path.Combine(stdRoot, "std.data.text.von", "source", "ast.v"),
            Path.Combine(stdRoot, "std.data.text.von", "source", "lexer.v"),
            Path.Combine(stdRoot, "std.data.text.von", "source", "parser.v"),
            Path.Combine(stdRoot, "std.data.text.v", "source", "_.v"),
            Path.Combine(stdRoot, "std.data.text.v", "source", "token.v"),
            Path.Combine(stdRoot, "std", "source", "adaptor", "clr", "_.v"),
            Path.Combine(stdRoot, "std", "source", "adaptor", "clr", "console.v"),
            Path.Combine(stdRoot, "std", "source", "adaptor", "clr", "file_system.v"),
        };

        foreach (var f in sourceFiles)
        {
            if (!File.Exists(f))
            {
                Console.Error.WriteLine($"源文件不存在: {f}");
                return 1;
            }
        }

        try
        {
            var compiler = new ValkyrieCompiler();
            var plan = new BuildPlan("legion.tools", "clr-microsoft-unknown-managed",
                Path.Combine(stdRoot, "legion.tools", "source", "_.v"));

            Console.Error.WriteLine("开始编译...");
            var artifacts = compiler.compile_files_to_target(sourceFiles.ToArray(), plan);

            if (artifacts is not { primary_artifact: not null })
            {
                Console.Error.WriteLine("编译失败：产物为空");
                return 1;
            }

            Directory.CreateDirectory(outputDir);

            var msilAsset = artifacts.sidecar_artifacts.FirstOrDefault(a => a.name.EndsWith(".msil", StringComparison.OrdinalIgnoreCase));
            if (msilAsset != null)
            {
                File.WriteAllBytes(msilPath, msilAsset.content);
                var msilText = Encoding.UTF8.GetString(msilAsset.content);
                Console.Error.WriteLine($"MSIL 已保存: {msilPath} ({msilAsset.content.Length} bytes)");

                var lines = msilText.Split('\n');
                var ldnullRetCount = 0;
                for (var i = 0; i < lines.Length - 1; i++)
                {
                    if (lines[i].Contains("ldnull") && lines[i + 1].Contains("ret"))
                    {
                        ldnullRetCount++;
                    }
                }
                Console.Error.WriteLine($"ldnull; ret 残留数: {ldnullRetCount}");

                var currentMethod = "";
                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith(".method "))
                    {
                        currentMethod = lines[i].Trim();
                    }
                    if (lines[i].Contains("ldnull") && i + 1 < lines.Length && lines[i + 1].Contains("ret"))
                    {
                        Console.Error.WriteLine($"  ldnull; ret 在方法: {currentMethod} (行 {i + 1})");
                    }
                }

                if (ldnullRetCount > 0)
                {
                    Console.Error.WriteLine($"警告: 仍有 {ldnullRetCount} 处 ldnull; ret 残留");
                }
            }
            else
            {
                Console.Error.WriteLine("未找到 MSIL 附属产物");
            }

            Console.Error.WriteLine($"编译成功！主产物: {artifacts.primary_artifact.name}, 大小: {artifacts.primary_artifact.content.Length}");
            Console.Error.WriteLine($"附属产物总数: {artifacts.sidecar_artifacts.Count}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"编译失败: {ex}");
            return 1;
        }
    }
}