using System.Text;
using Hermes.Generator;

namespace Hermes.Plugin.CSharp;

public sealed class CSharpGenerator : IIncrementalGenerator
{
    public string Name => "csharp";
    public string[] SupportedTargets => ["record", "class", "json", "ef", "ef-mysql", "ef-pgsql", "ef-sqlite", "dto"];

    public GeneratorResult Generate(GeneratorContext context)
    {
        var result = new GeneratorResult();
        var ns = GetNamespace(context.Options);
        var target = GetTarget(context.Options);

        if (target == "dto")
        {
            foreach (var storage in context.Schema.Storages)
            foreach (var model in storage.Models)
                result.Files.Add(GenerateDto(model, ns, context.SchemaPath, context.OutputPath));

            return result;
        }

        foreach (var classDef in context.Schema.Classes)
            result.Files.Add(GenerateRecord(classDef, ns, target, context.SchemaPath, context.OutputPath));

        foreach (var storage in context.Schema.Storages)
        foreach (var model in storage.Models)
            result.Files.Add(GenerateModelEntity(model, storage, ns, target, context.SchemaPath, context.OutputPath));

        foreach (var structDef in context.Schema.Structures)
            result.Files.Add(GenerateValueObject(structDef, ns, target, context.SchemaPath, context.OutputPath));

        foreach (var enumDef in context.Schema.Enums)
            result.Files.Add(GenerateEnum(enumDef, ns, context.SchemaPath, context.OutputPath));

        foreach (var flagsDef in context.Schema.Flags)
            result.Files.Add(GenerateFlags(flagsDef, ns, context.SchemaPath, context.OutputPath));

        foreach (var unionDef in context.Schema.Unions)
            result.Files.Add(GenerateUnion(unionDef, ns, target, context.SchemaPath, context.OutputPath));

        foreach (var storage in context.Schema.Storages)
            if (storage.Models.Count > 0)
            {
                result.Files.Add(GenerateRepositoryInterface(storage, ns, target, context.SchemaPath,
                    context.OutputPath));
                result.Files.Add(GenerateRepositoryImplementation(storage, ns, target, context.SchemaPath,
                    context.OutputPath));
            }

        foreach (var service in context.Schema.Services)
            result.Files.Add(GenerateController(service, ns, context.SchemaPath, context.OutputPath));

        if (target is "ef" or "ef-mysql" or "ef-pgsql" or "ef-sqlite" && context.Schema.Storages.Count > 0)
        {
            result.Files.Add(GenerateDbContext(context.Schema, ns, context.SchemaPath, context.OutputPath));
            result.Files.Add(GenerateDdlScript(context.Schema, ns, target, context.SchemaPath, context.OutputPath));
            result.Files.Add(GenerateMigrationUp(context.Schema, ns, target, context.SchemaPath, context.OutputPath));
            result.Files.Add(GenerateMigrationDown(context.Schema, ns, target, context.SchemaPath, context.OutputPath));
        }

        return result;
    }

