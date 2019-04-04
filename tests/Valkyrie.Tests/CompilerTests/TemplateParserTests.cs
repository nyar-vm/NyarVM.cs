using Nyar.Language.Valkyrie.Compiler;

namespace Valkyrie.Tests.CompilerTests;

public sealed class TemplateParserTests
{
    [Fact]
    public void ParseSource_WithMatchTemplateInFunctionBody_ShouldProduceMatchTemplateNode()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
                     micro is_match() -> bool {
                         <% match arch %>
                             <% case "clr" %>
                             return true
                             <% else %>
                             return false
                         <% end match %>
                     }
                     """;

        var parseResult = compiler.parse_source(source, "template_match.v");
        var diagnostics = string.Join(
            Environment.NewLine,
            compiler.diagnostics.messages.Select(message => message.message));

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors, diagnostics);

        var function = Assert.Single(parseResult.value!.Declarations);
        var method = Assert.IsType<DeclareMicro>(function);
        Assert.NotNull(method.Body);
        Assert.Contains(method.Body!.Statements, statement => statement is MatchTemplate);
    }

    [Fact]
    public void ParseSource_WithMatchTemplateAndQualifiedCalls_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
                     structure Regex {
                         pattern: utf8
                     }

                     micro is_match(re: Regex, text: utf8) -> bool {
                         <% match arch %>
                             <% case "clr" %>
                             return std.adaptor.dotnet.regex.is_match(re.pattern, text)
                             <% case "jvm" %>
                             return std.adaptor.jvm.regex.is_match(re.pattern, text)
                             <% case "wasm32" %>
                             return std.adaptor.wasm.regex.is_match(re.pattern, text)
                             <% else %>
                             return false
                         <% end match %>
                     }
                     """;

        var parseResult = compiler.parse_source(source, "template_match_qualified.v");
        var diagnostics = string.Join(
            Environment.NewLine,
            compiler.diagnostics.messages.Select(message => message.message));

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors, diagnostics);
    }

    [Fact]
    public void ParseSource_WithTemplateArmContainingEmptyStringCalls_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
                     micro read_all() -> utf8 {
                         <% match arch %>
                             <% case "clr" %>
                             return std.adaptor.dotnet.io.file_read_all_text("")
                             <% else %>
                             return ""
                         <% end match %>
                     }

                     micro read_char() -> i32 {
                         <% match arch %>
                             <% case "clr" %>
                             return std.adaptor.dotnet.console.console_read_key()
                             <% case "jvm" %>
                             return std.adaptor.jvm.console.jvm_read()
                             <% else %>
                             return 0
                         <% end match %>
                     }
                     """;

        var parseResult = compiler.parse_source(source, "template_match_strings.v");
        var diagnostics = string.Join(
            Environment.NewLine,
            compiler.diagnostics.messages.Select(message => message.message));

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors, diagnostics);
    }

    [Fact]
    public void ParseSource_WithTemplateAndOptionOfMatchReturnType_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
                     structure Match {
                         index: i32
                     }

                     micro find() -> Option<Match> {
                         <% match arch %>
                             <% case "clr" %>
                             return None
                             <% else %>
                             return None
                         <% end match %>
                     }
                     """;

        var parseResult = compiler.parse_source(source, "template_match_option_type.v");
        var diagnostics = string.Join(
            Environment.NewLine,
            compiler.diagnostics.messages.Select(message => message.message));

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors, diagnostics);
    }

    [Fact]
    public void ParseSource_WithTemplateAndListOfMatchReturnType_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
                     structure Match {
                         index: i32
                     }

                     micro find_all() -> List<Match> {
                         <% match arch %>
                             <% case "clr" %>
                             return List.new()
                             <% else %>
                             return List.new()
                         <% end match %>
                     }
                     """;

        var parseResult = compiler.parse_source(source, "template_match_list_type.v");
        var diagnostics = string.Join(
            Environment.NewLine,
            compiler.diagnostics.messages.Select(message => message.message));

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors, diagnostics);
    }

    [Fact]
    public void ParseSource_WithRealTextAsciiStdFile_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var path = @"e:\RiderProjects\Valkyrie.cs\examples\std\source\text\AsciiText.v";
        var source = File.ReadAllText(path);

        var parseResult = compiler.parse_source(source, "std_text_ascii.v");
        var diagnostics = string.Join(
            Environment.NewLine,
            compiler.diagnostics.messages.Select(message => message.message));

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors, diagnostics);
    }

    [Fact]
    public void ParseSource_WithTemplateAndGenericReturnType_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var source = """
                     micro split(s: utf8, sep: utf8): List<utf8> {
                         <% match arch %>
                             <% case "clr" %>
                             return std.adaptor.dotnet.string.string_split(s, sep)
                             <% else %>
                             return List.new()
                         <% end match %>
                     }
                     """;

        var parseResult = compiler.parse_source(source, "template_generic_ret.v");
        var diagnostics = string.Join(
            Environment.NewLine,
            compiler.diagnostics.messages.Select(message => message.message));

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors, diagnostics);
    }

    [Fact]
    public void ParseSource_WithRealIoReadStdFile_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var path = @"e:\RiderProjects\Valkyrie.cs\examples\std\source\io\read.v";
        var source = File.ReadAllText(path);

        var parseResult = compiler.parse_source(source, "read.v");
        var diagnostics = string.Join(
            Environment.NewLine,
            compiler.diagnostics.messages.Select(message => message.message));

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors, diagnostics);
    }
}
