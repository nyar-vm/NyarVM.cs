using System.Reflection.Metadata;
using System.Text.Json;

namespace Hermes.CLI;

/// <summary>
///     Hermes 命令处理器
/// </summary>
internal static class HermesCommands
{
    public static void generate(string? schema, string output, string? @namespace, string target, bool dryRun)
    {
        var schemaPath = schema ?? "./";
        Console.WriteLine("🔧 Hermes Generate");
        Console.WriteLine($"   Schema: {schemaPath}");
        Console.WriteLine($"   目标: {target}");
        Console.WriteLine($"   输出: {output}");

        try
        {
            var compiler = new HermesCompiler();
            var compilationResult = compiler.Compile(schemaPath);

            if (!compilationResult.Success)
            {
                var errors = compilationResult.Diagnostics.Diagnostics
                    .Where(d => d.Level == SchemaDiagnosticLevel.Error)
                    .Select(d => $"[{d.Code}] {d.Message}")
                    .ToList();

                var warnings = compilationResult.Diagnostics.Diagnostics
                    .Where(d => d.Level == SchemaDiagnosticLevel.Warning)
                    .Select(d => $"[{d.Code}] {d.Message}")
                    .ToList();

                if (warnings.Count > 0)
                {
                    Console.WriteLine("⚠️ 编译警告:");
                    foreach (var w in warnings) Console.WriteLine($"  {w}");
                }

                finalize_with_error($"Schema 编译失败:\n  {string.Join("\n  ", errors)}");
                return;
            }

            var schemaIr = compilationResult.Schema!;
            var ns = @namespace ?? schemaIr.Namespace ?? "Models";

            Console.WriteLine($"📊 Schema 分析: {schemaIr.Classes.Count} class, {schemaIr.Enums.Count} enum, " +
                              $"{schemaIr.Flags.Count} flags, {schemaIr.Unions.Count} union, " +
                              $"{schemaIr.Storages.Sum(s => s.Models.Count)} model");

            var dispatcher = new GeneratorDispatcher();
            register_generators(dispatcher, target);

            ensure_output_directory(output);

            var generatorConfigs = build_generator_configs(ns, output, target);
            var dispatchResult = dispatcher.Dispatch(schemaIr, output, generatorConfigs, schemaPath);

            if (!dispatchResult.Success)
            {
                finalize_with_error($"生成失败:\n  {string.Join("\n  ", dispatchResult.Errors)}");
                return;
            }

            if (dryRun)
            {
                Console.WriteLine("🏃 DryRun 模式，预览生成文件：");
                foreach (var file in dispatchResult.Files)
                    Console.WriteLine($"  📄 {file.Path} ({file.Content.Length} 字符)");

                return;
            }

            var filesWritten = write_generated_files(dispatchResult.Files);

            foreach (var warning in dispatchResult.Warnings) Console.WriteLine($"⚠️ {warning}");

            Console.WriteLine($"✅ Generate 完成: {filesWritten} 个文件已生成");
        }
        catch (Exception ex)
        {
            finalize_with_error($"生成失败: {ex.Message}");
        }
    }

    public static void validate(string? path, bool quiet, bool json)
    {
        var schemaPath = path ?? "./";
        Console.WriteLine("🔍 Hermes Validate");
        Console.WriteLine($"   路径: {schemaPath}");

        try
        {
            var compiler = new HermesCompiler();
            var result = compiler.Compile(schemaPath);

            var errors = result.Diagnostics.Diagnostics
                .Where(d => d.Level == SchemaDiagnosticLevel.Error)
                .ToList();

            var warnings = result.Diagnostics.Diagnostics
                .Where(d => d.Level == SchemaDiagnosticLevel.Warning)
                .ToList();

            if (json)
                output_validate_json(errors, warnings);
            else
                output_validate_text(errors, warnings, quiet);
        }
        catch (Exception ex)
        {
            if (json)
                Console.WriteLine($"{{\"success\":false,\"error\":\"{escape_json(ex.Message)}\"}}");
            else
                Console.WriteLine($"❌ 验证失败: {ex.Message}");
        }
    }

