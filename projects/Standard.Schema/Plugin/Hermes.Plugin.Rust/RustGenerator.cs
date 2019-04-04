using System.Text;
using Hermes.Generator;

namespace Hermes.Plugin.Rust;

public sealed class RustGenerator : IGenerator
{
    public string Name => "rust";
    public string[] SupportedTargets => ["struct", "serde"];

    public GeneratorResult Generate(GeneratorContext context)
    {
        var result = new GeneratorResult();
        var crateName = context.Options.TryGetValue("crate", out var crateVal)
            ? crateVal.ToString() ?? "hermes_models"
            : "hermes_models";
        var target = context.Options.TryGetValue("targets", out var targetVal)
            ? targetVal.ToString() ?? "struct"
            : "struct";
        var useSerde = target == "serde";

        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        sb.AppendLine("#![allow(unused)]");
        sb.AppendLine();

        if (useSerde)
        {
            sb.AppendLine("use serde::{Serialize, Deserialize};");
            sb.AppendLine();
        }

        foreach (var enumDef in context.Schema.Enums) GenerateEnumToBuilder(sb, enumDef, useSerde);

        foreach (var flagsDef in context.Schema.Flags) GenerateFlagsToBuilder(sb, flagsDef, useSerde);

        foreach (var classDef in context.Schema.Classes) GenerateStructToBuilder(sb, classDef, useSerde);

        foreach (var storage in context.Schema.Storages)
        foreach (var model in storage.Models)
            GenerateModelStructToBuilder(sb, model, useSerde);

        foreach (var unionDef in context.Schema.Unions) GenerateUnionToBuilder(sb, unionDef, useSerde);

        result.Files.Add(new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, $"{ToSnakeCase(crateName)}.rs"),
            Content = sb.ToString(),
            Generator = Name
        });

        return result;
    }

    private void GenerateStructToBuilder(StringBuilder sb, ClassDefinition classDef, bool useSerde)
    {
        if (useSerde)
            sb.AppendLine("#[derive(Debug, Clone, Serialize, Deserialize)]");
        else
            sb.AppendLine("#[derive(Debug, Clone)]");

        sb.AppendLine($"pub struct {ToPascalCase(classDef.Name)} {{");

        foreach (var field in classDef.fields)
        {
            var rustType = MapType(field.FieldType);
            var fieldName = ToSnakeCase(field.Name);
            var isOptional = field.IsOptional;

            if (useSerde)
            {
                var rename = fieldName != field.Name ? $", rename = \"{field.Name}\"" : "";
                var defaultAttr = isOptional ? ", default" : "";
                sb.AppendLine($"    #[serde(rename = \"{field.Name}\"{defaultAttr})]");
            }

            if (isOptional)
                sb.AppendLine($"    pub {fieldName}: Option<{rustType}>,");
            else
                sb.AppendLine($"    pub {fieldName}: {rustType},");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateModelStructToBuilder(StringBuilder sb, ModelDefinition modelDef, bool useSerde)
    {
        if (modelDef.DocComment is not null) sb.AppendLine($"/// {modelDef.DocComment}");

        if (useSerde)
            sb.AppendLine("#[derive(Debug, Clone, Serialize, Deserialize)]");
        else
            sb.AppendLine("#[derive(Debug, Clone)]");

        sb.AppendLine($"pub struct {ToPascalCase(modelDef.Name)} {{");

        foreach (var field in modelDef.fields)
        {
            var rustType = MapType(field.FieldType);
            var fieldName = ToSnakeCase(field.Name);
            var isOptional = field.IsOptional;

            if (field.DocComment is not null) sb.AppendLine($"    /// {field.DocComment}");

            if (useSerde)
            {
                var defaultAttr = isOptional ? ", default" : "";
                sb.AppendLine($"    #[serde(rename = \"{field.Name}\"{defaultAttr})]");
            }

            if (isOptional)
                sb.AppendLine($"    pub {fieldName}: Option<{rustType}>,");
            else
                sb.AppendLine($"    pub {fieldName}: {rustType},");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        if (modelDef.Getters.Count > 0)
        {
            sb.AppendLine($"impl {ToPascalCase(modelDef.Name)} {{");

            foreach (var getter in modelDef.Getters)
            {
                var rustReturnType = getter.ReturnType != null ? MapType(getter.ReturnType) : "Box<dyn std::any::Any>";
                var rustBody = ConvertGetterBody(getter.Body);

                if (getter.DocComment is not null) sb.AppendLine($"    /// {getter.DocComment}");

                sb.AppendLine($"    pub fn {ToSnakeCase(getter.Name)}(&self) -> {rustReturnType} {{");
                sb.AppendLine($"        {rustBody}");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }
    }

    private void GenerateEnumToBuilder(StringBuilder sb, EnumDefinition enumDef, bool useSerde)
    {
        if (useSerde)
        {
            sb.AppendLine("#[derive(Debug, Clone, Copy, Serialize, Deserialize)]");
            sb.AppendLine("#[serde(rename_all = \"SCREAMING_SNAKE_CASE\")]");
        }
        else
        {
            sb.AppendLine("#[derive(Debug, Clone, Copy)]");
        }

        sb.AppendLine($"pub enum {ToPascalCase(enumDef.Name)} {{");

        foreach (var member in enumDef.Members) sb.AppendLine($"    {ToScreamingSnakeCase(member.Name)},");

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateFlagsToBuilder(StringBuilder sb, FlagsDefinition flagsDef, bool useSerde)
    {
        sb.AppendLine($"pub struct {ToPascalCase(flagsDef.Name)} (pub u64);");
        sb.AppendLine();

        sb.AppendLine($"impl {ToPascalCase(flagsDef.Name)} {{");

        foreach (var member in flagsDef.Members)
            sb.AppendLine(
                $"    pub const {ToScreamingSnakeCase(member.Name)}: {ToPascalCase(flagsDef.Name)} = {ToPascalCase(flagsDef.Name)}(1 << {member.Value});");

        sb.AppendLine();
        sb.AppendLine($"    pub fn has_flag(&self, flag: {ToPascalCase(flagsDef.Name)}) -> bool {{");
        sb.AppendLine("        (self.0 & flag.0) != 0");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateUnionToBuilder(StringBuilder sb, UnionDefinition unionDef, bool useSerde)
    {
        if (useSerde)
        {
            sb.AppendLine("#[derive(Debug, Clone, Serialize, Deserialize)]");
            sb.AppendLine("#[serde(tag = \"kind\", content = \"value\")]");
        }
        else
        {
            sb.AppendLine("#[derive(Debug, Clone)]");
        }

        sb.AppendLine($"pub enum {ToPascalCase(unionDef.Name)} {{");

        foreach (var variant in unionDef.Variants)
            if (variant.Payload != null)
                sb.AppendLine($"    {ToPascalCase(variant.Name)}({MapType(variant.Payload)}),");
            else
                sb.AppendLine($"    {ToPascalCase(variant.Name)},");

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private string MapType(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" => "i8",
                "i16" => "i16",
                "i32" => "i32",
                "i64" => "i64",
                "u8" => "u8",
                "u16" => "u16",
                "u32" => "u32",
                "u64" => "u64",
                "f32" => "f32",
                "f64" => "f64",
                "bool" => "bool",
                "utf8" => "String",
                "utf16" => "String",
                "uuid" => "uuid::Uuid",
                "unit" => "()",
                "object" => "serde_json::Value"
            },
            ListType l => $"Vec<{MapType(l.ElementType)}>",
            ArrayType a => $"[{MapType(a.ElementType)}; {a.Size}]",
            OptionType o => $"Option<{MapType(o.InnerType)}>",
            DictType d => $"std::collections::HashMap<{MapType(d.KeyType)}, {MapType(d.ValueType)}>",
            RecordType r => $"std::collections::HashMap<{MapType(r.KeyType)}, {MapType(r.ValueType)}>",
            ResultType r => r.ErrorType != null
                ? $"Result<{MapType(r.OkType)}, {MapType(r.ErrorType)}>"
                : $"Result<{MapType(r.OkType)}, Box<dyn std::error::Error>>",
            StreamType s => $"impl Stream<Item = {MapType(s.InnerType)}>",
            ReferenceType refT => MapType(refT.ReferencedType),
            NamedType n => ToPascalCase(n.Name),
            _ => "Box<dyn std::any::Any>"
        };
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var parts = name.Split('_');
        return string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
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

    private static string ToScreamingSnakeCase(string name)
    {
        return ToSnakeCase(name).ToUpperInvariant();
    }

    private static string ConvertGetterBody(string hermesBody)
    {
        var parts = hermesBody.Split('.');
        var rustParts = new List<string>();
        for (var i = 0; i < parts.Length; i++)
            if (i == 0 && parts[i] == "self")
                rustParts.Add("self");
            else
                rustParts.Add(ToSnakeCase(parts[i]));

        return string.Join(".", rustParts);
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