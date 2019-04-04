using System.Collections.Immutable;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// TextVM 模式语法的递归下降解析器。
/// 支持操作符优先级（从低到高）：|、&amp;、!、-、*+?。
/// </summary>
public static class PatternParser
{
    /// <summary>
    /// 解析指定的模式字符串，返回 AST 根节点及可选的错误信息。
    /// </summary>
    /// <param name="pattern">要解析的正则模式字符串。</param>
    /// <returns>包含 AST 根节点和错误信息的元组。如果解析成功，error 为 null。</returns>
    public static (AstNode? Node, String? Error) Parse(String pattern)
    {
        if (pattern == null)
        {
            return (null, "模式字符串不能为 null");
        }

        ParserState parser = new ParserState(pattern);
        AstNode? result = parser.ParsePattern();

        if (parser.Error != null)
        {
            return (null, parser.Error);
        }

        if (parser.Position < pattern.Length)
        {
            return (null, $"位置 {parser.Position} 处存在无法识别的字符 '{pattern[parser.Position]}'");
        }

        if (result == null)
        {
            return (null, "模式为空");
        }

        return (result, null);
    }

    /// <summary>
    /// 解析器内部状态。
    /// </summary>
    private sealed class ParserState
    {
        private readonly String _pattern;

        /// <summary>
        /// 当前解析位置。
        /// </summary>
        public Int32 Position { get; private set; }

        /// <summary>
        /// 解析错误信息，为 null 表示无错误。
        /// </summary>
        public String? Error { get; private set; }

        /// <summary>
        /// 当前捕获组编号计数器。
        /// </summary>
        private Int32 _groupIdCounter;

        /// <summary>
        /// 使用指定的模式字符串初始化解析器状态。
        /// </summary>
        public ParserState(String pattern)
        {
            _pattern = pattern;
            Position = 0;
            _groupIdCounter = 0;
            Error = null;
        }

        /// <summary>
        /// 当前字符。
        /// </summary>
        private Char Current => Position < _pattern.Length ? _pattern[Position] : '\0';

        /// <summary>
        /// 是否已到达模式末尾。
        /// </summary>
        private Boolean IsAtEnd => Position >= _pattern.Length;

        /// <summary>
        /// 记录错误并返回 null。
        /// </summary>
        private AstNode? Fail(String message)
        {
            Error ??= message;
            return null;
        }

        /// <summary>
        /// 消耗当前字符并前进。
        /// </summary>
        private Char Advance()
        {
            Char c = _pattern[Position];
            Position++;
            return c;
        }

        /// <summary>
        /// 如果当前字符与期望值匹配，则消耗它。
        /// </summary>
        private Boolean Match(Char expected)
        {
            if (IsAtEnd || Current != expected)
            {
                return false;
            }

            Position++;
            return true;
        }

        /// <summary>
        /// 解析顶层模式：alternation。
        /// </summary>
        public AstNode? ParsePattern()
        {
            return ParseAlternation();
        }

        /// <summary>
        /// 解析 alternation（|），最低优先级。
        /// grammar: intersection ('|' intersection)*
        /// </summary>
        private AstNode? ParseAlternation()
        {
            AstNode? left = ParseIntersection();

            if (left == null && Error != null)
            {
                return null;
            }

            if (left == null)
            {
                return null;
            }

            while (!IsAtEnd && Current == '|')
            {
                Int32 opPos = Position;
                Advance();

                AstNode? right = ParseIntersection();

                if (right == null)
                {
                    return Fail(Error ?? $"位置 {opPos} 处操作符 '|' 后缺少表达式");
                }

                left = new AltNode(left, right)
                {
                    Position = left.Position,
                    Length = Position - left.Position,
                };
            }

            return left;
        }

        /// <summary>
        /// 解析 intersection（&amp;）。
        /// grammar: complement ('&amp;' complement)*
        /// </summary>
        private AstNode? ParseIntersection()
        {
            AstNode? left = ParseComplement();

            if (left == null && Error != null)
            {
                return null;
            }

            if (left == null)
            {
                return null;
            }

            while (!IsAtEnd && Current == '&')
            {
                Int32 opPos = Position;
                Advance();

                AstNode? right = ParseComplement();

                if (right == null)
                {
                    return Fail(Error ?? $"位置 {opPos} 处操作符 '&' 后缺少表达式");
                }

                left = new IntersectNode(left, right)
                {
                    Position = left.Position,
                    Length = Position - left.Position,
                };
            }

            return left;
        }

