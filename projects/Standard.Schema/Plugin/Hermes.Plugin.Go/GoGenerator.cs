using System.Text;
using Hermes.Generator;

namespace Hermes.Plugin.Go;

public sealed class GoGenerator : IGenerator
{
    public string Name => "go";
    public string[] SupportedTargets => ["struct", "gin"];

    public GeneratorResult Generate(GeneratorContext context)
    {
        var result = new GeneratorResult();
        var goPackage = context.Options.TryGetValue("package", out var pkgVal)
            ? pkgVal.ToString() ?? "models"
            : "models";
        var target = context.Options.TryGetValue("targets", out var targetVal)
            ? targetVal.ToString() ?? "struct"
            : "struct";

        var allFiles = new StringBuilder();
        allFiles.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        allFiles.AppendLine($"package {goPackage}");
        allFiles.AppendLine();

        var imports = new HashSet<string>();
        foreach (var classDef in context.Schema.Classes) CollectImports(classDef, imports);

        foreach (var unionDef in context.Schema.Unions) CollectUnionImports(unionDef, imports);

        if (imports.Count > 0)
        {
            allFiles.AppendLine("import (");
            foreach (var imp in imports.OrderBy(i => i)) allFiles.AppendLine($"    \"{imp}\"");
            allFiles.AppendLine(")");
            allFiles.AppendLine();
        }

        foreach (var enumDef in context.Schema.Enums) GenerateEnumToBuilder(allFiles, enumDef);

        foreach (var flagsDef in context.Schema.Flags) GenerateFlagsToBuilder(allFiles, flagsDef);

        foreach (var classDef in context.Schema.Classes) GenerateStructToBuilder(allFiles, classDef);

        foreach (var storage in context.Schema.Storages)
        foreach (var model in storage.Models)
            GenerateModelStructToBuilder(allFiles, model);

        foreach (var unionDef in context.Schema.Unions) GenerateUnionToBuilder(allFiles, unionDef);

        if (target == "gin")
            foreach (var serviceDef in context.Schema.Services)
                GenerateGinHandlersToBuilder(allFiles, serviceDef, goPackage);

        result.Files.Add(new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, $"{goPackage}.go"),
            Content = allFiles.ToString(),
            Generator = Name
        });

        return result;
    }

    private void GenerateStructToBuilder(StringBuilder sb, ClassDefinition classDef)
    {
        sb.AppendLine($"type {classDef.Name} struct {{");

        foreach (var field in classDef.fields)
        {
            var goType = MapType(field.FieldType);
            var jsonTag = field.IsOptional ? ",omitempty" : "";
            sb.AppendLine($"    {ToPascalCase(field.Name)} {goType} `json:\"{ToSnakeCase(field.Name)}{jsonTag}\"`");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateModelStructToBuilder(StringBuilder sb, ModelDefinition modelDef)
    {
        if (modelDef.DocComment is not null) sb.AppendLine($"// {modelDef.DocComment}");

        sb.AppendLine($"type {modelDef.Name} struct {{");

        foreach (var field in modelDef.fields)
        {
            var goType = MapType(field.FieldType);
            var jsonTag = field.IsOptional ? ",omitempty" : "";

            if (field.DocComment is not null) sb.AppendLine($"    // {field.DocComment}");

            sb.AppendLine($"    {ToPascalCase(field.Name)} {goType} `json:\"{ToSnakeCase(field.Name)}{jsonTag}\"`");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        foreach (var getter in modelDef.Getters)
        {
            var goReturnType = getter.ReturnType != null ? MapType(getter.ReturnType) : "interface{}";
            var goBody = ConvertGetterBody(getter.Body, modelDef.Name);

            if (getter.DocComment is not null) sb.AppendLine($"// {getter.DocComment}");

            sb.AppendLine($"func (m *{modelDef.Name}) {ToPascalCase(getter.Name)}() {goReturnType} {{");
            sb.AppendLine($"    return {goBody}");
            sb.AppendLine("}");
            sb.AppendLine();
        }
    }

    private void GenerateEnumToBuilder(StringBuilder sb, EnumDefinition enumDef)
    {
        sb.AppendLine($"type {enumDef.Name} int");
        sb.AppendLine();
        sb.AppendLine("const (");

        var members = enumDef.Members.ToList();
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            if (i == 0)
                sb.AppendLine($"    {member.Name} {enumDef.Name} = iota");
            else
                sb.AppendLine($"    {member.Name}");
        }

        sb.AppendLine(")");
        sb.AppendLine();

        sb.AppendLine($"func (e {enumDef.Name}) String() string {{");
        sb.AppendLine("    switch e {");
        foreach (var member in members)
        {
            sb.AppendLine($"    case {member.Name}:");
            sb.AppendLine($"        return \"{member.Name}\"");
        }

        sb.AppendLine("    default:");
        sb.AppendLine("        return \"unknown\"");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateFlagsToBuilder(StringBuilder sb, FlagsDefinition flagsDef)
    {
        sb.AppendLine($"type {flagsDef.Name} int");
        sb.AppendLine();
        sb.AppendLine("const (");

        var members = flagsDef.Members.ToList();
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            if (i == 0)
                sb.AppendLine($"    {member.Name} {flagsDef.Name} = 1 << {member.Value}");
            else
                sb.AppendLine($"    {member.Name} = 1 << {member.Value}");
        }

        sb.AppendLine(")");
        sb.AppendLine();

        sb.AppendLine($"func (f {flagsDef.Name}) HasFlag(flag {flagsDef.Name}) bool {{");
        sb.AppendLine("    return f&flag != 0");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateUnionToBuilder(StringBuilder sb, UnionDefinition unionDef)
    {
        sb.AppendLine($"type {unionDef.Name} interface {{");
        sb.AppendLine($"    is{unionDef.Name}()");
        sb.AppendLine("}");
        sb.AppendLine();

        foreach (var variant in unionDef.Variants)
            if (variant.Payload != null)
            {
                sb.AppendLine($"type {unionDef.Name}{variant.Name} struct {{");
                sb.AppendLine($"    Value {MapType(variant.Payload)}");
                sb.AppendLine("}");
                sb.AppendLine();
                sb.AppendLine($"func ({unionDef.Name}{variant.Name}) is{unionDef.Name}() {{}}");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"type {unionDef.Name}{variant.Name} struct {{}}");
                sb.AppendLine();
                sb.AppendLine($"func ({unionDef.Name}{variant.Name}) is{unionDef.Name}() {{}}");
                sb.AppendLine();
            }
    }

    private void GenerateGinHandlersToBuilder(StringBuilder sb,
        Nyar.Dialect.Schema.IR.Where.ServiceDefinition serviceDef, string goPackage)
    {
        sb.AppendLine($"func Register{serviceDef.Name}Routes(r *gin.Engine, svc *{serviceDef.Name}Service) {{");

        foreach (var endpoint in serviceDef.Endpoints)
            if (endpoint is Nyar.Dialect.Schema.IR.Where.HttpEndpoint http)
            {
                var method = http.HttpMethod.ToUpperInvariant();
                var path = http.Path ?? $"/{ToSnakeCase(http.Name)}";
                sb.AppendLine($"    r.{GoHttpMethod(method)}(\"{path}\", svc.{ToPascalCase(http.Name)})");
            }

        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"type {serviceDef.Name}Service struct {{");
        sb.AppendLine("}");
        sb.AppendLine();

        foreach (var endpoint in serviceDef.Endpoints)
            if (endpoint is Nyar.Dialect.Schema.IR.Where.HttpEndpoint http)
            {
                sb.AppendLine($"func (s *{serviceDef.Name}Service) {ToPascalCase(http.Name)}(c *gin.Context) {{");
                sb.AppendLine("    // TODO: 实现业务逻辑");
                sb.AppendLine("    c.JSON(200, gin.H{})");
                sb.AppendLine("}");
                sb.AppendLine();
            }
    }

    private void CollectImports(ClassDefinition classDef, HashSet<string> imports)
    {
        foreach (var field in classDef.fields) CollectTypeImports(field.FieldType, imports);
    }

    private void CollectUnionImports(UnionDefinition unionDef, HashSet<string> imports)
    {
        foreach (var variant in unionDef.Variants)
            if (variant.Payload != null)
                CollectTypeImports(variant.Payload, imports);
    }

    private void CollectTypeImports(SchemaType type, HashSet<string> imports)
    {
        switch (type)
        {
            case ListType l:
                imports.Add("fmt");
                CollectTypeImports(l.ElementType, imports);
                break;
            case DictType d:
                CollectTypeImports(d.KeyType, imports);
                CollectTypeImports(d.ValueType, imports);
                break;
            case OptionType o:
                CollectTypeImports(o.InnerType, imports);
                break;
            case StreamType:
                imports.Add("fmt");
                break;
        }
    }

    private string MapType(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" => "int8",
                "i16" => "int16",
                "i32" => "int32",
                "i64" => "int64",
                "u8" => "uint8",
                "u16" => "uint16",
                "u32" => "uint32",
                "u64" => "uint64",
                "f32" => "float32",
                "f64" => "float64",
                "bool" => "bool",
                "utf8" => "string",
                "utf16" => "string",
                "uuid" => "[16]byte",
                "unit" => "struct{}",
                "object" => "interface{}"
            },
            ListType l => $"[]{MapType(l.ElementType)}",
            ArrayType a => $"[{a.Size}]{MapType(a.ElementType)}",
            OptionType o => $"*{MapType(o.InnerType)}",
            DictType d => $"map[{MapType(d.KeyType)}]{MapType(d.ValueType)}",
            RecordType r => $"map[{MapType(r.KeyType)}]{MapType(r.ValueType)}",
            ResultType r => r.ErrorType != null
                ? $"({MapType(r.OkType)}, error)"
                : $"({MapType(r.OkType)}, error)",
            StreamType s => $"<-chan {MapType(s.InnerType)}",
            ReferenceType refT => MapType(refT.ReferencedType),
            NamedType n => n.Name,
            _ => "interface{}"
        };
    }

    private static string GoHttpMethod(string method)
    {
        return method switch
        {
            "GET" => "GET",
            "POST" => "POST",
            "PUT" => "PUT",
            "DELETE" => "DELETE",
            "PATCH" => "PATCH",
            _ => method
        };
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    private static string ConvertGetterBody(string hermesBody, string receiverVar)
    {
        var parts = hermesBody.Split('.');
        var goParts = new List<string>();
        for (var i = 0; i < parts.Length; i++)
            if (i == 0 && parts[i] == "self")
                goParts.Add("m");
            else
                goParts.Add(ToPascalCase(parts[i]));

        return string.Join(".", goParts);
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
        sb.AppendLine();
        return sb.ToString();
    }
}