    public static void diff(string? oldPath, string? newPath, bool json, bool changesOnly)
    {
        if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newPath))
        {
            Console.WriteLine("❌ 请提供两个路径：hermes diff <old> <new>");
            return;
        }

        Console.WriteLine("🔍 Hermes Diff");
        Console.WriteLine($"   旧版本: {oldPath}");
        Console.WriteLine($"   新版本: {newPath}");

        try
        {
            var compiler = new HermesCompiler();
            var oldSchema = compiler.Compile(oldPath);
            var newSchema = compiler.Compile(newPath);

            if (!oldSchema.Success)
            {
                Console.WriteLine("❌ 旧 Schema 编译失败");
                return;
            }

            if (!newSchema.Success)
            {
                Console.WriteLine("❌ 新 Schema 编译失败");
                return;
            }

            var diff = compute_diff(oldSchema.Schema!, newSchema.Schema!);

            if (json)
                output_diff_json(diff);
            else
                output_diff_text(diff, changesOnly);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 比较失败: {ex.Message}");
        }
    }

    public static void init(string? name, bool force, bool withSchema)
    {
        var projectDir = string.IsNullOrEmpty(name) ? "./" : name;

        Console.WriteLine("🚀 Hermes Init");
        Console.WriteLine($"   项目目录: {projectDir}");

        try
        {
            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
                Console.WriteLine($"   ✅ 创建目录: {projectDir}");
            }

            create_git_ignore(projectDir, force);
            create_readme(projectDir, force);

            if (withSchema) create_sample_schema(projectDir, force);

            Console.WriteLine();
            Console.WriteLine("✅ 项目初始化完成");
            Console.WriteLine($"   cd {projectDir}");
            Console.WriteLine("   hermes generate");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 初始化失败: {ex.Message}");
        }
    }

    #region Generate 辅助

    private static void register_generators(GeneratorDispatcher dispatcher, string target)
    {
        var normalizedTarget = target.ToLower();

        if (normalizedTarget is "all" or "csharp") dispatcher.Register(new CSharpGenerator());

        if (normalizedTarget is "all" or "typescript" or "ts") dispatcher.Register(new TypeScriptGenerator());

        if (normalizedTarget is "all" or "go") dispatcher.Register(new GoGenerator());

        if (normalizedTarget is "all" or "java") dispatcher.Register(new JavaGenerator());

        if (normalizedTarget is "all" or "rust") dispatcher.Register(new RustGenerator());

        if (normalizedTarget is "all" or "sql" or "sql-ddl" or "sql_ddl") dispatcher.Register(new SqlPlugin());

        if (normalizedTarget is "all" or "ggscript") dispatcher.Register(new GGScriptGenerator());

        if (normalizedTarget is "all" or "rpc") dispatcher.Register(new RpcGenerator());
    }

    private static Dictionary<string, Dictionary<string, object>> build_generator_configs(string ns, string outputPath,
        string target)
    {
        var configs = new Dictionary<string, Dictionary<string, object>>();
        var normalizedTarget = target.ToLower();

        var generatorNames = normalizedTarget == "all"
            ? new[] { "csharp", "typescript", "go", "java", "rust", "sql", "ggscript", "rpc" }
            : new[]
            {
                normalizedTarget switch
                {
                    "ts" => "typescript", "sql-ddl" => "sql", "sql_ddl" => "sql", _ => normalizedTarget
                }
            };

        foreach (var genName in generatorNames)
        {
            var config = new Dictionary<string, object> { ["enabled"] = true };

            switch (genName)
            {
                case "csharp":
                    config["namespace"] = ns;
                    config["targets"] = "record";
                    config["output"] = Path.Combine(outputPath, "csharp");
                    break;
                case "typescript":
                    config["targets"] = "types";
                    config["output"] = Path.Combine(outputPath, "typescript");
                    break;
                case "go":
                    config["targets"] = "struct";
                    config["output"] = Path.Combine(outputPath, "go");
                    break;
                case "java":
                    config["targets"] = "pojo";
                    config["output"] = Path.Combine(outputPath, "java");
                    break;
                case "rust":
                    config["targets"] = "struct";
                    config["output"] = Path.Combine(outputPath, "rust");
                    break;
                case "sql":
                    config["targets"] = "ddl";
                    config["output"] = Path.Combine(outputPath, "sql");
                    config["include-dml"] = false;
                    break;
                case "ggscript":
                    config["output"] = Path.Combine(outputPath, "ggscript");
                    break;
                case "rpc":
                    config["targets"] = "rpc-client";
                    config["output"] = Path.Combine(outputPath, "rpc");
                    break;
            }

            configs[genName] = config;
        }

        return configs;
    }

    private static void ensure_output_directory(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }

    private static int write_generated_files(IReadOnlyList<GeneratedFile> files)
    {
        var filesWritten = 0;

        foreach (var file in files)
        {
            if (file.ChangeKind == FileChangeKind.Deleted)
            {
                if (File.Exists(file.Path))
                {
                    File.Delete(file.Path);
                    Console.WriteLine($"  🗑️ {file.Path}");
                }

                continue;
            }

            var dir = Path.GetDirectoryName(file.Path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(file.Path, file.Content);
            filesWritten++;
            Console.WriteLine($"  ✅ {file.Path}");
        }

        return filesWritten;
    }

    private static void finalize_with_error(string message)
    {
        Console.WriteLine($"❌ {message}");
    }

    #endregion

    #region Validate 辅助

    private static void output_validate_text(List<SchemaDiagnostic> errors, List<SchemaDiagnostic> warnings, bool quiet)
    {
        if (!quiet && warnings.Count > 0)
        {
            Console.WriteLine("⚠️ 警告:");
            foreach (var w in warnings)
            {
                Console.WriteLine($"  [{w.Code}] {w.Message}");
                if (!string.IsNullOrEmpty(w.FilePath)) Console.WriteLine($"    文件: {w.FilePath}:{w.Line}:{w.Column}");
            }
        }

        if (errors.Count > 0)
        {
            Console.WriteLine("❌ 错误:");
            foreach (var e in errors)
            {
                Console.WriteLine($"  [{e.Code}] {e.Message}");
                if (!string.IsNullOrEmpty(e.FilePath)) Console.WriteLine($"    文件: {e.FilePath}:{e.Line}:{e.Column}");
            }
        }
        else if (!quiet)
        {
            Console.WriteLine("✅ Schema 验证通过");
        }
    }

    private static void output_validate_json(List<SchemaDiagnostic> errors, List<SchemaDiagnostic> warnings)
    {
        var diagnostics = new List<object>();
        foreach (var d in warnings.Concat(errors))
            diagnostics.Add(new
            {
                level = d.Level.ToString(),
                code = d.Code,
                message = d.Message,
                source = d.FilePath,
                line = d.Line,
                column = d.Column
            });

        var json = JsonSerializer.Serialize(new
        {
            success = errors.Count == 0,
            warnings = warnings.Count,
            errors = errors.Count,
            diagnostics
        }, new JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine(json);
    }

    private static string escape_json(string s)
    {
        return s.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    #endregion

    #region Diff 辅助

    private static SchemaDiffResult compute_diff(SchemaIR oldSchema, SchemaIR newSchema)
    {
        var diff = new SchemaDiffResult();

        compare_classes(oldSchema, newSchema, diff);
        compare_enums(oldSchema, newSchema, diff);
        compare_flags(oldSchema, newSchema, diff);
        compare_unions(oldSchema, newSchema, diff);
        compare_services(oldSchema, newSchema, diff);
        compare_models(oldSchema, newSchema, diff);

        return diff;
    }

    private static void compare_classes(SchemaIR oldSchema, SchemaIR newSchema, SchemaDiffResult diff)
    {
        foreach (var cls in oldSchema.Classes)
            if (!newSchema.Classes.Any(c => c.Name == cls.Name))
                diff.deleted_classes.Add(cls);

        foreach (var cls in newSchema.Classes)
        {
            var oldCls = oldSchema.Classes.FirstOrDefault(c => c.Name == cls.Name);
            if (oldCls == null)
            {
                diff.added_classes.Add(cls);
            }
            else
            {
                var changes = compare_fields(oldCls.fields, cls.fields);
                if (changes.has_changes) diff.modified_classes.Add((oldCls, cls, changes));
            }
        }
    }

    private static void compare_enums(SchemaIR oldSchema, SchemaIR newSchema, SchemaDiffResult diff)
    {
        foreach (var e in oldSchema.Enums)
            if (!newSchema.Enums.Any(en => en.Name == e.Name))
                diff.deleted_enums.Add(e);

        foreach (var e in newSchema.Enums)
        {
            var oldE = oldSchema.Enums.FirstOrDefault(en => en.Name == e.Name);
            if (oldE == null)
                diff.added_enums.Add(e);
            else if (string.Join(",", oldE.Members) != string.Join(",", e.Members)) diff.modified_enums.Add((oldE, e));
        }
    }

    private static void compare_flags(SchemaIR oldSchema, SchemaIR newSchema, SchemaDiffResult diff)
    {
        foreach (var f in oldSchema.Flags)
            if (!newSchema.Flags.Any(fl => fl.Name == f.Name))
                diff.deleted_flags.Add(f);

        foreach (var f in newSchema.Flags)
        {
            var oldF = oldSchema.Flags.FirstOrDefault(fl => fl.Name == f.Name);
            if (oldF == null)
                diff.added_flags.Add(f);
            else if (string.Join(",", oldF.Members) != string.Join(",", f.Members)) diff.modified_flags.Add((oldF, f));
        }
    }

    private static void compare_unions(SchemaIR oldSchema, SchemaIR newSchema, SchemaDiffResult diff)
    {
        foreach (var u in oldSchema.Unions)
            if (!newSchema.Unions.Any(union => union.Name == u.Name))
                diff.deleted_unions.Add(u);

        foreach (var u in newSchema.Unions)
        {
            var oldU = oldSchema.Unions.FirstOrDefault(union => union.Name == u.Name);
            if (oldU == null)
                diff.added_unions.Add(u);
            else if (string.Join(",", oldU.Variants) != string.Join(",", u.Variants))
                diff.modified_unions.Add((oldU, u));
        }
    }

    private static void compare_services(SchemaIR oldSchema, SchemaIR newSchema, SchemaDiffResult diff)
    {
        foreach (var s in oldSchema.Services)
            if (!newSchema.Services.Any(sv => sv.Name == s.Name))
                diff.deleted_services.Add(s);

        foreach (var s in newSchema.Services)
        {
            var oldS = oldSchema.Services.FirstOrDefault(sv => sv.Name == s.Name);
            if (oldS == null)
                diff.added_services.Add(s);
            else if (oldS.Endpoints.Count != s.Endpoints.Count) diff.modified_services.Add((oldS, s));
        }
    }

    private static void compare_models(SchemaIR oldSchema, SchemaIR newSchema, SchemaDiffResult diff)
    {
        foreach (var storage in oldSchema.Storages)
        foreach (var model in storage.Models)
        {
            var exists = newSchema.Storages.Any(s => s.Models.Any(m => m.Name == model.Name));
            if (!exists) diff.deleted_models.Add(model);
        }

        foreach (var storage in newSchema.Storages)
        foreach (var model in storage.Models)
        {
            var oldModel = oldSchema.Storages
                .SelectMany(s => s.Models)
                .FirstOrDefault(m => m.Name == model.Name);

            if (oldModel == null) diff.added_models.Add(model);
        }
    }

    private static FieldDiffResult compare_fields(IReadOnlyList<FieldDefinition> oldFields,
        IReadOnlyList<FieldDefinition> newFields)
    {
        var result = new FieldDiffResult();

        foreach (var f in oldFields)
            if (!newFields.Any(nf => nf.Name == f.Name))
                result.deleted.Add(f);

        foreach (var f in newFields)
        {
            var oldF = oldFields.FirstOrDefault(nf => nf.Name == f.Name);
            if (oldF == null)
                result.added.Add(f);
            else if (oldF.FieldType.TypeName != f.FieldType.TypeName || oldF.IsOptional != f.IsOptional)
                result.modified.Add((oldF, f));
        }

        return result;
    }

    private static void output_diff_text(SchemaDiffResult diff, bool changesOnly)
    {
        if (!diff.has_changes)
        {
            Console.WriteLine("✅ 两个 Schema 完全相同");
            return;
        }

        if (!changesOnly)
        {
            Console.WriteLine("📊 差异摘要:");
            Console.WriteLine(
                $"   添加: {diff.added_classes.Count + diff.added_enums.Count + diff.added_flags.Count + diff.added_unions.Count + diff.added_services.Count + diff.added_models.Count}");
            Console.WriteLine(
                $"   修改: {diff.modified_classes.Count + diff.modified_enums.Count + diff.modified_flags.Count + diff.modified_unions.Count + diff.modified_services.Count}");
            Console.WriteLine(
                $"   删除: {diff.deleted_classes.Count + diff.deleted_enums.Count + diff.deleted_flags.Count + diff.deleted_unions.Count + diff.deleted_services.Count + diff.deleted_models.Count}");
        }

        output_diff_section("➕ 新增 Class", diff.added_classes.Select(c => c.Name));
        output_diff_section("🔧 修改 Class", diff.modified_classes.Select(c => c.Item1.Name));
        output_diff_section("➖ 删除 Class", diff.deleted_classes.Select(c => c.Name));

        output_diff_section("➕ 新增 Enum", diff.added_enums.Select(e => e.Name));
        output_diff_section("🔧 修改 Enum", diff.modified_enums.Select(e => e.Item1.Name));
        output_diff_section("➖ 删除 Enum", diff.deleted_enums.Select(e => e.Name));

        output_diff_section("➕ 新增 Flags", diff.added_flags.Select(f => f.Name));
        output_diff_section("🔧 修改 Flags", diff.modified_flags.Select(f => f.Item1.Name));
        output_diff_section("➖ 删除 Flags", diff.deleted_flags.Select(f => f.Name));

        output_diff_section("➕ 新增 Union", diff.added_unions.Select(u => u.Name));
        output_diff_section("🔧 修改 Union", diff.modified_unions.Select(u => u.Item1.Name));
        output_diff_section("➖ 删除 Union", diff.deleted_unions.Select(u => u.Name));

        output_diff_section("➕ 新增 Service", diff.added_services.Select(s => s.Name));
        output_diff_section("🔧 修改 Service", diff.modified_services.Select(s => s.Item1.Name));
        output_diff_section("➖ 删除 Service", diff.deleted_services.Select(s => s.Name));

        output_diff_section("➕ 新增 Model", diff.added_models.Select(m => m.Name));
        output_diff_section("➖ 删除 Model", diff.deleted_models.Select(m => m.Name));
    }

    private static void output_diff_section(string title, IEnumerable<string> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return;

        Console.WriteLine();
        Console.WriteLine(title);
        foreach (var item in list) Console.WriteLine($"  {item}");
    }

    private static void output_diff_json(SchemaDiffResult diff)
    {
        var obj = new
        {
            hasChanges = diff.has_changes,
            added = new
            {
                classes = diff.added_classes.Select(c => c.Name).ToList(),
                enums = diff.added_enums.Select(e => e.Name).ToList(),
                flags = diff.added_flags.Select(f => f.Name).ToList(),
                unions = diff.added_unions.Select(u => u.Name).ToList(),
                services = diff.added_services.Select(s => s.Name).ToList(),
                models = diff.added_models.Select(m => m.Name).ToList()
            },
            modified = new
            {
                classes = diff.modified_classes.Select(c => c.Item1.Name).ToList(),
                enums = diff.modified_enums.Select(e => e.Item1.Name).ToList(),
                flags = diff.modified_flags.Select(f => f.Item1.Name).ToList(),
                unions = diff.modified_unions.Select(u => u.Item1.Name).ToList(),
                services = diff.modified_services.Select(s => s.Item1.Name).ToList()
            },
            deleted = new
            {
                classes = diff.deleted_classes.Select(c => c.Name).ToList(),
                enums = diff.deleted_enums.Select(e => e.Name).ToList(),
                flags = diff.deleted_flags.Select(f => f.Name).ToList(),
                unions = diff.deleted_unions.Select(u => u.Name).ToList(),
                services = diff.deleted_services.Select(s => s.Name).ToList(),
                models = diff.deleted_models.Select(m => m.Name).ToList()
            }
        };

        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
    }

    #endregion

    #region Init 辅助

    private static void create_git_ignore(string projectDir, bool force)
    {
        var path = Path.Combine(projectDir, ".gitignore");
        if (File.Exists(path) && !force)
        {
            Console.WriteLine($"   ⚠️ {path} 已存在，跳过");
            return;
        }

        var content = @"# Hermes generated files
generated/
*.generated.cs
*.generated.ts
*.generated.go
*.generated.java
*.generated.rs
*.generated.sql
*.generated.ggs

# Logs
*.log
logs/

# IDE
.idea/
*.sln
*.csproj.user
*.suo
*.user
*.userosscache
*.sln.docstates
";
        File.WriteAllText(path, content);
        Console.WriteLine("   ✅ 创建: .gitignore");
    }

    private static void create_readme(string projectDir, bool force)
    {
        var path = Path.Combine(projectDir, "README.md");
        if (File.Exists(path) && !force)
        {
            Console.WriteLine($"   ⚠️ {path} 已存在，跳过");
            return;
        }

        var content = @"# Hermes 项目

使用 Hermes 元编程代码生成工具管理多语言代码。

## 快速开始

```bash
# 生成所有目标代码
hermes generate

# 验证 Schema
hermes validate

# 比较 Schema 差异
hermes diff old.he new.he
```

## 目录结构

```
├── schemas/          # Schema 定义文件
│   └── app.he       # 主应用 Schema
├── generated/        # 生成的代码（自动生成）
└── README.md
```

## Schema 定义

在 `schemas/` 目录下创建 `.he` 或 `.hermes` 文件定义数据模型。

## 生成目标

| 目标 | 说明 |
|------|------|
| csharp | C# 记录类型 |
| typescript | TypeScript 类型定义 |
| go | Go 结构体 |
| java | Java POJO |
| rust | Rust 结构体 |
| sql | SQL DDL |
| ggscript | GGScript 类型 |
| rpc | RPC 客户端/服务端代码 |
";
        File.WriteAllText(path, content);
        Console.WriteLine("   ✅ 创建: README.md");
    }

    private static void create_sample_schema(string projectDir, bool force)
    {
        var schemasDir = Path.Combine(projectDir, "schemas");
        if (!Directory.Exists(schemasDir)) Directory.CreateDirectory(schemasDir);

        var path = Path.Combine(schemasDir, "app.he");
        if (File.Exists(path) && !force)
        {
            Console.WriteLine($"   ⚠️ {path} 已存在，跳过");
            return;
        }

        var content = @"// Hermes Schema 示例
namespace app;

// 用户实体
class User {
    id: int32 @primary;
    name: utf8 @required;
    email: utf8 @unique;
    age: int32 @range(1, 120);
    status: Status;
}

// 用户状态枚举
enum Status {
    Pending;
    Active;
    Suspended;
}

// 权限标志
flags Permissions {
    Read;
    Write;
    Execute;
    Admin;
}

// 订单实体
class Order {
    id: int32 @primary;
    userId: int32 @foreign(User.id);
    productName: utf8;
    price: float64;
    quantity: int32;
}

// API 服务
service UserService {
    get(id: int32) -> User @get(""/api/users/{id}"");
    create(user: User) -> User @post(""/api/users"");
    list(page: int32, size: int32) -> list<User> @get(""/api/users"");
    delete(id: int32) @delete(""/api/users/{id}"");
}
";
        File.WriteAllText(path, content);
        Console.WriteLine("   ✅ 创建: schemas/app.he");
    }

    #endregion
}