        /// <summary>
        /// 解析 complement（!），前缀操作符。
        /// grammar: '!' complement | concatDiff
        /// </summary>
        private AstNode? ParseComplement()
        {
            if (!IsAtEnd && Current == '!')
            {
                Int32 pos = Position;
                Advance();

                AstNode? inner = ParseComplement();

                if (inner == null)
                {
                    return Fail(Error ?? $"位置 {pos} 处操作符 '!' 后缺少表达式");
                }

                return new ComplementNode(inner)
                {
                    Position = pos,
                    Length = Position - pos,
                };
            }

            return ParseConcatDiff();
        }

        /// <summary>
        /// 解析 concatDiff，处理差操作符（-）和隐式连接。
        /// grammar: concat ('-' concat)*
        /// </summary>
        private AstNode? ParseConcatDiff()
        {
            AstNode? left = ParseConcat();

            if (left == null && Error != null)
            {
                return null;
            }

            // 处理起始位置的 '-' 作为字面量
            if (left == null && !IsAtEnd && Current == '-')
            {
                Int32 pos = Position;
                Advance();
                left = new LiteralNode("-")
                {
                    Position = pos,
                    Length = 1,
                };
            }

            if (left == null)
            {
                return null;
            }

            while (!IsAtEnd && Current == '-')
            {
                Int32 opPos = Position;
                Advance();

                AstNode? right = ParseConcat();

                if (right == null)
                {
                    return Fail(Error ?? $"位置 {opPos} 处差操作符 '-' 后缺少表达式");
                }

                left = new DifferenceNode(left, right)
                {
                    Position = left.Position,
                    Length = Position - left.Position,
                };
            }

            return left;
        }

        /// <summary>
        /// 解析 concat（隐式连接）。
        /// grammar: quantified+
        /// </summary>
        private AstNode? ParseConcat()
        {
            AstNode? first = ParseQuantified();

            if (first == null)
            {
                return null;
            }

            List<AstNode> children = [first];

            while (!IsAtEnd)
            {
                Char c = Current;

                // 这些字符由父级解析器处理
                if (c is '|' or '&' or '!' or ')' or ']' or '-')
                {
                    break;
                }

                // 在字符类中 '-' 可以是范围分隔符，但在这里 '-' 由 ParseConcatDiff 处理
                AstNode? next = ParseQuantified();

                if (next == null)
                {
                    break;
                }

                children.Add(next);
            }

            if (children.Count == 1)
            {
                return children[0];
            }

            return new ConcatNode([.. children])
            {
                Position = children[0].Position,
                Length = Position - children[0].Position,
            };
        }

        /// <summary>
        /// 解析 quantified（量词）。
        /// grammar: atom ('*' | '+' | '?')?
        /// </summary>
        private AstNode? ParseQuantified()
        {
            Int32 startPos = Position;

            AstNode? atom = ParseAtom();

            if (atom == null)
            {
                return null;
            }

            if (!IsAtEnd)
            {
                Char c = Current;

                switch (c)
                {
                    case '*':
                        Advance();
                        return new StarNode(atom)
                        {
                            Position = atom.Position,
                            Length = Position - atom.Position,
                        };

                    case '+':
                        Advance();
                        return new PlusNode(atom)
                        {
                            Position = atom.Position,
                            Length = Position - atom.Position,
                        };

                    case '?':
                        Advance();
                        return new OptionalNode(atom)
                        {
                            Position = atom.Position,
                            Length = Position - atom.Position,
                        };
                }
            }

            return atom;
        }