    public GeneratorResult GenerateIncremental(IncrementalGeneratorContext context)
    {
        var result = new GeneratorResult();
        var ns = GetNamespace(context.Options);
        var target = GetTarget(context.Options);

        if (target == "dto")
        {
            foreach (var storage in context.Schema.Storages)
            foreach (var model in storage.Models)
            {
                if (!context.ChangedTypeNames.Contains(model.Name)) continue;

                var dtoFile = GenerateDto(model, ns, context.SchemaPath, context.OutputPath);
                dtoFile.ChangeKind = FileChangeKind.Modified;
                result.Files.Add(dtoFile);
            }

            return result;
        }

        foreach (var classDef in context.Schema.Classes)
        {
            if (!context.ChangedTypeNames.Contains(classDef.Name)) continue;

            var file = GenerateRecord(classDef, ns, target, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var storage in context.Schema.Storages)
        foreach (var model in storage.Models)
        {
            if (!context.ChangedTypeNames.Contains(model.Name)) continue;

            var file = GenerateModelEntity(model, storage, ns, target, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var structDef in context.Schema.Structures)
        {
            if (!context.ChangedTypeNames.Contains(structDef.Name)) continue;

            var file = GenerateValueObject(structDef, ns, target, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var enumDef in context.Schema.Enums)
        {
            if (!context.ChangedTypeNames.Contains(enumDef.Name)) continue;

            var file = GenerateEnum(enumDef, ns, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var flagsDef in context.Schema.Flags)
        {
            if (!context.ChangedTypeNames.Contains(flagsDef.Name)) continue;

            var file = GenerateFlags(flagsDef, ns, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var unionDef in context.Schema.Unions)
        {
            if (!context.ChangedTypeNames.Contains(unionDef.Name)) continue;

            var file = GenerateUnion(unionDef, ns, target, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var storage in context.Schema.Storages)
        {
            if (!context.ChangedTypeNames.Contains(storage.Name)) continue;

            if (storage.Models.Count > 0)
            {
                var iface = GenerateRepositoryInterface(storage, ns, target, context.SchemaPath, context.OutputPath);
                iface.ChangeKind = FileChangeKind.Modified;
                result.Files.Add(iface);

                var impl = GenerateRepositoryImplementation(storage, ns, target, context.SchemaPath,
                    context.OutputPath);
                impl.ChangeKind = FileChangeKind.Modified;
                result.Files.Add(impl);
            }
        }

        foreach (var service in context.Schema.Services)
        {
            if (!context.ChangedTypeNames.Contains(service.Name)) continue;

            var ctrl = GenerateController(service, ns, context.SchemaPath, context.OutputPath);
            ctrl.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(ctrl);
        }

        return result;
    }

    #region Class 生成

    private GeneratedFile GenerateRecord(ClassDefinition classDef, string ns, string target, string schemaPath,
        string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        var imports = CollectImports(classDef, target);
        if (imports.Count > 0)
        {
            foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"using {imp};");

            sb.AppendLine();
        }

        if (target == "json") sb.AppendLine("[System.Text.Json.Serialization.JsonSerializable]");

        var typeKeyword = target == "class" ? "class" : "record";
        var sealedModifier = target == "class" ? "sealed " : "";
        var baseSuffix = classDef.Base != null ? $" : {classDef.Base.Name}" : "";

        sb.AppendLine($"public {sealedModifier}{typeKeyword} {classDef.Name}{baseSuffix}");
        sb.AppendLine("{");

        #region 属性声明

        foreach (var field in classDef.fields)
        {
            var csType = MapType(field.FieldType);
            var propertyName = ToPascalCase(field.Name);
            var nullableSuffix = NullableSuffix(field);

            if (target == "json")
            {
                var jsonName = ToCamelCase(field.Name);
                if (jsonName != propertyName)
                    sb.AppendLine($"    [System.Text.Json.Serialization.JsonPropertyName(\"{jsonName}\")]");

                if (field.IsOptional) sb.AppendLine("    [System.Text.Json.Serialization.JsonInclude]");
            }

            sb.AppendLine($"    public {csType}{nullableSuffix} {propertyName} {{ get; set; }}");
            sb.AppendLine();
        }

        #endregion

        #region 构造函数

        if (typeKeyword == "class")
        {
            sb.AppendLine($"    public {classDef.Name}() {{ }}");
            sb.AppendLine();

            if (classDef.fields.Count > 0)
            {
                var paramList = string.Join(", ", classDef.fields.Select(f =>
                {
                    var csType = MapType(f.FieldType);
                    var nullableSuffix = NullableSuffix(f);
                    var defaultVal = f.IsOptional ? " = null" : "";
                    return $"{csType}{nullableSuffix} {ToCamelCase(f.Name)}{defaultVal}";
                }));
                sb.AppendLine($"    public {classDef.Name}({paramList})");
                sb.AppendLine("    {");
                foreach (var field in classDef.fields)
                    sb.AppendLine($"        {ToPascalCase(field.Name)} = {ToCamelCase(field.Name)};");

                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        #endregion

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{classDef.Name}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = classDef.Name
        };
    }

    #endregion

    #region 值对象生成

    /// <summary>
    ///     生成不可变值类型（readonly record struct）
    /// </summary>
    private GeneratedFile GenerateValueObject(StructureDefinition structDef, string ns, string target,
        string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        var imports = new HashSet<string>();
        foreach (var field in structDef.fields) CollectTypeImports(field.FieldType, imports);

        if (target == "json") imports.Add("System.Text.Json.Serialization");

        if (imports.Count > 0)
        {
            foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"using {imp};");

            sb.AppendLine();
        }

        if (target == "json") sb.AppendLine("[System.Text.Json.Serialization.JsonSerializable]");

        var positionalParams = string.Join(", ", structDef.fields.Select(f =>
        {
            var csType = MapType(f.FieldType);
            var nullableSuffix = f.IsOptional && f.FieldType is not OptionType ? "?" : "";
            return $"{csType}{nullableSuffix} {ToCamelCase(f.Name)}";
        }));

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// 值对象 {structDef.Name}——不可变、按值比较、无独立标识");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public readonly record struct {structDef.Name}({positionalParams})");
        sb.AppendLine("{");

        #region 验证逻辑

        var hasValidation = false;
        foreach (var field in structDef.fields)
        {
            var minAttr = field.Attributes.FirstOrDefault(a => a.Name is "min" or "Min");
            var maxAttr = field.Attributes.FirstOrDefault(a => a.Name is "max" or "Max");
            var lenAttr = field.Attributes.FirstOrDefault(a =>
                a.Name is "len" or "Len" or "maxLength" or "maxlength" or "max_length");

            if (minAttr != null || maxAttr != null || lenAttr != null)
            {
                hasValidation = true;
                break;
            }
        }

        if (hasValidation)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 构造时验证约束条件");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public {structDef.Name}({positionalParams})");
            sb.AppendLine("    {");

            foreach (var field in structDef.fields)
            {
                var fieldName = ToCamelCase(field.Name);
                var propName = ToPascalCase(field.Name);

                var minAttr = field.Attributes.FirstOrDefault(a => a.Name is "min" or "Min");
                var maxAttr = field.Attributes.FirstOrDefault(a => a.Name is "max" or "Max");
                var lenAttr = field.Attributes.FirstOrDefault(a =>
                    a.Name is "len" or "Len" or "maxLength" or "maxlength" or "max_length");

                if (minAttr != null && minAttr.Arguments.Count > 0 &&
                    int.TryParse(minAttr.Arguments[0].Value, out var minVal))
                {
                    sb.AppendLine($"        if ({fieldName} < {minVal})");
                    sb.AppendLine("        {");
                    sb.AppendLine(
                        $"            throw new System.ArgumentOutOfRangeException(nameof({fieldName}), $\"{propName} 不能小于 {minVal}\");");
                    sb.AppendLine("        }");
                }

                if (maxAttr != null && maxAttr.Arguments.Count > 0 &&
                    int.TryParse(maxAttr.Arguments[0].Value, out var maxVal))
                {
                    sb.AppendLine($"        if ({fieldName} > {maxVal})");
                    sb.AppendLine("        {");
                    sb.AppendLine(
                        $"            throw new System.ArgumentOutOfRangeException(nameof({fieldName}), $\"{propName} 不能大于 {maxVal}\");");
                    sb.AppendLine("        }");
                }

                if (lenAttr != null && lenAttr.Arguments.Count > 0 &&
                    int.TryParse(lenAttr.Arguments[0].Value, out var maxLen))
                {
                    sb.AppendLine($"        if ({fieldName} != null && {fieldName}.Length > {maxLen})");
                    sb.AppendLine("        {");
                    sb.AppendLine(
                        $"            throw new System.ArgumentOutOfRangeException(nameof({fieldName}), $\"{propName} 长度不能超过 {maxLen}\");");
                    sb.AppendLine("        }");
                }
            }

            foreach (var field in structDef.fields)
            {
                var fieldName = ToCamelCase(field.Name);
                sb.AppendLine($"        this.{fieldName} = {fieldName};");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        #endregion

        #region JSON 属性名

        if (target == "json")
            foreach (var field in structDef.fields)
            {
                var jsonName = ToCamelCase(field.Name);
                var propName = ToPascalCase(field.Name);
                if (jsonName != propName)
                {
                    sb.AppendLine($"    [System.Text.Json.Serialization.JsonPropertyName(\"{jsonName}\")]");
                    sb.AppendLine(
                        $"    public {MapType(field.FieldType)}{(field.IsOptional && field.FieldType is not OptionType ? "?" : "")} {propName} => {jsonName};");
                    sb.AppendLine();
                }
            }

        #endregion

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{structDef.Name}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = structDef.Name
        };
    }

    #endregion

    #region Model 实体生成

    private GeneratedFile GenerateModelEntity(ModelDefinition modelDef, StorageDefinition storage, string ns,
        string target, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        var imports = new HashSet<string>();
        foreach (var field in modelDef.fields) CollectTypeImports(field.FieldType, imports);

        if (target == "json") imports.Add("System.Text.Json.Serialization");

        if (target == "ef")
        {
            imports.Add("System.ComponentModel.DataAnnotations");
            imports.Add("System.ComponentModel.DataAnnotations.Schema");
        }

        if (imports.Count > 0)
        {
            foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"using {imp};");

            sb.AppendLine();
        }

        if (target == "json") sb.AppendLine("[System.Text.Json.Serialization.JsonSerializable]");

        var typeKeyword = target is "class" or "ef" ? "class" : "record";
        var sealedModifier = target is "class" or "ef" ? "sealed " : "";

        if (modelDef.DocComment is not null)
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {modelDef.DocComment}");
            sb.AppendLine("/// </summary>");
        }

        if (target == "ef") sb.AppendLine($"[Table(\"{ToSnakeCase(modelDef.Name)}\")]");

        sb.AppendLine($"public {sealedModifier}{typeKeyword} {modelDef.Name}");
        sb.AppendLine("{");

        #region 字段属性

        foreach (var field in modelDef.fields)
        {
            var csType = MapType(field.FieldType);
            var propertyName = ToPascalCase(field.Name);
            var nullableSuffix = NullableSuffix(field);

            if (field.DocComment is not null)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {field.DocComment}");
                sb.AppendLine("    /// </summary>");
            }

            if (target == "json")
            {
                var jsonName = ToCamelCase(field.Name);
                if (jsonName != propertyName)
                    sb.AppendLine($"    [System.Text.Json.Serialization.JsonPropertyName(\"{jsonName}\")]");
            }

            var isKey = IsKeyField(field);
            var isForeignKey = field.IsForeignKey;

            if (target == "ef")
            {
                GenerateEfAnnotations(field, isKey, isForeignKey, sb);
            }
            else
            {
                if (isKey) sb.AppendLine("    [System.ComponentModel.DataAnnotations.Key]");
            }

            sb.AppendLine($"    public {csType}{nullableSuffix} {propertyName} {{ get; set; }}");
            sb.AppendLine();
        }

        #endregion

        #region 导航属性

        if (target == "ef") GenerateNavigationProperties(modelDef, sb);

        #endregion

        #region Getter

        foreach (var getter in modelDef.Getters)
        {
            var csReturnType = getter.ReturnType != null ? MapType(getter.ReturnType) : "object";
            var propertyName = ToPascalCase(getter.Name);
            var csBody = ConvertGetterBody(getter.Body);

            if (getter.DocComment is not null)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {getter.DocComment}");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    public {csReturnType} {propertyName} => {csBody};");
            sb.AppendLine();
        }

        #endregion

        #region 构造函数

        if (typeKeyword == "class")
        {
            sb.AppendLine($"    public {modelDef.Name}() {{ }}");
            sb.AppendLine();

            if (modelDef.fields.Count > 0)
            {
                var paramList = string.Join(", ", modelDef.fields.Select(f =>
                {
                    var csType = MapType(f.FieldType);
                    var nullableSuffix = NullableSuffix(f);
                    var defaultVal = f.IsOptional ? " = null" : "";
                    return $"{csType}{nullableSuffix} {ToCamelCase(f.Name)}{defaultVal}";
                }));
                sb.AppendLine($"    public {modelDef.Name}({paramList})");
                sb.AppendLine("    {");
                foreach (var field in modelDef.fields)
                    sb.AppendLine($"        {ToPascalCase(field.Name)} = {ToCamelCase(field.Name)};");

                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        #endregion

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{modelDef.Name}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = modelDef.Name
        };
    }

    #endregion

    #region DbContext 生成

    private GeneratedFile GenerateDbContext(SchemaIR schema, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        var contextName = $"{ToPascalCase(schema.Namespace)}DbContext";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {schema.Namespace} 数据库上下文");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public sealed class {contextName} : DbContext");
        sb.AppendLine("{");

        sb.AppendLine($"    public {contextName}(DbContextOptions<{contextName}> options) : base(options) {{ }}");
        sb.AppendLine();

        foreach (var storage in schema.Storages)
        foreach (var model in storage.Models)
        {
            var modelName = ToPascalCase(model.Name);
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {modelName} 实体集");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public DbSet<{modelName}> {modelName}s {{ get; set; }}");
            sb.AppendLine();
        }

        sb.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
        sb.AppendLine("    {");

