using System.Text.RegularExpressions;

namespace Std.Data.Text.Vue;


/// <summary>

///     Vue 单文件组件解析器，支持 .vue 文件的 template + script + style 解析

/// 。</summary>
public sealed partial class VueSfcParser
{
    private readonly DiagnosticSink _diagnostics;

    public VueSfcParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    
/// <summary>
    
///     解析 Vue SFC 源码
    
/// 。</summary>
    public VueSfcParseResult Parse(string source, string filePath = "")
    {
        var scriptBlocks = ExtractAllBlocks(source, "script");
        var templateBlocks = ExtractAllBlocks(source, "template");
        var styleBlocks = ExtractAllBlocks(source, "style");

        var script = scriptBlocks.Count > 0 ? ParseScriptBlock(scriptBlocks[0]) : null;
        var template = templateBlocks.Count > 0 ? ParseTemplateBlock(templateBlocks[0].Content) : null;
        var styles = styleBlocks.Select(ParseStyleBlock).ToList();

        var componentName = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrEmpty(componentName))
        {
            componentName = "AnonymousComponent";
        }

        return new VueSfcParseResult
        {
            Name = componentName,
            Script = script,
            Template = template,
            Styles = styles
        };
    }

    #region Block Extraction

    private static List<SfcBlock> ExtractAllBlocks(string source, string tagName)
    {
        var blocks = new List<SfcBlock>();
        var pattern = $@"<(?<tag>{tagName})(?<attrs>\s[^>]*)?>(?<content>[\s\S]*?)</\k<tag>>";
        var matches = Regex.Matches(source, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var attrs = match.Groups["attrs"].Success ? match.Groups["attrs"].Value.Trim() : "";
            var content = match.Groups["content"].Value;

            blocks.Add(new SfcBlock(content, attrs));
        }

        return blocks;
    }

    private readonly record struct SfcBlock(string Content, string Attributes);

    #endregion

    #region Script Parsing

    private VueScriptBlock ParseScriptBlock(SfcBlock block)
    {
        var isSetup = BlockHasAttr(block.Attributes, "setup");
        var lang = ExtractAttrValue(block.Attributes, "lang") ?? "js";

        var props = ExtractDefineProps(block.Content);
        var emits = ExtractDefineEmits(block.Content);
        var reactiveVars = ExtractReactiveVars(block.Content);
        var exposedMembers = ExtractDefineExpose(block.Content);

        return new VueScriptBlock
        {
            Content = block.Content.Trim(),
            IsSetup = isSetup,
            Lang = lang,
            Props = props,
            Emits = emits,
            ReactiveVars = reactiveVars,
            ExposedMembers = exposedMembers
        };
    }

    private static bool BlockHasAttr(string attributes, string attrName)
    {
        var pattern = $@"\b{attrName}\b";
        return Regex.IsMatch(attributes, pattern, RegexOptions.IgnoreCase);
    }

    private static string? ExtractAttrValue(string attributes, string attrName)
    {
        var pattern = $@"\b{attrName}\s*=\s*[""']([^""']*)[""']";
        var match = Regex.Match(attributes, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private List<VuePropDef> ExtractDefineProps(string scriptContent)
    {
        var props = new List<VuePropDef>();

        var objectPattern = @"defineProps\s*\(\s*\{([\s\S]*?)\}\s*\)";
        var objMatch = Regex.Match(scriptContent, objectPattern);
        if (objMatch.Success)
        {
            var propsBody = objMatch.Groups[1].Value;
            var propPattern = @"(\w+)\s*:\s*\{([^}]*)\}";
            foreach (Match propMatch in Regex.Matches(propsBody, propPattern))
            {
                var name = propMatch.Groups[1].Value;
                var propDef = propMatch.Groups[2].Value;

                var typeMatch = Regex.Match(propDef, @"type\s*:\s*(\w+)");
                var requiredMatch = Regex.Match(propDef, @"required\s*:\s*(true|false)");
                var defaultMatch = Regex.Match(propDef, @"default\s*:\s*(.+?)(?:,\s*$|\s*$)", RegexOptions.Multiline);

                props.Add(new VuePropDef
                {
                    Name = name,
                    TypeName = typeMatch.Success ? typeMatch.Groups[1].Value : "any",
                    Required = requiredMatch.Success && requiredMatch.Groups[1].Value == "true",
                    DefaultValue = defaultMatch.Success ? defaultMatch.Groups[1].Value.Trim() : null
                });
            }
        }

        var arrayPattern = @"defineProps\s*\(\s*\[([\s\S]*?)\]\s*\)";
        var arrMatch = Regex.Match(scriptContent, arrayPattern);
        if (arrMatch.Success)
        {
            var items = arrMatch.Groups[1].Value;
            foreach (Match itemMatch in Regex.Matches(items, @"['""](\w+)['""]"))
            {
                props.Add(new VuePropDef
                {
                    Name = itemMatch.Groups[1].Value,
                    TypeName = "any"
                });
            }
        }

        var typePattern = @"defineProps\s*<\s*{([^}]*)}\s*>";
        var typeMatch2 = Regex.Match(scriptContent, typePattern);
        if (typeMatch2.Success)
        {
            var typeBody = typeMatch2.Groups[1].Value;
            var fieldPattern = @"(\w+)(\??)\s*:\s*(\w+)";
            foreach (Match fieldMatch in Regex.Matches(typeBody, fieldPattern))
            {
                props.Add(new VuePropDef
                {
                    Name = fieldMatch.Groups[1].Value,
                    TypeName = fieldMatch.Groups[3].Value,
                    Required = fieldMatch.Groups[2].Value != "?"
                });
            }
        }

        return props;
    }

    private List<string> ExtractDefineEmits(string scriptContent)
    {
        var emits = new List<string>();

        var arrayPattern = @"defineEmits\s*\(\s*\[([\s\S]*?)\]\s*\)";
        var arrMatch = Regex.Match(scriptContent, arrayPattern);
        if (arrMatch.Success)
        {
            var items = arrMatch.Groups[1].Value;
            foreach (Match itemMatch in Regex.Matches(items, @"['""](\w+)['""]"))
            {
                emits.Add(itemMatch.Groups[1].Value);
            }
        }

        var typePattern = @"defineEmits\s*<\s*{([^}]*)}\s*>()";
        var typeMatch = Regex.Match(scriptContent, typePattern);
        if (typeMatch.Success)
        {
            var typeBody = typeMatch.Groups[1].Value;
            foreach (Match eventMatch in Regex.Matches(typeBody, @"(\w+)\s*[:(]"))
            {
                emits.Add(eventMatch.Groups[1].Value);
            }
        }

        return emits;
    }

    private List<VueReactiveVar> ExtractReactiveVars(string scriptContent)
    {
        var vars = new List<VueReactiveVar>();

        var refPattern = @"(?:const|let)\s+(\w+)\s*=\s*(?:ref|computed)\s*\(([^)]*)\)";
        foreach (Match match in Regex.Matches(scriptContent, refPattern))
        {
            var kind = match.Value.Contains("computed") ? VueReactiveKind.Computed : VueReactiveKind.Ref;
            vars.Add(new VueReactiveVar
            {
                Name = match.Groups[1].Value,
                Kind = kind,
                Initializer = match.Groups[2].Value.Trim()
            });
        }

        var reactivePattern = @"(?:const|let)\s+(\w+)\s*=\s*reactive\s*\(([^)]*)\)";
        foreach (Match match in Regex.Matches(scriptContent, reactivePattern))
        {
            vars.Add(new VueReactiveVar
            {
                Name = match.Groups[1].Value,
                Kind = VueReactiveKind.Reactive,
                Initializer = match.Groups[2].Value.Trim()
            });
        }

        return vars;
    }

    private List<string> ExtractDefineExpose(string scriptContent)
    {
        var members = new List<string>();

        var pattern = @"defineExpose\s*\(\s*\{([^}]*)\}";
        var match = Regex.Match(scriptContent, pattern);
        if (match.Success)
        {
            var body = match.Groups[1].Value;
            foreach (Match memberMatch in Regex.Matches(body, @"(\w+)"))
            {
                members.Add(memberMatch.Groups[1].Value);
            }
        }

        return members;
    }

    #endregion

    #region Template Parsing

    private VueTemplateBlock ParseTemplateBlock(string templateContent)
    {
        var nodes = new List<VueTemplateNode>();

        if (string.IsNullOrWhiteSpace(templateContent))
        {
            return new VueTemplateBlock { Children = nodes };
        }

        ParseTemplateNodes(templateContent.Trim(), nodes);
        return new VueTemplateBlock { Children = nodes };
    }

    private void ParseTemplateNodes(string template, List<VueTemplateNode> nodes)
    {
        var pos = 0;

        while (pos < template.Length)
        {
            var textEnd = template.IndexOf('<', pos);

            if (textEnd < 0)
            {
                AddTextNode(template[pos..], nodes);
                break;
            }

            if (textEnd > pos)
            {
                AddTextNode(template[pos..textEnd], nodes);
            }

            var tagEnd = template.IndexOf('>', textEnd);
            if (tagEnd < 0)
            {
                break;
            }

            var tagContent = template[(textEnd + 1)..tagEnd].Trim();
            pos = tagEnd + 1;

            if (tagContent.StartsWith("!--"))
            {
                var commentEnd = template.IndexOf("-->", pos);
                pos = commentEnd >= 0 ? commentEnd + 3 : template.Length;
                continue;
            }

            if (tagContent.StartsWith("/"))
            {
                continue;
            }

            var isSelfClosing = tagContent.EndsWith("/");
            if (isSelfClosing)
            {
                tagContent = tagContent[..^1].Trim();
            }

            var (tagName, attributes) = ParseTagWithVueAttrs(tagContent);

            if (TryExtractDirective(tagName, attributes, out var directiveNode))
            {
                if (!isSelfClosing)
                {
                    var (children, endPos) = ParseBlockContent(template, pos, tagName);
                    pos = endPos;
                    directiveNode = new VueDirectiveNode
                    {
                        DirectiveName = directiveNode.DirectiveName,
                        Argument = directiveNode.Argument,
                        Modifiers = directiveNode.Modifiers,
                        Expression = directiveNode.Expression,
                        Children = children
                    };
                }

                nodes.Add(directiveNode);
            }
            else
            {
                if (!isSelfClosing)
                {
                    var (children, endPos) = ParseBlockContent(template, pos, tagName);
                    pos = endPos;
                    nodes.Add(new VueElementNode
                    {
                        TagName = tagName,
                        Attributes = attributes,
                        Children = children
                    });
                }
                else
                {
                    nodes.Add(new VueElementNode
                    {
                        TagName = tagName,
                        Attributes = attributes,
                        IsSelfClosing = true
                    });
                }
            }
        }
    }

    private void AddTextNode(string text, List<VueTemplateNode> nodes)
    {
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return;
        }

        var hasInterpolation = InterpolationRegex().IsMatch(trimmed);

        nodes.Add(new VueTextNode
        {
            Text = trimmed,
            HasInterpolation = hasInterpolation
        });
    }

    private bool TryExtractDirective(
        string tagName,
        List<VueAttribute> attributes,
        out VueDirectiveNode directiveNode)
    {
        directiveNode = default!;

        foreach (var attr in attributes)
        {
            if (attr.Kind != VueAttributeKind.Directive)
            {
                continue;
            }

            var directiveName = attr.Name switch
            {
                "v-if" => "if",
                "v-else-if" => "else-if",
                "v-else" => "else",
                "v-for" => "for",
                "v-show" => "show",
                "v-html" => "html",
                "v-text" => "text",
                "v-once" => "once",
                "v-pre" => "pre",
                "v-cloak" => "cloak",
                "v-memo" => "memo",
                _ => null
            };

            if (directiveName is not null)
            {
                directiveNode = new VueDirectiveNode
                {
                    DirectiveName = directiveName,
                    Expression = attr.Value ?? ""
                };
                return true;
            }
        }

        return false;
    }

    private static (string TagName, List<VueAttribute> Attributes) ParseTagWithVueAttrs(string tagContent)
    {
        var parts = tagContent.Split(' ', 2);
        var tagName = parts[0];
        var attributes = new List<VueAttribute>();

        if (parts.Length > 1)
        {
            var attrString = parts[1];
            var attrMatches = VueAttributeRegex().Matches(attrString);

            foreach (Match match in attrMatches)
            {
                var rawKey = match.Groups[1].Value;
                var rawValue = match.Groups[2].Success ? match.Groups[2].Value : null;

                var (name, kind) = ClassifyAttribute(rawKey);

                attributes.Add(new VueAttribute
                {
                    Name = name,
                    Value = rawValue,
                    Kind = kind
                });
            }
        }

        return (tagName, attributes);
    }

    private static (string Name, VueAttributeKind Kind) ClassifyAttribute(string rawKey)
    {
        if (rawKey.StartsWith("v-model"))
        {
            var arg = ExtractDirectiveArg(rawKey, "v-model");
            return (arg, VueAttributeKind.Model);
        }

        if (rawKey.StartsWith("v-bind:") || rawKey.StartsWith(":"))
        {
            var arg = rawKey.StartsWith(":")
                ? rawKey[1..]
                : ExtractDirectiveArg(rawKey, "v-bind");
            return (arg, VueAttributeKind.Bind);
        }

        if (rawKey.StartsWith("v-on:") || rawKey.StartsWith("@"))
        {
            var arg = rawKey.StartsWith("@")
                ? rawKey[1..]
                : ExtractDirectiveArg(rawKey, "v-on");
            return (arg, VueAttributeKind.On);
        }

        if (rawKey.StartsWith("v-slot:") || rawKey.StartsWith("#"))
        {
            var arg = rawKey.StartsWith("#")
                ? rawKey[1..]
                : ExtractDirectiveArg(rawKey, "v-slot");
            return (arg, VueAttributeKind.Slot);
        }

        if (rawKey.StartsWith("v-"))
        {
            return (rawKey, VueAttributeKind.Directive);
        }

        return (rawKey, VueAttributeKind.Plain);
    }

    private static string ExtractDirectiveArg(string rawKey, string prefix)
    {
        var rest = rawKey[prefix.Length..];

        if (rest.StartsWith(":"))
        {
            var parts = rest[1..].Split('.');
            return parts[0];
        }

        if (rest.StartsWith("."))
        {
            return prefix;
        }

        return rawKey;
    }

    private (List<VueTemplateNode> Children, int EndPos) ParseBlockContent(string template, int startPos,
        string blockTag)
    {
        var children = new List<VueTemplateNode>();
        var pos = startPos;
        var depth = 1;
        var blockStart = startPos;

        while (pos < template.Length && depth > 0)
        {
            var nextOpen = template.IndexOf('<', pos);
            if (nextOpen < 0)
            {
                break;
            }

            var nextClose = template.IndexOf('>', nextOpen);
            if (nextClose < 0)
            {
                break;
            }

            var tag = template[(nextOpen + 1)..nextClose].Trim();
            var tagOnly = tag.Split(' ')[0];

            if (tagOnly == blockTag)
            {
                depth++;
            }
            else if (tagOnly == "/" + blockTag)
            {
                depth--;
                if (depth == 0)
                {
                    var blockContent = template[blockStart..nextOpen].Trim();
                    if (!string.IsNullOrEmpty(blockContent))
                    {
                        ParseTemplateNodes(blockContent, children);
                    }

                    return (children, nextClose + 1);
                }
            }

            pos = nextClose + 1;
        }

        return (children, pos);
    }

    #endregion

    #region Style Parsing

    private VueStyleBlock ParseStyleBlock(SfcBlock block)
    {
        var lang = ExtractAttrValue(block.Attributes, "lang") ?? "css";
        var scoped = BlockHasAttr(block.Attributes, "scoped");
        var module = BlockHasAttr(block.Attributes, "module");

        var rules = ParseCssRules(block.Content);

        return new VueStyleBlock
        {
            Content = block.Content.Trim(),
            Lang = lang,
            Scoped = scoped,
            Module = module,
            Rules = rules
        };
    }

    private static List<VueCssRule> ParseCssRules(string styleContent)
    {
        var rules = new List<VueCssRule>();

        if (string.IsNullOrWhiteSpace(styleContent))
        {
            return rules;
        }

        var rulePattern = @"([^{]+)\{([^}]*)\}";
        foreach (Match ruleMatch in Regex.Matches(styleContent, rulePattern))
        {
            var selector = ruleMatch.Groups[1].Value.Trim();
            var propsBody = ruleMatch.Groups[2].Value.Trim();

            var properties = new List<VueCssProperty>();
            foreach (var propLine in propsBody.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var colonIdx = propLine.IndexOf(':');
                if (colonIdx < 0)
                {
                    continue;
                }

                var propName = propLine[..colonIdx].Trim();
                var propValue = propLine[(colonIdx + 1)..].Trim();

                if (!string.IsNullOrEmpty(propName) && !string.IsNullOrEmpty(propValue))
                {
                    properties.Add(new VueCssProperty { Name = propName, Value = propValue });
                }
            }

            rules.Add(new VueCssRule { Selector = selector, Properties = properties });
        }

        return rules;
    }

    #endregion

    #region Generated Regex

    [GeneratedRegex(@"\{\{(.+?)\}\}")]
    private static partial Regex InterpolationRegex();

    [GeneratedRegex(
        @"((?:v-[\w-]+(?:\:[\w-]+)?(?:\.[\w-]+)*|@[\w-]+(?:\.[\w-]+)*|:[\w-]+(?:\.[\w-]+)*|#[\w-]+|[\w-]+)(?:=""([^""]*)""|='([^']*)')?)")]
    private static partial Regex VueAttributeRegex();

    #endregion
}