        /// <summary>
        /// 解析 atom（原子）。
        /// grammar: '(' pattern ')' | '[' charClass ']' | escape | '.' | '^' | '$' | literal
        /// </summary>
        private AstNode? ParseAtom()
        {
            if (IsAtEnd)
            {
                return null;
            }

            Char c = Current;

            switch (c)
            {
                case '(':
                    return ParseGroup();

                case '[':
                    return ParseCharClass();

                case '\\':
                    return ParseEscape();

                case '.':
                {
                    Int32 pos = Position;
                    Advance();
                    return new AnyNode
                    {
                        Position = pos,
                        Length = 1,
                    };
                }

                case '^':
                {
                    Int32 pos = Position;
                    Advance();
                    return new AnchorNode(AnchorKind.Start)
                    {
                        Position = pos,
                        Length = 1,
                    };
                }

                case '$':
                {
                    Int32 pos = Position;
                    Advance();
                    return new AnchorNode(AnchorKind.End)
                    {
                        Position = pos,
                        Length = 1,
                    };
                }

                // 这些字符不由 atom 处理
                case '|':
                case '&':
                case '!':
                case '*':
                case '+':
                case '?':
                case ')':
                case ']':
                case '-':
                    return null;

                default:
                {
                    Int32 pos = Position;
                    Advance();
                    return new LiteralNode(c.ToString())
                    {
                        Position = pos,
                        Length = 1,
                    };
                }
            }
        }

        /// <summary>
        /// 解析捕获组 (...) 或非捕获组 (?:...)。
        /// </summary>
        private AstNode? ParseGroup()
        {
            Int32 startPos = Position;
            Advance(); // 消耗 '('

            if (IsAtEnd)
            {
                return Fail("未闭合的组 '('");
            }

            AstNode? inner = ParsePattern();

            if (inner == null)
            {
                return Fail(Error ?? "组内模式为空");
            }

            if (IsAtEnd || Current != ')')
            {
                return Fail("缺少闭合的 ')'");
            }

            Advance(); // 消耗 ')'

            Int32 groupId = _groupIdCounter++;

            return new CaptureNode(inner, groupId)
            {
                Position = startPos,
                Length = Position - startPos,
            };
        }

        /// <summary>
        /// 解析字符类 [...] 或 [^...]。
        /// </summary>
        private AstNode? ParseCharClass()
        {
            Int32 startPos = Position;
            Advance(); // 消耗 '['

            Boolean negated = false;

            if (!IsAtEnd && Current == '^')
            {
                negated = true;
                Advance();
            }

            List<CharRange> ranges = [];
            Boolean first = true;

            while (!IsAtEnd && Current != ']')
            {
                // 处理 '-' 作为范围分隔符
                if (!first && Current == '-' && Position + 1 < _pattern.Length && _pattern[Position + 1] != ']')
                {
                    Int32 rangePos = Position;
                    Advance(); // 消耗 '-'

                    Char hi = ParseCharClassSingleChar();

                    if (Position == rangePos + 1)
                    {
                        // 没有解析到有效的范围上界
                        return Fail($"位置 {rangePos} 处字符类范围无效");
                    }

                    // 用 hi 更新最后一个范围的 Hi
                    CharRange prevRange = ranges[^1];
                    ranges[^1] = new CharRange(prevRange.Lo, hi);
                }
                else
                {
                    // 解析单个字符或转义序列（可能展开为多个范围）
                    Boolean expanded = ParseCharClassExpandedRange(ranges);

                    if (!expanded)
                    {
                        return Fail(Error ?? $"位置 {Position} 处字符类元素无效");
                    }
                }

                first = false;
            }

            if (IsAtEnd)
            {
                return Fail("未闭合的字符类 '['");
            }

            Advance(); // 消耗 ']'

            return new CharClassNode([.. ranges], negated)
            {
                Position = startPos,
                Length = Position - startPos,
            };
        }

        /// <summary>
        /// 解析字符类中的单个字符（非转义或简单转义）。
        /// </summary>
        private Char ParseCharClassSingleChar()
        {
            if (IsAtEnd)
            {
                return '\0';
            }

            if (Current == '\\')
            {
                Advance(); // 消耗 '\'

                if (IsAtEnd)
                {
                    return '\0';
                }

                Char esc = Advance();

                return esc switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '0' => '\0',
                    '\\' => '\\',
                    '-' => '-',
                    ']' => ']',
                    _ => esc,
                };
            }

            return Advance();
        }