        foreach (var storage in schema.Storages)
        foreach (var model in storage.Models)
        {
            var modelName = ToPascalCase(model.Name);
            sb.AppendLine($"        modelBuilder.Entity<{modelName}>(entity =>");
            sb.AppendLine("        {");

            sb.AppendLine($"            entity.ToTable(\"{ToSnakeCase(modelName)}\");");

            var keyField = GetKeyField(model);
            if (keyField != null)
            {
                var keyPropName = ToPascalCase(keyField.Name);
                sb.AppendLine($"            entity.HasKey(e => e.{keyPropName});");
                sb.AppendLine($"            entity.Property(e => e.{keyPropName}).ValueGeneratedOnAdd();");
            }

            foreach (var field in model.fields)
            {
                if (IsKeyField(field)) continue;

                var propName = ToPascalCase(field.Name);
                var colName = ToSnakeCase(field.Name);

                if (!field.IsOptional) sb.AppendLine($"            entity.Property(e => e.{propName}).IsRequired();");

                sb.AppendLine($"            entity.Property(e => e.{propName}).HasColumnName(\"{colName}\");");

                var sqlType = MapSqlType(field.FieldType);
                if (sqlType != null)
                    sb.AppendLine($"            entity.Property(e => e.{propName}).HasColumnType(\"{sqlType}\");");

                var maxLenAttr = field.Attributes.FirstOrDefault(a =>
                    a.Name is "maxLength" or "maxlength" or "max_length" or "len" or "Len");
                if (maxLenAttr != null && maxLenAttr.Arguments.Count > 0 &&
                    int.TryParse(maxLenAttr.Arguments[0].Value, out var maxLen))
                    sb.AppendLine($"            entity.Property(e => e.{propName}).HasMaxLength({maxLen});");

                var uniqueAttr = field.Attributes.FirstOrDefault(a => a.Name is "unique" or "Unique");
                if (uniqueAttr != null) sb.AppendLine($"            entity.HasIndex(e => e.{propName}).IsUnique();");
            }

            foreach (var field in model.fields)
            {
                if (!field.IsForeignKey) continue;

                var propName = ToPascalCase(field.Name);
                var refAttr = field.Attributes.FirstOrDefault(a => a.Name is "ref" or "Ref" or "fk" or "Fk");
                if (refAttr != null && refAttr.Arguments.Count > 0)
                {
                    var targetEntity = refAttr.Arguments[0].Value;
                    var navPropName = ToPascalCase(targetEntity);
                    sb.AppendLine($"            entity.HasOne(e => e.{navPropName})");
                    sb.AppendLine("                .WithMany()");
                    sb.AppendLine($"                .HasForeignKey(e => e.{propName});");
                }
            }

            sb.AppendLine("        });");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{contextName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = contextName
        };
    }

    #endregion

    #region Controller 生成

    private GeneratedFile GenerateController(ServiceDefinition service, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        var controllerName = $"{service.Name}Controller";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {service.Name} 控制器");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[ApiController]");
        sb.AppendLine("[Route(\"api/[controller]\")]");
        sb.AppendLine($"public sealed class {controllerName} : ControllerBase");
        sb.AppendLine("{");

        foreach (var endpoint in service.Endpoints)
        {
            var methodName = ToPascalCase(endpoint.Name);

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {endpoint.Name}");
            sb.AppendLine("    /// </summary>");

            if (endpoint is HttpEndpoint httpEp)
            {
                var httpMethod = httpEp.HttpMethod.ToUpperInvariant();
                var routeAttr = httpMethod switch
                {
                    "GET" => "HttpGet",
                    "POST" => "HttpPost",
                    "PUT" => "HttpPut",
                    "DELETE" => "HttpDelete",
                    "PATCH" => "HttpPatch",
                    _ => "HttpGet"
                };

                if (!string.IsNullOrEmpty(httpEp.Path))
                    sb.AppendLine($"    [{routeAttr}(\"{httpEp.Path}\")]");
                else
                    sb.AppendLine($"    [{routeAttr}]");
            }
            else
            {
                sb.AppendLine("    [HttpGet]");
            }

            var returnType = endpoint.ReturnType != null ? "Task<IActionResult>" : "Task<IActionResult>";
            sb.AppendLine(
                $"    public async {returnType} {methodName}Async(CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine("        // TODO: 实现业务逻辑");
            sb.AppendLine("        await Task.CompletedTask;");
            sb.AppendLine("        return Ok();");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{controllerName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = controllerName
        };
    }

    #endregion

    #region EF 注解生成

    private void GenerateEfAnnotations(FieldDefinition field, bool isKey, bool isForeignKey, StringBuilder sb)
    {
        if (isKey)
        {
            sb.AppendLine("    [Key]");
            sb.AppendLine("    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
        }

        if (isForeignKey)
        {
            var refAttr = field.Attributes.FirstOrDefault(a => a.Name is "ref" or "Ref" or "fk" or "Fk");
            if (refAttr != null && refAttr.Arguments.Count > 0)
            {
                var targetEntity = refAttr.Arguments[0].Value;
                sb.AppendLine($"    [ForeignKey(\"{targetEntity}\")]");
            }
        }

        if (!field.IsOptional && !isKey) sb.AppendLine("    [Required]");

        var maxLengthAttr =
            field.Attributes.FirstOrDefault(a =>
                a.Name is "maxLength" or "maxlength" or "max_length" or "len" or "Len");
        if (maxLengthAttr != null && maxLengthAttr.Arguments.Count > 0 &&
            int.TryParse(maxLengthAttr.Arguments[0].Value, out var maxLen)) sb.AppendLine($"    [MaxLength({maxLen})]");

        var colName = ToSnakeCase(field.Name);
        sb.AppendLine($"    [Column(\"{colName}\")]");

        var csType = MapType(field.FieldType);
        var sqlType = MapSqlType(field.FieldType);
        if (sqlType != null && csType != sqlType) sb.AppendLine($"    [Column(TypeName = \"{sqlType}\")]");

        var uniqueAttr = field.Attributes.FirstOrDefault(a => a.Name is "unique" or "Unique");
        if (uniqueAttr != null) sb.AppendLine("    [Index(IsUnique = true)]");
    }

    private void GenerateNavigationProperties(ModelDefinition modelDef, StringBuilder sb)
    {
        foreach (var field in modelDef.fields)
        foreach (var attr in field.Attributes)
            if (attr.Name is "HasOne" or "hasOne" or "has_one")
            {
                var navType = attr.Arguments.Count > 0 ? attr.Arguments[0].Value : field.Name;
                var navPropertyName = ToPascalCase(field.Name);
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// 导航属性: {navType}（对一关系）");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public {navType} {navPropertyName} {{ get; set; }} = null!;");
                sb.AppendLine();
            }
            else if (attr.Name is "HasMany" or "hasMany" or "has_many")
            {
                var navType = attr.Arguments.Count > 0 ? attr.Arguments[0].Value : field.Name;
                var navPropertyName = ToPascalCase(field.Name);
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// 导航属性: ICollection&lt;{navType}&gt;（对多关系）");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine(
                    $"    public System.Collections.Generic.ICollection<{navType}> {navPropertyName} {{ get; set; }} = new System.Collections.Generic.List<{navType}>();");
                sb.AppendLine();
            }
            else if (attr.Name is "ref" or "Ref" or "fk" or "Fk")
            {
                var navType = attr.Arguments.Count > 0 ? attr.Arguments[0].Value : field.Name;
                var navPropertyName = ToPascalCase(navType);
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// 导航属性: {navType}（外键引用）");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public {navType} {navPropertyName} {{ get; set; }} = null!;");
                sb.AppendLine();
            }
    }

    #endregion

    #region Enum/Flags/Union 生成

    private GeneratedFile GenerateEnum(EnumDefinition enumDef, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        sb.AppendLine($"public enum {enumDef.Name}");
        sb.AppendLine("{");

        var members = enumDef.Members.ToList();
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            var suffix = i < members.Count - 1 ? "," : "";
            sb.AppendLine($"    {member.Name} = {member.Value}{suffix}");
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{enumDef.Name}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = enumDef.Name
        };
    }