        /// <summary>
        /// 解析字符类中的一个元素，可能展开为多个范围（如 \d、\w、\s）。
        /// 返回 true 表示成功解析了元素。
        /// </summary>
        private Boolean ParseCharClassExpandedRange(List<CharRange> ranges)
        {
            if (IsAtEnd)
            {
                return false;
            }

            if (Current == '\\')
            {
                Int32 startPos = Position;
                Advance(); // 消耗 '\'

                if (IsAtEnd)
                {
                    return false;
                }

                Char esc = Advance();

                switch (esc)
                {
                    case 'd':
                        ranges.Add(new CharRange('0', '9'));
                        return true;

                    case 'D':
                        // 非数字：所有非 0-9 的字符（用补集表示）
                        ranges.Add(new CharRange('\x00', '\x2F'));
                        ranges.Add(new CharRange(':', '\xFFFF'));
                        return true;

                    case 'w':
                        ranges.Add(new CharRange('0', '9'));
                        ranges.Add(new CharRange('A', 'Z'));
                        ranges.Add(new CharRange('a', 'z'));
                        ranges.Add(new CharRange('_', '_'));
                        return true;

                    case 'W':
                        ranges.Add(new CharRange('\x00', '\x2F'));
                        ranges.Add(new CharRange(':', '\x40'));
                        ranges.Add(new CharRange('[', '\x5E'));
                        ranges.Add(new CharRange('`', '`'));
                        ranges.Add(new CharRange('{', '\xFFFF'));
                        return true;

                    case 's':
                        ranges.Add(new CharRange(' ', ' '));
                        ranges.Add(new CharRange('\t', '\t'));
                        ranges.Add(new CharRange('\n', '\n'));
                        ranges.Add(new CharRange('\r', '\r'));
                        ranges.Add(new CharRange('\f', '\f'));
                        ranges.Add(new CharRange('\v', '\v'));
                        return true;

                    case 'S':
                        ranges.Add(new CharRange('\x00', '\x08'));
                        ranges.Add(new CharRange('\x0E', '\x1F'));
                        ranges.Add(new CharRange('!', '\uFFFF'));
                        return true;

                    case 'p':
                        // Unicode 属性，简化处理
                        if (!IsAtEnd && Current == '{')
                        {
                            Advance(); // 消耗 '{'

                            while (!IsAtEnd && Current != '}')
                            {
                                Advance();
                            }

                            if (!IsAtEnd)
                            {
                                Advance(); // 消耗 '}'
                            }

                            // 简化：对 \p{L} 使用常见的 Unicode 字母范围
                            ranges.Add(new CharRange('A', 'Z'));
                            ranges.Add(new CharRange('a', 'z'));
                            ranges.Add(new CharRange('\u00C0', '\u00D6'));
                            ranges.Add(new CharRange('\u00D8', '\u00F6'));
                            ranges.Add(new CharRange('\u00F8', '\u024F'));
                            ranges.Add(new CharRange('\u0370', '\u03FF'));
                            ranges.Add(new CharRange('\u0400', '\u04FF'));
                            ranges.Add(new CharRange('\u0500', '\u052F'));
                            ranges.Add(new CharRange('\u0530', '\u058F'));
                            ranges.Add(new CharRange('\u0600', '\u06FF'));
                            ranges.Add(new CharRange('\u1E00', '\u1EFF'));
                            ranges.Add(new CharRange('\u2C00', '\u2C5F'));
                            ranges.Add(new CharRange('\u3040', '\u309F'));
                            ranges.Add(new CharRange('\u30A0', '\u30FF'));
                            return true;
                        }

                        return false;

                    case 'n':
                        ranges.Add(new CharRange('\n', '\n'));
                        return true;

                    case 'r':
                        ranges.Add(new CharRange('\r', '\r'));
                        return true;

                    case 't':
                        ranges.Add(new CharRange('\t', '\t'));
                        return true;

                    case '\\':
                        ranges.Add(new CharRange('\\', '\\'));
                        return true;

                    case '-':
                        ranges.Add(new CharRange('-', '-'));
                        return true;

                    case ']':
                        ranges.Add(new CharRange(']', ']'));
                        return true;

                    default:
                        ranges.Add(new CharRange(esc, esc));
                        return true;
                }
            }

            // 非转义字符：添加为单字符区间
            Char c = Advance();
            ranges.Add(new CharRange(c, c));
            return true;
        }

        /// <summary>
        /// 解析转义序列。
        /// </summary>
        private AstNode? ParseEscape()
        {
            Int32 startPos = Position;
            Advance(); // 消耗 '\'

            if (IsAtEnd)
            {
                return Fail("不完整的转义序列");
            }

            Char c = Advance();

            switch (c)
            {
                // 字符类简写
                case 'd':
                    return new CharClassNode(
                        [new CharRange('0', '9')],
                        false)
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case 'D':
                    return new CharClassNode(
                        [
                            new CharRange('\x00', '\x2F'),
                            new CharRange(':', '\xFFFF')
                        ],
                        false)
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case 'w':
                    return new CharClassNode(
                        [
                            new CharRange('0', '9'),
                            new CharRange('A', 'Z'),
                            new CharRange('a', 'z'),
                            new CharRange('_', '_')
                        ],
                        false)
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case 'W':
                    return new CharClassNode(
                        [
                            new CharRange('\x00', '\x2F'),
                            new CharRange(':', '\x40'),
                            new CharRange('[', '\x5E'),
                            new CharRange('`', '`'),
                            new CharRange('{', '\xFFFF')
                        ],
                        false)
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case 's':
                    return new CharClassNode(
                        [
                            new CharRange(' ', ' '),
                            new CharRange('\t', '\t'),
                            new CharRange('\n', '\n'),
                            new CharRange('\r', '\r'),
                            new CharRange('\f', '\f'),
                            new CharRange('\v', '\v')
                        ],
                        false)
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case 'S':
                    return new CharClassNode(
                        [
                            new CharRange('\x00', '\x08'),
                            new CharRange('\x0E', '\x1F'),
                            new CharRange('!', '\uFFFF')
                        ],
                        false)
                    {
                        Position = startPos,
                        Length = 2,
                    };

                // 单词边界
                case 'b':
                    return new AnchorNode(AnchorKind.WordBoundary)
                    {
                        Position = startPos,
                        Length = 2,
                    };

                // 常见转义字符
                case 'n':
                    return new LiteralNode("\n")
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case 'r':
                    return new LiteralNode("\r")
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case 't':
                    return new LiteralNode("\t")
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case '\\':
                    return new LiteralNode("\\")
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case '.':
                    return new LiteralNode(".")
                    {
                        Position = startPos,
                        Length = 2,
                    };

                case '-':
                    return new LiteralNode("-")
                    {
                        Position = startPos,
                        Length = 2,
                    };

                // 反向引用 \1-\9
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                {
                    Int32 groupId = c - '0';
                    return new BackrefNode(groupId)
                    {
                        Position = startPos,
                        Length = 2,
                    };
                }

                // Unicode 属性 \p{L}
                case 'p':
                    if (!IsAtEnd && Current == '{')
                    {
                        Advance(); // 消耗 '{'

                        while (!IsAtEnd && Current != '}')
                        {
                            Advance();
                        }

                        if (!IsAtEnd)
                        {
                            Advance(); // 消耗 '}'
                        }

                        // 简化：返回常见 Unicode 字母范围
                        return new CharClassNode(
                            [
                                new CharRange('A', 'Z'),
                                new CharRange('a', 'z'),
                                new CharRange('\u00C0', '\u00D6'),
                                new CharRange('\u00D8', '\u00F6'),
                                new CharRange('\u00F8', '\u024F'),
                                new CharRange('\u0370', '\u03FF'),
                                new CharRange('\u0400', '\u04FF'),
                                new CharRange('\u0500', '\u052F'),
                                new CharRange('\u0530', '\u058F'),
                                new CharRange('\u0600', '\u06FF'),
                                new CharRange('\u1E00', '\u1EFF'),
                                new CharRange('\u2C00', '\u2C5F')
                            ],
                            false)
                        {
                            Position = startPos,
                            Length = Position - startPos,
                        };
                    }

                    return Fail($"位置 {startPos} 处无效的 Unicode 属性转义");

                // 其他转义作为字面量处理
                default:
                    return new LiteralNode(c.ToString())
                    {
                        Position = startPos,
                        Length = 2,
                    };
            }
        }
    }
}