    private GeneratedFile GenerateFlags(FlagsDefinition flagsDef, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        sb.AppendLine("[Flags]");
        sb.AppendLine($"public enum {flagsDef.Name}");
        sb.AppendLine("{");

        var members = flagsDef.Members.ToList();
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            var suffix = i < members.Count - 1 ? "," : "";
            sb.AppendLine($"    {member.Name} = 1 << {member.Value}{suffix}");
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{flagsDef.Name}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = flagsDef.Name
        };
    }

    private GeneratedFile GenerateUnion(UnionDefinition unionDef, string ns, string target, string schemaPath,
        string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        var imports = new HashSet<string> { "System", "System.Collections.Generic" };
        if (target == "json")
        {
            imports.Add("System.Text.Json");
            imports.Add("System.Text.Json.Serialization");
        }

        foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"using {imp};");

        sb.AppendLine();

        if (target == "json")
        {
            sb.AppendLine("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"$kind\")]");
            var derivedTypes = unionDef.Variants
                .Select(v => $"[JsonDerivedType(typeof({unionDef.Name}.{v.Name}), \"{v.Name}\")]")
                .ToList();
            foreach (var dt in derivedTypes) sb.AppendLine(dt);
        }

        sb.AppendLine($"public abstract record {unionDef.Name}");
        sb.AppendLine("{");

        #region 工厂方法

        foreach (var variant in unionDef.Variants)
        {
            var param = variant.Payload != null ? $"{MapType(variant.Payload)} value" : "";
            sb.AppendLine(
                $"    public static {unionDef.Name} {variant.Name}({param}) => new {variant.Name}Variant({(variant.Payload != null ? "value" : "")});");
            sb.AppendLine();
        }

        #endregion

        #region 变体类型

        foreach (var variant in unionDef.Variants)
        {
            if (variant.Payload != null)
                sb.AppendLine(
                    $"    public sealed record {variant.Name}Variant({MapType(variant.Payload)} Value) : {unionDef.Name};");
            else
                sb.AppendLine($"    public sealed record {variant.Name}Variant : {unionDef.Name};");

            sb.AppendLine();
        }

        #endregion

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{unionDef.Name}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = unionDef.Name
        };
    }

    #endregion

    #region Repository 生成

    private GeneratedFile GenerateRepositoryInterface(StorageDefinition storage, string ns, string target,
        string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        var repositoryName = $"{ToPascalCase(storage.Name)}Repository";
        var interfaceName = $"I{repositoryName}";
        var isPostgres = target == "ef-pgsql";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");

        if (isPostgres) sb.AppendLine("using Hermes.YYDB.Query;");

        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {storage.Name} 仓储接口");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public interface {interfaceName}");
        sb.AppendLine("{");

        foreach (var model in storage.Models)
        {
            var modelName = ToPascalCase(model.Name);
            var keyType = GetKeyType(model);

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 根据主键查找 {modelName}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    Task<{modelName}?> Find{modelName}ByIdAsync({keyType} id, CancellationToken cancellationToken = default);");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 查询所有 {modelName}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    Task<List<{modelName}>> GetAll{modelName}sAsync(CancellationToken cancellationToken = default);");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 分页查询 {modelName}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    Task<(List<{modelName}> Items, int TotalCount)> Get{modelName}sPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 条件查询 {modelName}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    Task<List<{modelName}>> Query{modelName}sAsync(Func<{modelName}, bool> predicate, CancellationToken cancellationToken = default);");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 创建 {modelName}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    Task<{modelName}> Create{modelName}Async({modelName} entity, CancellationToken cancellationToken = default);");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 批量创建 {modelName}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    Task<int> Create{modelName}BatchAsync(IEnumerable<{modelName}> entities, CancellationToken cancellationToken = default);");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 更新 {modelName}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    Task<{modelName}> Update{modelName}Async({modelName} entity, CancellationToken cancellationToken = default);");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 删除 {modelName}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    Task Delete{modelName}Async({keyType} id, CancellationToken cancellationToken = default);");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 统计 {modelName} 数量");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    Task<long> Count{modelName}Async(CancellationToken cancellationToken = default);");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 判断 {modelName} 是否存在");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(
                $"    Task<bool> Exists{modelName}Async({keyType} id, CancellationToken cancellationToken = default);");
            sb.AppendLine();

            if (isPostgres)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// 插入或更新 {modelName}（PostgreSQL ON CONFLICT DO UPDATE）");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine(
                    $"    Task<{modelName}> Upsert{modelName}Async({modelName} entity, CancellationToken cancellationToken = default);");
                sb.AppendLine();

                var ftsFields = GetFullTextSearchFields(model);
                if (ftsFields.Count > 0)
                {
                    sb.AppendLine("    /// <summary>");
                    sb.AppendLine($"    /// 全文搜索 {modelName}（PostgreSQL TSVECTOR）");
                    sb.AppendLine("    /// </summary>");
                    sb.AppendLine(
                        $"    Task<List<{modelName}>> Search{modelName}sAsync(string searchTerm, string language = \"simple\", int limit = 50, CancellationToken cancellationToken = default);");
                    sb.AppendLine();
                }

                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// 按窗口函数排名查询 {modelName}");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine(
                    $"    Task<List<Ranked{modelName}>> Get{modelName}sRankedAsync(string partitionField, string orderField, bool descending = true, int? limit = null, CancellationToken cancellationToken = default);");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{interfaceName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = interfaceName
        };
    }

    private GeneratedFile GenerateRepositoryImplementation(StorageDefinition storage, string ns, string target,
        string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        var repositoryName = $"{ToPascalCase(storage.Name)}Repository";
        var interfaceName = $"I{repositoryName}";
        var isPostgres = target == "ef-pgsql";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Hermes.ORM;");
        sb.AppendLine("using Hermes.YYDB.Query;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {storage.Name} 仓储实现");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public sealed class {repositoryName} : {interfaceName}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IQueryExecutor _executor;");
        sb.AppendLine();
        sb.AppendLine($"    public {repositoryName}(IQueryExecutor executor)");
        sb.AppendLine("    {");
        sb.AppendLine("        _executor = executor;");
        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var model in storage.Models)
        {
            var modelName = ToPascalCase(model.Name);
            var keyType = GetKeyType(model);
            var keyField = GetKeyField(model);
            var keyPropName = keyField != null ? ToPascalCase(keyField.Name) : "Id";

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task<{modelName}?> Find{modelName}ByIdAsync({keyType} id, CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return await _executor.Query<{modelName}>()");
            sb.AppendLine($"            .Filter(e => EF.Property<{keyType}>(e, \"{keyPropName}\") == id)");
            sb.AppendLine("            .FirstOrDefaultAsync(cancellationToken);");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task<List<{modelName}>> GetAll{modelName}sAsync(CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return await _executor.Query<{modelName}>()");
            sb.AppendLine("            .ToListAsync(cancellationToken);");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task<(List<{modelName}> Items, int TotalCount)> Get{modelName}sPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var query = _executor.Query<{modelName}>();");
            sb.AppendLine("        var totalCount = (int)await query.CountAsync(cancellationToken);");
            sb.AppendLine($"        var items = await _executor.Query<{modelName}>()");
            sb.AppendLine("            .Skip((page - 1) * pageSize)");
            sb.AppendLine("            .Take(pageSize)");
            sb.AppendLine("            .ToListAsync(cancellationToken);");
            sb.AppendLine("        return (items, totalCount);");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task<List<{modelName}>> Query{modelName}sAsync(Func<{modelName}, bool> predicate, CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var all = await _executor.Query<{modelName}>().ToListAsync(cancellationToken);");
            sb.AppendLine("        return all.Where(predicate).ToList();");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task<{modelName}> Create{modelName}Async({modelName} entity, CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        await _executor.Query<{modelName}>().InsertAsync(entity, cancellationToken);");
            sb.AppendLine("        return entity;");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task<int> Create{modelName}BatchAsync(IEnumerable<{modelName}> entities, CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine("        var count = 0;");
            sb.AppendLine("        foreach (var entity in entities)");
            sb.AppendLine("        {");
            sb.AppendLine($"            await _executor.Query<{modelName}>().InsertAsync(entity, cancellationToken);");
            sb.AppendLine("            count++;");
            sb.AppendLine("        }");
            sb.AppendLine("        return count;");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task<{modelName}> Update{modelName}Async({modelName} entity, CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var builder = _executor.Query<{modelName}>()");
            sb.AppendLine($"            .Filter(e => e.{keyPropName} == entity.{keyPropName});");
            sb.AppendLine();
            sb.AppendLine(
                $"        foreach (var prop in typeof({modelName}).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))");
            sb.AppendLine("        {");
            sb.AppendLine($"            if (prop.Name != \"{keyPropName}\" && prop.CanRead && prop.CanWrite)");
            sb.AppendLine("            {");
            sb.AppendLine("                var value = prop.GetValue(entity);");
            sb.AppendLine("                if (value != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    builder = builder.Set(prop.Name, value);");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        await builder.UpdateAsync(cancellationToken);");
            sb.AppendLine("        return entity;");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task Delete{modelName}Async({keyType} id, CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        await _executor.Query<{modelName}>()");
            sb.AppendLine($"            .Filter(e => e.{keyPropName} == id)");
            sb.AppendLine("            .DeleteAsync(cancellationToken);");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task<long> Count{modelName}Async(CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return await _executor.Query<{modelName}>().CountAsync(cancellationToken);");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine(
                $"    public async Task<bool> Exists{modelName}Async({keyType} id, CancellationToken cancellationToken = default)");
            sb.AppendLine("    {");
            sb.AppendLine("        return await _executor.Query<" + modelName + ">()");
            sb.AppendLine("            .Filter(e => e." + keyPropName + " == id)");
            sb.AppendLine("            .AnyAsync(cancellationToken);");
            sb.AppendLine("    }");
            sb.AppendLine();

            if (isPostgres)
            {
                sb.AppendLine("    /// <inheritdoc />");
                sb.AppendLine(
                    $"    public async Task<{modelName}> Upsert{modelName}Async({modelName} entity, CancellationToken cancellationToken = default)");
                sb.AppendLine("    {");
                sb.AppendLine("        var assignments = new List<FieldAssignment>();");
                sb.AppendLine(
                    $"        foreach (var prop in typeof({modelName}).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))");
                sb.AppendLine("        {");
                sb.AppendLine("            if (prop.CanRead)");
                sb.AppendLine("            {");
                sb.AppendLine("                var value = prop.GetValue(entity);");
                sb.AppendLine("                if (value != null)");
                sb.AppendLine("                {");
                sb.AppendLine("                    assignments.Add(new FieldAssignment(prop.Name, value));");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        var conflictFields = new List<string> {{ \"{keyPropName}\" }};");
                sb.AppendLine("        var upsertQuery = new UpsertQuery(");
                sb.AppendLine($"            \"{modelName}\",");
                sb.AppendLine("            assignments,");
                sb.AppendLine("            conflictFields,");
                sb.AppendLine("            UpsertStrategy.DoUpdate);");
                sb.AppendLine();
                sb.AppendLine("        await _executor.ExecuteRawAsync(upsertQuery, cancellationToken);");
                sb.AppendLine("        return entity;");
                sb.AppendLine("    }");
                sb.AppendLine();

                var ftsFields = GetFullTextSearchFields(model);
                if (ftsFields.Count > 0)
                {
                    var ftsFieldNames = string.Join(", ", ftsFields.Select(f => $"\"{f}\""));
                    sb.AppendLine("    /// <inheritdoc />");
                    sb.AppendLine(
                        $"    public async Task<List<{modelName}>> Search{modelName}sAsync(string searchTerm, string language = \"simple\", int limit = 50, CancellationToken cancellationToken = default)");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        var searchFields = new List<string> {{ {ftsFieldNames} }};");
                    sb.AppendLine("        var ftsQuery = new FullTextSearchQuery(");
                    sb.AppendLine($"            \"{modelName}\",");
                    sb.AppendLine("            searchFields,");
                    sb.AppendLine("            searchTerm,");
                    sb.AppendLine("            language,");
                    sb.AppendLine("            FullTextSearchMode.Plain,");
                    sb.AppendLine("            pagination: new QueryPagination(0, limit),");
                    sb.AppendLine("            orderByRank: true);");
                    sb.AppendLine();
                    sb.AppendLine("        var result = await _executor.ExecuteRawAsync(ftsQuery, cancellationToken);");
                    sb.AppendLine("        return result.Rows;");
                    sb.AppendLine("    }");
                    sb.AppendLine();
                }

                sb.AppendLine("    /// <inheritdoc />");
                sb.AppendLine(
                    $"    public async Task<List<Ranked{modelName}>> Get{modelName}sRankedAsync(string partitionField, string orderField, bool descending = true, int? limit = null, CancellationToken cancellationToken = default)");
                sb.AppendLine("    {");
                sb.AppendLine("        var windowQuery = new WindowQuery(");
                sb.AppendLine($"            \"{modelName}\",");
                sb.AppendLine("            new List<WindowFunctionDefinition>");
                sb.AppendLine("            {");
                sb.AppendLine("                new WindowFunctionDefinition(");
                sb.AppendLine("                    WindowFunctionKind.RowNumber,");
                sb.AppendLine("                    orderField,");
                sb.AppendLine("                    \"row_number\",");
                sb.AppendLine("                    partitionBy: new List<string> { partitionField },");
                sb.AppendLine(
                    "                    orderBy: new List<WindowOrderItem> { new(orderField, descending) })");
                sb.AppendLine("            },");
                sb.AppendLine("            pagination: limit.HasValue ? new QueryPagination(0, limit.Value) : null);");
                sb.AppendLine();
                sb.AppendLine("        var result = await _executor.ExecuteRawAsync(windowQuery, cancellationToken);");
                sb.AppendLine(
                    $"        return result.Rows.Select(r => new Ranked{modelName}(r, (int)(r.RowNumber ?? 0))).ToList();");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{repositoryName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = repositoryName
        };
    }

    /// <summary>
    ///     获取模型的主键类型
    /// </summary>
    private string GetKeyType(ModelDefinition model)
    {
        var keyField = GetKeyField(model);
        if (keyField != null) return MapType(keyField.FieldType);

        return "long";
    }

    /// <summary>
    ///     获取模型的主键字段
    /// </summary>
    private FieldDefinition? GetKeyField(ModelDefinition model)
    {
        foreach (var field in model.fields)
            if (IsKeyField(field))
                return field;

        return null;
    }

    private static bool IsKeyField(FieldDefinition field)
    {
        foreach (var attr in field.Attributes)
            if (attr.Name is "key" or "Key" or "id" or "Id")
                return true;

        return false;
    }

    /// <summary>
    ///     获取标记为全文搜索的字段列表
    /// </summary>
    private static List<string> GetFullTextSearchFields(ModelDefinition model)
    {
        var ftsFields = new List<string>();

        foreach (var field in model.fields)
        foreach (var attr in field.Attributes)
            if (attr.Name is "fulltext" or "fullText" or "FullText" or "fts" or "Fts")
                ftsFields.Add(ToSnakeCase(field.Name));

        return ftsFields;
    }

    #endregion

    #region DDL 生成

    private GeneratedFile GenerateDdlScript(SchemaIR schema, string ns, string target, string schemaPath,
        string outputPath)
    {
        var dialect = target switch
        {
            "ef-mysql" => "mysql",
            "ef-pgsql" => "postgresql",
            _ => "sqlite"
        };

        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("-- Hermes 自动生成的 DDL 脚本");
        sb.AppendLine($"-- Schema: {schema.Namespace}");
        sb.AppendLine($"-- 方言: {dialect}");
        sb.AppendLine($"-- 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        foreach (var storage in schema.Storages)
        {
            sb.AppendLine("-- ========================================");
            sb.AppendLine($"-- Storage: {storage.Name}");
            sb.AppendLine("-- ========================================");
            sb.AppendLine();

            foreach (var model in storage.Models)
            {
                GenerateCreateTable(model, sb, dialect);
                sb.AppendLine();
            }

            foreach (var model in storage.Models) GenerateIndexes(model, sb, dialect);

            foreach (var model in storage.Models) GenerateForeignKeys(model, sb, dialect);
        }

        var suffix = dialect == "mysql" ? "_mysql" : dialect == "postgresql" ? "_pgsql" : "";
        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{ToPascalCase(schema.Namespace)}_ddl{suffix}.sql"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = $"{ToPascalCase(schema.Namespace)}Ddl"
        };
    }

    private void GenerateCreateTable(ModelDefinition model, StringBuilder sb, string dialect)
    {
        var tableName = ToSnakeCase(model.Name);
        var (ql, qr) = GetQuoteChars(dialect);

        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {ql}{tableName}{qr}");
        sb.AppendLine("(");

        var columns = new List<string>();

        foreach (var field in model.fields)
        {
            var colName = ToSnakeCase(field.Name);
            var sqlType = MapSqlColumnType(field.FieldType, dialect);
            var nullable = field.IsOptional ? "" : " NOT NULL";

            if (IsKeyField(field))
            {
                sqlType = MapKeyColumnType(field.FieldType, dialect);
                var autoInc = dialect switch
                {
                    "mysql" => " AUTO_INCREMENT",
                    "postgresql" => " GENERATED ALWAYS AS IDENTITY",
                    _ => " AUTOINCREMENT"
                };
                columns.Add($"    {ql}{colName}{qr} {sqlType}{nullable} PRIMARY KEY{autoInc}");
            }
            else
            {
                var defaultValue = "";
                if (field.IsOptional) defaultValue = " DEFAULT NULL";

                var onUpdateAttr =
                    field.Attributes.FirstOrDefault(a => a.Name is "onUpdate" or "on_update" or "OnUpdate");
                if (onUpdateAttr != null && dialect == "mysql")
                {
                    var onUpdateValue = onUpdateAttr.Arguments.Count > 0
                        ? onUpdateAttr.Arguments[0].Value
                        : "CURRENT_TIMESTAMP";
                    defaultValue += $" ON UPDATE {onUpdateValue}";
                }

                columns.Add($"    {ql}{colName}{qr} {sqlType}{nullable}{defaultValue}");
            }
        }

        sb.AppendLine(string.Join(",\n", columns));
        sb.Append(")");

        if (dialect == "mysql")
            sb.AppendLine(" ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
        else
            sb.AppendLine(");");

        if (dialect == "postgresql")
        {
            var inheritAttr = model.Attributes.FirstOrDefault(a => a.Name is "inherits" or "Inherits" or "inherit");
            if (inheritAttr != null && inheritAttr.Arguments.Count > 0)
            {
                var parentTable = ToSnakeCase(inheritAttr.Arguments[0].Value);
                sb.AppendLine();
                sb.AppendLine($"ALTER TABLE {ql}{tableName}{qr} INHERIT {ql}{parentTable}{qr};");
            }

            var partitionAttr =
                model.Attributes.FirstOrDefault(a => a.Name is "partition" or "Partition" or "partitionBy");
            if (partitionAttr != null && partitionAttr.Arguments.Count > 0)
            {
                var partitionType = partitionAttr.Arguments[0].Value.ToUpperInvariant();
                var partitionColumn = partitionAttr.Arguments.Count > 1
                    ? ToSnakeCase(partitionAttr.Arguments[1].Value)
                    : "id";
                sb.AppendLine();
                sb.AppendLine($"-- 分区表: {partitionType} BY RANGE ({ql}{partitionColumn}{qr})");
            }
        }

        sb.AppendLine();
    }

    private void GenerateIndexes(ModelDefinition model, StringBuilder sb, string dialect)
    {
        var tableName = ToSnakeCase(model.Name);
        var (ql, qr) = GetQuoteChars(dialect);

        foreach (var field in model.fields)
        {
            var uniqueAttr = field.Attributes.FirstOrDefault(a => a.Name is "unique" or "Unique");
            if (uniqueAttr != null)
            {
                var colName = ToSnakeCase(field.Name);
                sb.AppendLine(
                    $"CREATE UNIQUE INDEX IF NOT EXISTS {ql}ix_{tableName}_{colName}{qr} ON {ql}{tableName}{qr} ({ql}{colName}{qr});");
            }

            var indexAttr = field.Attributes.FirstOrDefault(a => a.Name is "index" or "Index");
            if (indexAttr != null)
            {
                var colName = ToSnakeCase(field.Name);
                sb.AppendLine(
                    $"CREATE INDEX IF NOT EXISTS {ql}ix_{tableName}_{colName}{qr} ON {ql}{tableName}{qr} ({ql}{colName}{qr});");
            }

            var ftsAttr =
                field.Attributes.FirstOrDefault(a =>
                    a.Name is "fulltext" or "fullText" or "FullText" or "fts" or "Fts");
            if (ftsAttr != null)
            {
                var colName = ToSnakeCase(field.Name);
                if (dialect == "mysql")
                    sb.AppendLine(
                        $"CREATE FULLTEXT INDEX IF NOT EXISTS {ql}ft_{tableName}_{colName}{qr} ON {ql}{tableName}{qr} ({ql}{colName}{qr});");
                else if (dialect == "postgresql")
                    sb.AppendLine(
                        $"CREATE INDEX IF NOT EXISTS {ql}ft_{tableName}_{colName}{qr} ON {ql}{tableName}{qr} USING GIN (TO_TSVECTOR('simple', {ql}{colName}{qr}));");
            }
        }
    }

    private void GenerateForeignKeys(ModelDefinition model, StringBuilder sb, string dialect)
    {
        var tableName = ToSnakeCase(model.Name);
        var (ql, qr) = GetQuoteChars(dialect);

        foreach (var field in model.fields)
        {
            if (!field.IsForeignKey) continue;

            var refAttr = field.Attributes.FirstOrDefault(a => a.Name is "ref" or "Ref" or "fk" or "Fk");
            if (refAttr != null && refAttr.Arguments.Count > 0)
            {
                var targetEntity = refAttr.Arguments[0].Value;
                var colName = ToSnakeCase(field.Name);
                var targetTable = ToSnakeCase(targetEntity);
                var targetCol = "id";

                if (refAttr.Arguments.Count > 1) targetCol = ToSnakeCase(refAttr.Arguments[1].Value);

                var fkName = $"fk_{tableName}_{colName}";
                var onDelete = dialect == "mysql" ? " ON DELETE RESTRICT ON UPDATE CASCADE" : "";
                sb.AppendLine($"ALTER TABLE {ql}{tableName}{qr} ADD CONSTRAINT {ql}{fkName}{qr}");
                sb.AppendLine(
                    $"    FOREIGN KEY ({ql}{colName}{qr}) REFERENCES {ql}{targetTable}{qr} ({ql}{targetCol}{qr}){onDelete};");
            }
        }
    }

    private static (string Left, string Right) GetQuoteChars(string dialect)
    {
        return dialect switch
        {
            "mysql" => ("`", "`"),
            _ => ("\"", "\"")
        };
    }

    #region Migration 生成

    private GeneratedFile GenerateMigrationUp(SchemaIR schema, string ns, string target, string schemaPath,
        string outputPath)
    {
        var dialect = target switch
        {
            "ef-mysql" => "mysql",
            "ef-pgsql" => "postgresql",
            _ => "sqlite"
        };
        var (ql, qr) = GetQuoteChars(dialect);
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("-- Hermes Migration: UP");
        sb.AppendLine($"-- Schema: {schema.Namespace}");
        sb.AppendLine($"-- 方言: {dialect}");
        sb.AppendLine($"-- 版本: {DateTime.Now:yyyyMMddHHmmss}");
        sb.AppendLine();

        foreach (var storage in schema.Storages)
        foreach (var model in storage.Models)
        {
            var tableName = ToSnakeCase(model.Name);
            sb.AppendLine($"-- 创建表 {tableName}");
            GenerateCreateTable(model, sb, dialect);
        }

        var suffix = dialect == "mysql" ? "_mysql" : dialect == "postgresql" ? "_pgsql" : "";
        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, "migrations", $"{DateTime.Now:yyyyMMddHHmmss}_up{suffix}.sql"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = "MigrationUp"
        };
    }

    private GeneratedFile GenerateMigrationDown(SchemaIR schema, string ns, string target, string schemaPath,
        string outputPath)
    {
        var dialect = target switch
        {
            "ef-mysql" => "mysql",
            "ef-pgsql" => "postgresql",
            _ => "sqlite"
        };
        var (ql, qr) = GetQuoteChars(dialect);
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("-- Hermes Migration: DOWN");
        sb.AppendLine($"-- Schema: {schema.Namespace}");
        sb.AppendLine($"-- 方言: {dialect}");
        sb.AppendLine($"-- 版本: {DateTime.Now:yyyyMMddHHmmss}");
        sb.AppendLine();

        foreach (var storage in schema.Storages)
        {
            foreach (var model in storage.Models)
            {
                var tableName = ToSnakeCase(model.Name);

                foreach (var field in model.fields)
                {
                    if (!field.IsForeignKey) continue;

                    var refAttr = field.Attributes.FirstOrDefault(a => a.Name is "ref" or "Ref" or "fk" or "Fk");
                    if (refAttr != null && refAttr.Arguments.Count > 0)
                    {
                        var colName = ToSnakeCase(field.Name);
                        var fkName = $"fk_{tableName}_{colName}";
                        sb.AppendLine($"ALTER TABLE {ql}{tableName}{qr} DROP CONSTRAINT {ql}{fkName}{qr};");
                    }
                }
            }

            sb.AppendLine();

            foreach (var model in storage.Models)
            {
                var tableName = ToSnakeCase(model.Name);
                sb.AppendLine($"DROP TABLE IF EXISTS {ql}{tableName}{qr};");
            }
        }

        var suffix = dialect == "mysql" ? "_mysql" : dialect == "postgresql" ? "_pgsql" : "";
        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, "migrations", $"{DateTime.Now:yyyyMMddHHmmss}_down{suffix}.sql"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = "MigrationDown"
        };
    }

    #endregion

    #endregion

    #region 类型映射

    private HashSet<string> CollectImports(ClassDefinition classDef, string target)
    {
        var imports = new HashSet<string>();

        foreach (var field in classDef.fields) CollectTypeImports(field.FieldType, imports);

        if (target == "json") imports.Add("System.Text.Json.Serialization");

        return imports;
    }

    private void CollectTypeImports(SchemaType type, HashSet<string> imports)
    {
        switch (type)
        {
            case ListType:
                imports.Add("System.Collections.Generic");
                break;
            case DictType:
                imports.Add("System.Collections.Generic");
                break;
            case RecordType:
                imports.Add("System.Collections.Generic");
                break;
            case ResultType:
                imports.Add("System");
                break;
            case OptionType o:
                CollectTypeImports(o.InnerType, imports);
                break;
            case StreamType:
                imports.Add("System.Collections.Generic");
                break;
            case ArrayType a:
                CollectTypeImports(a.ElementType, imports);
                break;
        }
    }

    private string MapType(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" => "sbyte",
                "i16" => "short",
                "i32" => "int",
                "i64" => "long",
                "u8" => "byte",
                "u16" => "ushort",
                "u32" => "uint",
                "u64" => "ulong",
                "f32" => "float",
                "f64" => "double",
                "bool" => "bool",
                "utf8" => "string",
                "utf16" => "string",
                "uuid" => "Guid",
                "unit" => "void",
                "object" => "object",
                "datetime" => "DateTime",
                "decimal" => "decimal",
                _ => "object"
            },
            ListType l => $"List<{MapType(l.ElementType)}>",
            ArrayType a => $"{MapType(a.ElementType)}[]",
            OptionType o => $"{MapType(o.InnerType)}?",
            DictType d => $"Dictionary<{MapType(d.KeyType)}, {MapType(d.ValueType)}>",
            RecordType r => $"Dictionary<{MapType(r.KeyType)}, {MapType(r.ValueType)}>",
            ResultType r => r.ErrorType != null
                ? $"Result<{MapType(r.OkType)}, {MapType(r.ErrorType)}>"
                : $"Result<{MapType(r.OkType)}>",
            StreamType s => $"IAsyncEnumerable<{MapType(s.InnerType)}>",
            ReferenceType refT => MapType(refT.ReferencedType),
            NamedType n => n.Name switch
            {
                "datetime" => "DateTime",
                "map" => "Dictionary<string, object>",
                "set" => "HashSet<string>",
                "bytes" => "byte[]",
                "decimal" => "decimal",
                "any" => "object",
                _ => n.Name
            },
            _ => "object"
        };
    }

    private static string? MapSqlType(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "utf8" or "utf16" => "nvarchar(max)",
                "uuid" => "uniqueidentifier",
                "decimal" => "decimal(18,2)",
                "f32" => "real",
                "f64" => "float",
                "i64" => "bigint",
                "i32" => "int",
                "i16" => "smallint",
                "i8" => "tinyint",
                "u64" => "bigint",
                "u32" => "int",
                "u16" => "smallint",
                "u8" => "tinyint",
                "bool" => "bit",
                "bytes" => "varbinary(max)",
                "datetime" => "datetime2",
                _ => null
            },
            NamedType n => n.Name switch
            {
                "datetime" => "datetime2",
                "decimal" => "decimal(18,2)",
                _ => null
            },
            _ => null
        };
    }

    private static string MapSqlColumnType(SchemaType type, string dialect)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" => dialect == "mysql" ? "TINYINT" : "SMALLINT",
                "i16" => "SMALLINT",
                "i32" => "INT",
                "i64" => "BIGINT",
                "u8" => dialect == "mysql" ? "TINYINT UNSIGNED" : "INTEGER",
                "u16" => dialect == "mysql" ? "SMALLINT UNSIGNED" : "INTEGER",
                "u32" => dialect == "mysql" ? "INT UNSIGNED" : "BIGINT",
                "u64" => dialect == "mysql" ? "BIGINT UNSIGNED" : "BIGINT",
                "f32" => dialect == "mysql" ? "FLOAT" : "REAL",
                "f64" => dialect == "mysql" ? "DOUBLE" : "DOUBLE",
                "bool" => dialect == "mysql" ? "TINYINT(1)" : "BOOLEAN",
                "utf8" => dialect == "mysql" ? "VARCHAR(255)" : "TEXT",
                "utf16" => dialect == "mysql" ? "VARCHAR(255)" : "TEXT",
                "uuid" => dialect == "mysql" ? "CHAR(36)" : dialect == "postgresql" ? "UUID" : "TEXT",
                "bytes" => dialect == "mysql" ? "MEDIUMBLOB" : "BLOB",
                "datetime" => dialect == "mysql" ? "DATETIME(6)" : dialect == "postgresql" ? "TIMESTAMPTZ" : "TEXT",
                "decimal" => dialect == "mysql" ? "DECIMAL(18,2)" : "REAL",
                _ => "TEXT"
            },
            ListType => dialect == "mysql" ? "JSON" : dialect == "postgresql" ? "JSONB" : "TEXT",
            ArrayType => dialect == "mysql" ? "JSON" : dialect == "postgresql" ? "JSONB" : "TEXT",
            DictType => dialect == "mysql" ? "JSON" : dialect == "postgresql" ? "JSONB" : "TEXT",
            RecordType => dialect == "mysql" ? "JSON" : dialect == "postgresql" ? "JSONB" : "TEXT",
            NamedType n => n.Name switch
            {
                "datetime" => dialect == "mysql" ? "DATETIME(6)" : dialect == "postgresql" ? "TIMESTAMPTZ" : "TEXT",
                "decimal" => dialect == "mysql" ? "DECIMAL(18,2)" : "REAL",
                "inet" => dialect == "postgresql" ? "INET" : "TEXT",
                "cidr" => dialect == "postgresql" ? "CIDR" : "TEXT",
                "macaddr" => dialect == "postgresql" ? "MACADDR" : "TEXT",
                "interval" => dialect == "postgresql" ? "INTERVAL" : "TEXT",
                "money" => dialect == "postgresql" ? "MONEY" : dialect == "mysql" ? "DECIMAL(19,4)" : "REAL",
                "point" => dialect == "postgresql" ? "POINT" : "TEXT",
                "json" => dialect == "mysql" ? "JSON" : dialect == "postgresql" ? "JSONB" : "TEXT",
                "tsvector" => dialect == "postgresql" ? "TSVECTOR" : "TEXT",
                "any" => "TEXT",
                _ => "TEXT"
            },
            _ => "TEXT"
        };
    }

    private static string MapKeyColumnType(SchemaType type, string dialect)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i32" => "INT",
                "i64" => "BIGINT",
                "uuid" => dialect == "mysql" ? "CHAR(36)" : dialect == "postgresql" ? "UUID" : "TEXT",
                "utf8" => dialect == "mysql" ? "VARCHAR(255)" : "TEXT",
                _ => "INT"
            },
            NamedType n => n.Name switch
            {
                "uuid" => dialect == "mysql" ? "CHAR(36)" : dialect == "postgresql" ? "UUID" : "TEXT",
                _ => "INT"
            },
            _ => "INT"
        };
    }

    #endregion

    #region DTO 生成

    /// <summary>
    ///     为模型生成 DTO 类文件
    /// </summary>
    private GeneratedFile GenerateDto(ModelDefinition modelDef, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        var imports = new HashSet<string>();
        foreach (var field in modelDef.fields) CollectDtoTypeImports(field.FieldType, imports);

        if (imports.Count > 0)
        {
            foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"using {imp};");

            sb.AppendLine();
        }

        var fieldGroups = BuildFieldDtoGroups(modelDef);
        var allGroups = CollectAllDtoGroups(fieldGroups);

        foreach (var group in allGroups)
        {
            GenerateDtoRecord(sb, modelDef, group, fieldGroups);
            sb.AppendLine();
        }

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{modelDef.Name}Dto.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = $"{modelDef.Name}Dto"
        };
    }

    /// <summary>
    ///     为 DTO 字段收集所需的 using 导入
    /// </summary>
    private void CollectDtoTypeImports(SchemaType type, HashSet<string> imports)
    {
        switch (type)
        {
            case ListType:
                imports.Add("System.Collections.Generic");
                break;
            case DictType:
                imports.Add("System.Collections.Generic");
                break;
            case RecordType:
                imports.Add("System.Collections.Generic");
                break;
            case OptionType o:
                CollectDtoTypeImports(o.InnerType, imports);
                break;
            case ArrayType a:
                CollectDtoTypeImports(a.ElementType, imports);
                break;
            case ReferenceType refT:
                CollectDtoTypeImports(refT.ReferencedType, imports);
                break;
        }
    }

    /// <summary>
    ///     为每个字段确定其所属的 DTO 组
    /// </summary>
    private Dictionary<FieldDefinition, List<string>> BuildFieldDtoGroups(ModelDefinition modelDef)
    {
        var result = new Dictionary<FieldDefinition, List<string>>();

        foreach (var field in modelDef.fields)
        {
            var groups = InferDtoGroups(field);
            result[field] = groups;
        }

        return result;
    }

    /// <summary>
    ///     推断字段的 DTO 组（显式标注优先，否则按默认约定）
    /// </summary>
    private List<string> InferDtoGroups(FieldDefinition field)
    {
        if (field.DtoGroups.Count > 0) return field.DtoGroups.ToList();

        var normalizedName = field.Name.ToLowerInvariant().Replace("@", "");
        if (normalizedName is "row_version" or "concurrency_token" or "_version" or "version") return ["entity_dto"];

        var groups = new List<string>();

        var isReference = IsSingleReference(field) || IsListReference(field);
        var isPrimaryKey = field.IsPrimaryKey;
        var isDatetime = IsDatetimeType(field.FieldType);
        var hasDefault = field.DefaultValue is not null;
        var isNullable = field.IsOptional;

        if (isPrimaryKey)
        {
            groups.Add("detail");
        }
        else if (isReference)
        {
            groups.Add("detail");
        }
        else if (isDatetime && !hasDefault)
        {
            groups.Add("detail");
        }
        else if (isNullable)
        {
            groups.Add("detail");
            groups.Add("update");
        }
        else if (hasDefault)
        {
            groups.Add("detail");
            groups.Add("update");
        }
        else
        {
            groups.Add("create");
            groups.Add("detail");
        }

        return groups;
    }

    /// <summary>
    ///     收集模型中所有唯一的 DTO 组名
    /// </summary>
    private List<string> CollectAllDtoGroups(Dictionary<FieldDefinition, List<string>> fieldGroups)
    {
        var groups = new HashSet<string> { "entity_dto", "detail", "create", "update" };

        foreach (var groupList in fieldGroups.Values)
        foreach (var g in groupList)
            groups.Add(g);

        return [.. groups.OrderBy(g => g)];
    }

    /// <summary>
    ///     生成单个 DTO record 类
    /// </summary>
    private void GenerateDtoRecord(StringBuilder sb, ModelDefinition modelDef, string group,
        Dictionary<FieldDefinition, List<string>> fieldGroups)
    {
        var (className, fields) = ResolveDtoClass(modelDef, group, fieldGroups);

        if (fields.Count == 0) return;

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {modelDef.Name} 的 {group} DTO");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public sealed record {className}");
        sb.AppendLine("{");

        foreach (var field in fields)
        {
            var propertyName = ToPascalCase(field.Name.TrimStart('@'));
            var csType = MapDtoType(field, group);
            var nullableSuffix = GetDtoNullableSuffix(field, group);

            if (field.DocComment is not null)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {field.DocComment}");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    public {csType}{nullableSuffix} {propertyName} {{ get; set; }}");
            sb.AppendLine();
        }

        sb.AppendLine("}");
    }

    /// <summary>
    ///     解析 DTO 类名和包含的字段列表
    /// </summary>
    private (string ClassName, List<FieldDefinition> Fields) ResolveDtoClass(ModelDefinition modelDef, string group,
        Dictionary<FieldDefinition, List<string>> fieldGroups)
    {
        var modelName = modelDef.Name;

        switch (group)
        {
            case "entity_dto":
            {
                var fields = fieldGroups
                    .Where(kvp => kvp.Value.Contains("entity_dto") || !IsInternalField(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList();
                return ($"{modelName}Dto", fields);
            }
            case "detail":
            {
                var fields = fieldGroups
                    .Where(kvp => kvp.Value.Contains("detail"))
                    .Select(kvp => kvp.Key)
                    .ToList();
                return ($"{modelName}DetailDto", fields);
            }
            case "create":
            {
                var fields = fieldGroups
                    .Where(kvp => kvp.Value.Contains("create") && !kvp.Key.IsPrimaryKey)
                    .Select(kvp => kvp.Key)
                    .ToList();
                return ($"{modelName}CreateRequest", fields);
            }
            case "update":
            {
                var fields = fieldGroups
                    .Where(kvp => kvp.Value.Contains("update"))
                    .Select(kvp => kvp.Key)
                    .ToList();
                return ($"{modelName}UpdateRequest", fields);
            }
            default:
            {
                var fields = fieldGroups
                    .Where(kvp => kvp.Value.Contains(group))
                    .Select(kvp => kvp.Key)
                    .ToList();
                return ($"{modelName}{ToPascalCase(group)}Dto", fields);
            }
        }
    }

    /// <summary>
    ///     映射 DTO 字段的 C# 类型
    /// </summary>
    private string MapDtoType(FieldDefinition field, string group)
    {
        if (IsSingleReference(field))
        {
            if (group is "detail" or "entity_dto")
            {
                var refModelName = GetReferenceModelName(field.FieldType);
                var isNullable = field.FieldType is OptionType;
                return $"{refModelName}Dto{(isNullable ? "?" : "")}";
            }

            return MapType(field.FieldType);
        }

        if (IsListReference(field))
        {
            if (group is "detail" or "entity_dto")
            {
                var refModelName = GetListReferenceModelName(field.FieldType);
                var isNullable = field.FieldType is OptionType;
                return $"List<{refModelName}Dto>{(isNullable ? "?" : "")}";
            }

            return MapType(field.FieldType);
        }

        return MapType(field.FieldType);
    }

    /// <summary>
    ///     获取 DTO 字段的可空后缀
    /// </summary>
    private static string GetDtoNullableSuffix(FieldDefinition field, string group)
    {
        if (IsSingleReference(field) || IsListReference(field)) return "";

        if (group == "update")
        {
            var csType = field.FieldType switch
            {
                PrimitiveType p => p.TypeName,
                NamedType n => n.Name,
                _ => ""
            };

            if (csType is "utf8" or "utf16" or "object" or "any") return "";

            if (field.FieldType is OptionType) return "";

            return "?";
        }

        return NullableSuffix(field);
    }

    /// <summary>
    ///     判断字段是否为单引用类型（&Model 或 &Model?）
    /// </summary>
    private static bool IsSingleReference(FieldDefinition field)
    {
        return field.FieldType is ReferenceType or OptionType { InnerType: ReferenceType };
    }

    /// <summary>
    ///     判断字段是否为列表引用类型（[&Model] 或 [&Model]?）
    /// </summary>
    private static bool IsListReference(FieldDefinition field)
    {
        return field.FieldType is ListType { ElementType: ReferenceType }
            or OptionType { InnerType: ListType { ElementType: ReferenceType } };
    }

    /// <summary>
    ///     判断类型是否为 datetime/timestamp
    /// </summary>
    private static bool IsDatetimeType(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName is "datetime",
            NamedType n => n.Name is "datetime" or "timestamp",
            OptionType o => IsDatetimeType(o.InnerType),
            _ => false
        };
    }

    /// <summary>
    ///     判断字段是否为内部字段（仅属于 entity_dto）
    /// </summary>
    private static bool IsInternalField(FieldDefinition field)
    {
        var normalizedName = field.Name.ToLowerInvariant().Replace("@", "");
        return normalizedName is "row_version" or "concurrency_token" or "_version" or "version";
    }

    /// <summary>
    ///     获取单引用类型的目标模型名（处理 OptionType 包裹 ReferenceType 和 ReferenceType 包裹 OptionType 两种情况）
    /// </summary>
    private static string GetReferenceModelName(SchemaType type)
    {
        var refT = type switch
        {
            ReferenceType r => r,
            OptionType { InnerType: ReferenceType r } => r,
            _ => null
        };

        if (refT != null)
            return refT.ReferencedType switch
            {
                NamedType n => n.Name,
                PrimitiveType p => p.TypeName,
                OptionType { InnerType: NamedType n } => n.Name,
                OptionType { InnerType: PrimitiveType p } => p.TypeName,
                _ => refT.ReferencedType.TypeName
            };

        return type.TypeName;
    }

    /// <summary>
    ///     获取列表引用类型的目标模型名（处理 OptionType 包裹的情况）
    /// </summary>
    private static string GetListReferenceModelName(SchemaType type)
    {
        var refT = type switch
        {
            ListType { ElementType: ReferenceType r } => r,
            OptionType { InnerType: ListType { ElementType: ReferenceType r } } => r,
            _ => null
        };

        if (refT != null)
            return refT.ReferencedType switch
            {
                NamedType n => n.Name,
                PrimitiveType p => p.TypeName,
                OptionType { InnerType: NamedType n } => n.Name,
                OptionType { InnerType: PrimitiveType p } => p.TypeName,
                _ => refT.ReferencedType.TypeName
            };

        return type.TypeName;
    }

    #endregion

    #region 工具方法

    private static string NullableSuffix(FieldDefinition field)
    {
        return field.IsOptional && field.FieldType is not OptionType ? "?" : "";
    }

    private static string ConvertGetterBody(string hermesBody)
    {
        var parts = hermesBody.Split('.');
        var csParts = new List<string>();
        for (var i = 0; i < parts.Length; i++)
            if (i == 0 && parts[i] == "self")
                csParts.Add("this");
            else
                csParts.Add(ToPascalCase(parts[i]));

        return string.Join(".", csParts);
    }

    private static string GetNamespace(IReadOnlyDictionary<string, object> options)
    {
        var ns = options.TryGetValue("namespace", out var nsVal) ? nsVal.ToString() ?? "Models" : "Models";
        return NamespaceToPascalCase(ns);
    }

    /// <summary>
    ///     将点分隔的 snake_case 命名空间转换为 C# PascalCase 命名空间
    /// </summary>
    private static string NamespaceToPascalCase(string ns)
    {
        if (string.IsNullOrEmpty(ns)) return ns;

        var segments = ns.Split('.');
        var result = new string[segments.Length];
        for (var i = 0; i < segments.Length; i++) result[i] = ToPascalCase(segments[i]);

        return string.Join(".", result);
    }

    private static string GetTarget(IReadOnlyDictionary<string, object> options)
    {
        return options.TryGetValue("targets", out var targetVal) ? targetVal.ToString() ?? "record" : "record";
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var parts = name.Split('_');
        var sb = new StringBuilder(name.Length);
        foreach (var part in parts)
            if (part.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(part[0]));
                sb.Append(part.AsSpan(1));
            }

        return sb.ToString();
    }

    private static string ToCamelCase(string name)
    {
        var pascal = ToPascalCase(name);
        if (string.IsNullOrEmpty(pascal)) return pascal;

        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private string GenerateFileHeader(string schemaPath, string outputPath)
    {
        var relativePath = schemaPath;
        if (!string.IsNullOrEmpty(outputPath))
            try
            {
                relativePath = Path.GetRelativePath(outputPath, schemaPath);
            }
            catch
            {
            }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// 本文件由 atlas/hermes 命令自动生成，修改无效");
        sb.AppendLine($"// 源文件: {relativePath}");
        sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        return sb.ToString();
    }

    #endregion
}