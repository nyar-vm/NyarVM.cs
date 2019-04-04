using Oak.Wat.Ast;

namespace Oak.Wat;

/// <summary>
///     WAT 语言语法分析器，将 Token 流解析为 AST
/// </summary>
public sealed class WatParser
{
    private readonly WatLexer _lexer;
    private WatTokenStream _tokens = null!;


    /// <summary>
    ///     初始化 WatParser 的新实例
    /// </summary>
    public WatParser()
    {
        _lexer = new WatLexer();
    }


    /// <summary>
    ///     解析 WAT 源码并生成 AST
    /// </summary>
    /// <param name="code">WAT 源码。</param>
    /// <returns>解析生成的模块 AST。</returns>
    public WatModule Parse(string code)
    {
        var tokens = _lexer.Tokenize(code);
        _tokens = new WatTokenStream(tokens);
        return ParseModule();
    }

    #region 类型定义解析

    /// <summary>
    ///     解析类型定义 (type ...)
    /// </summary>
    private WatTypeDefinition ParseType()
    {
        _tokens.Expect(WatTokenType.Keyword, "type");

        if (_tokens.Check(WatTokenType.Identifier)) _tokens.Advance();

        var parameters = new List<string>();
        var results = new List<string>();

        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken is { Type: WatTokenType.Keyword, Value: "param" })
            {
                _tokens.Advance();
                while (_tokens.Check(WatTokenType.ValueType)) parameters.Add(_tokens.Advance().Value);
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "result" })
            {
                _tokens.Advance();
                while (_tokens.Check(WatTokenType.ValueType)) results.Add(_tokens.Advance().Value);
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else
            {
                while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd()) _tokens.Advance();
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
        }

        return new WatTypeDefinition(parameters, results);
    }

    #endregion

    #region 导出解析

    /// <summary>
    ///     解析导出声明 (export "name" (func/memory/table/global idx))
    /// </summary>
    private WatExport ParseExport()
    {
        _tokens.Expect(WatTokenType.Keyword, "export");

        var name = "";
        if (_tokens.Check(WatTokenType.StringLiteral)) name = _tokens.Advance().Value;

        var exportKind = "func";
        uint index = 0;

        if (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken.Type == WatTokenType.Keyword)
            {
                exportKind = innerToken.Value;
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number))
                    index = ParseIndexValue(_tokens.Advance().Value);
            }

            _tokens.Expect(WatTokenType.Punctuation, ")");
        }

        return new WatExport(name, exportKind, index);
    }

    #endregion

    #region 模块解析

    /// <summary>
    ///     解析 (module ...) 顶层结构
    /// </summary>
    private WatModule ParseModule()
    {
        _tokens.Expect(WatTokenType.Punctuation, "(");
        _tokens.Expect(WatTokenType.Keyword, "module");

        string? name = null;
        if (_tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.StringLiteral))
            name = _tokens.Advance().Value;

        var imports = new List<WatImport>();
        var exports = new List<WatExport>();
        var functions = new List<WatFunction>();
        var memories = new List<WatMemory>();
        var tables = new List<WatTable>();
        var globals = new List<WatGlobal>();
        var dataSegments = new List<WatDataSegment>();
        var types = new List<WatTypeDefinition>();
        var elemSegments = new List<WatElemSegment>();
        WatStart? start = null;

        while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd())
        {
            var field = ParseModuleField();
            switch (field)
            {
                case WatImport i:
                    imports.Add(i);
                    break;
                case WatExport e:
                    exports.Add(e);
                    break;
                case WatFunction f:
                    functions.Add(f);
                    break;
                case WatMemory m:
                    memories.Add(m);
                    break;
                case WatTable t:
                    tables.Add(t);
                    break;
                case WatGlobal g:
                    globals.Add(g);
                    break;
                case WatDataSegment d:
                    dataSegments.Add(d);
                    break;
                case WatTypeDefinition td:
                    types.Add(td);
                    break;
                case WatElemSegment es:
                    elemSegments.Add(es);
                    break;
                case WatStart s:
                    start = s;
                    break;
            }
        }

        _tokens.Expect(WatTokenType.Punctuation, ")");
        return new WatModule(default, name, imports, exports, functions, memories, tables, globals, dataSegments, types,
            elemSegments, start);
    }


    /// <summary>
    ///     解析模块字段，分派到具体的解析方法
    /// </summary>
    private WatAstNode? ParseModuleField()
    {
        _tokens.Expect(WatTokenType.Punctuation, "(");
        var token = _tokens.Current;

        WatAstNode? field = null;
        if (token.Type == WatTokenType.Keyword)
            field = token.Value switch
            {
                "func" => ParseFunc(),
                "type" => ParseType(),
                "import" => ParseImport(),
                "export" => ParseExport(),
                "memory" => ParseMemory(),
                "table" => ParseTable(),
                "global" => ParseGlobal(),
                "data" => ParseData(),
                "elem" => ParseElem(),
                "start" => ParseStart(),
                _ => null
            };

        if (field is null)
            while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd())
                _tokens.Advance();

        _tokens.Expect(WatTokenType.Punctuation, ")");
        return field;
    }

    #endregion

    #region 函数解析

    /// <summary>
    ///     解析函数定义
    /// </summary>
    private WatFunction ParseFunc()
    {
        _tokens.Expect(WatTokenType.Keyword, "func");

        string? name = null;
        if (_tokens.Check(WatTokenType.Identifier))
            name = _tokens.Advance().Value;
        else if (_tokens.Check(WatTokenType.StringLiteral)) name = _tokens.Advance().Value;

        string? exportName = null;
        string? importModule = null;
        string? importName = null;
        var parameters = new List<WatParameter>();
        var results = new List<string>();
        var locals = new List<WatLocal>();
        var instructions = new List<WatInstruction>();

        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken is { Type: WatTokenType.Keyword, Value: "export" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) exportName = _tokens.Advance().Value;
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "import" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) importModule = _tokens.Advance().Value;
                if (_tokens.Check(WatTokenType.StringLiteral)) importName = _tokens.Advance().Value;
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "type" })
            {
                _tokens.Advance();
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "param" })
            {
                parameters.Add(ParseParam());
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "result" })
            {
                results.Add(ParseResult());
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "local" })
            {
                locals.Add(ParseLocal());
            }
            else
            {
                var instr = ParseInstructionInParens();
                if (instr is not null) instructions.Add(instr);
            }
        }

        while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd())
        {
            var instr = ParseInstruction();
            if (instr is not null) instructions.Add(instr);
        }

        return new WatFunction(default, name, parameters, results, locals, instructions, exportName, importModule,
            importName);
    }


    /// <summary>
    ///     解析参数声明 (param ...)
    /// </summary>
    private WatParameter ParseParam()
    {
        _tokens.Expect(WatTokenType.Keyword, "param");
        string? name = null;
        var valueType = "i32";

        if (_tokens.Check(WatTokenType.Identifier)) name = _tokens.Advance().Value;

        if (_tokens.Check(WatTokenType.ValueType)) valueType = _tokens.Advance().Value;

        _tokens.Expect(WatTokenType.Punctuation, ")");
        return new WatParameter(valueType, name);
    }


    /// <summary>
    ///     解析返回值声明 (result ...)，返回值类型字符串
    /// </summary>
    private string ParseResult()
    {
        _tokens.Expect(WatTokenType.Keyword, "result");
        var resultType = "i32";
        if (_tokens.Check(WatTokenType.ValueType)) resultType = _tokens.Advance().Value;
        _tokens.Expect(WatTokenType.Punctuation, ")");
        return resultType;
    }


    /// <summary>
    ///     解析局部变量声明 (local ...)
    /// </summary>
    private WatLocal ParseLocal()
    {
        _tokens.Expect(WatTokenType.Keyword, "local");
        string? name = null;
        var valueType = "i32";

        if (_tokens.Check(WatTokenType.Identifier)) name = _tokens.Advance().Value;

        if (_tokens.Check(WatTokenType.ValueType)) valueType = _tokens.Advance().Value;

        _tokens.Expect(WatTokenType.Punctuation, ")");
        return new WatLocal(valueType, name);
    }

    #endregion

    #region 导入解析

    /// <summary>
    ///     解析导入声明 (import "module" "name" (func/memory/table/global ...))
    /// </summary>
    private WatImport ParseImport()
    {
        _tokens.Expect(WatTokenType.Keyword, "import");

        var module = "";
        var field = "";

        if (_tokens.Check(WatTokenType.StringLiteral)) module = _tokens.Advance().Value;
        if (_tokens.Check(WatTokenType.StringLiteral)) field = _tokens.Advance().Value;

        WatAstNode descriptor = new WatFuncImportDescriptor(default, null, null, [], []);

        if (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            descriptor = ParseImportDescriptor() ?? descriptor;
            _tokens.Expect(WatTokenType.Punctuation, ")");
        }

        return new WatImport(default, module, field, descriptor);
    }


    /// <summary>
    ///     解析导入描述，分派到具体的描述解析方法
    /// </summary>
    private WatAstNode? ParseImportDescriptor()
    {
        var token = _tokens.Current;
        if (token.Type == WatTokenType.Keyword)
            return token.Value switch
            {
                "func" => ParseFuncImportDescriptor(),
                "memory" => ParseMemoryImportDescriptor(),
                "table" => ParseTableImportDescriptor(),
                "global" => ParseGlobalImportDescriptor(),
                _ => null
            };
        return null;
    }


    /// <summary>
    ///     解析函数导入描述 (func ...)
    /// </summary>
    private WatFuncImportDescriptor ParseFuncImportDescriptor()
    {
        _tokens.Expect(WatTokenType.Keyword, "func");

        string? id = null;
        if (_tokens.Check(WatTokenType.Identifier)) id = _tokens.Advance().Value;

        string? typeRef = null;
        if (_tokens.Check(WatTokenType.Identifier)) typeRef = _tokens.Advance().Value;

        var parameters = new List<WatParameter>();
        var results = new List<string>();

        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken is { Type: WatTokenType.Keyword, Value: "param" })
            {
                parameters.Add(ParseParam());
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "result" })
            {
                results.Add(ParseResult());
            }
            else
            {
                while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd()) _tokens.Advance();
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
        }

        return new WatFuncImportDescriptor(default, id, typeRef, parameters, results);
    }


    /// <summary>
    ///     解析内存导入描述 (memory ...)
    /// </summary>
    private WatMemoryImportDescriptor ParseMemoryImportDescriptor()
    {
        _tokens.Expect(WatTokenType.Keyword, "memory");

        string? id = null;
        if (_tokens.Check(WatTokenType.Identifier)) id = _tokens.Advance().Value;

        var minPages = 0;
        if (_tokens.Check(WatTokenType.Number)) minPages = int.Parse(_tokens.Advance().Value);

        int? maxPages = null;
        if (_tokens.Check(WatTokenType.Number)) maxPages = int.Parse(_tokens.Advance().Value);

        return new WatMemoryImportDescriptor(default, id, minPages, maxPages);
    }


    /// <summary>
    ///     解析表导入描述 (table ...)
    /// </summary>
    private WatTableImportDescriptor ParseTableImportDescriptor()
    {
        _tokens.Expect(WatTokenType.Keyword, "table");

        string? id = null;
        if (_tokens.Check(WatTokenType.Identifier)) id = _tokens.Advance().Value;

        var elementType = "funcref";
        if (IsRefType(_tokens.Current.Value) && _tokens.Current.Type == WatTokenType.ValueType)
            elementType = _tokens.Advance().Value;

        var minSize = 0;
        if (_tokens.Check(WatTokenType.Number)) minSize = int.Parse(_tokens.Advance().Value);

        int? maxSize = null;
        if (_tokens.Check(WatTokenType.Number)) maxSize = int.Parse(_tokens.Advance().Value);

        return new WatTableImportDescriptor(default, id, elementType, minSize, maxSize);
    }


    /// <summary>
    ///     解析全局变量导入描述 (global ...)
    /// </summary>
    private WatGlobalImportDescriptor ParseGlobalImportDescriptor()
    {
        _tokens.Expect(WatTokenType.Keyword, "global");

        string? id = null;
        if (_tokens.Check(WatTokenType.Identifier)) id = _tokens.Advance().Value;

        var isMutable = false;
        var valueType = "i32";

        if (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            _tokens.Expect(WatTokenType.Keyword, "mut");
            isMutable = true;
            if (_tokens.Check(WatTokenType.ValueType)) valueType = _tokens.Advance().Value;
            _tokens.Expect(WatTokenType.Punctuation, ")");
        }
        else if (_tokens.Check(WatTokenType.ValueType))
        {
            valueType = _tokens.Advance().Value;
        }

        return new WatGlobalImportDescriptor(default, id, isMutable, valueType);
    }

    #endregion

    #region 内存/表/全局变量解析

    /// <summary>
    ///     解析内存声明 (memory ...)
    /// </summary>
    private WatMemory ParseMemory()
    {
        _tokens.Expect(WatTokenType.Keyword, "memory");

        string? name = null;
        if (_tokens.Check(WatTokenType.Identifier)) name = _tokens.Advance().Value;

        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken is { Type: WatTokenType.Keyword, Value: "export" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) _tokens.Advance();
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "import" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) _tokens.Advance();
            }

            _tokens.Expect(WatTokenType.Punctuation, ")");
        }

        uint initialPages = 0;
        if (_tokens.Check(WatTokenType.Number)) initialPages = uint.Parse(_tokens.Advance().Value);

        uint? maxPages = null;
        if (_tokens.Check(WatTokenType.Number)) maxPages = uint.Parse(_tokens.Advance().Value);

        return new WatMemory(initialPages, maxPages, name);
    }


    /// <summary>
    ///     解析表声明 (table ...)
    /// </summary>
    private WatTable ParseTable()
    {
        _tokens.Expect(WatTokenType.Keyword, "table");

        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken is { Type: WatTokenType.Keyword, Value: "export" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) _tokens.Advance();
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "import" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) _tokens.Advance();
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "elem" })
            {
                _tokens.Advance();
                while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd()) _tokens.Advance();
            }

            _tokens.Expect(WatTokenType.Punctuation, ")");
        }

        var elementType = "funcref";
        if (_tokens.Check(WatTokenType.ValueType) && IsRefType(_tokens.Current.Value))
            elementType = _tokens.Advance().Value;

        uint initialSize = 0;
        if (_tokens.Check(WatTokenType.Number)) initialSize = uint.Parse(_tokens.Advance().Value);

        uint? maxSize = null;
        if (_tokens.Check(WatTokenType.Number)) maxSize = uint.Parse(_tokens.Advance().Value);

        return new WatTable(elementType, initialSize, maxSize);
    }


    /// <summary>
    ///     解析全局变量声明 (global ...)
    /// </summary>
    private WatGlobal ParseGlobal()
    {
        _tokens.Expect(WatTokenType.Keyword, "global");

        string? name = null;
        if (_tokens.Check(WatTokenType.Identifier)) name = _tokens.Advance().Value;

        var isMutable = false;
        var valueType = "i32";
        var initExpression = new List<WatInstruction>();

        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken is { Type: WatTokenType.Keyword, Value: "export" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) _tokens.Advance();
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "import" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) _tokens.Advance();
                if (_tokens.Check(WatTokenType.StringLiteral)) _tokens.Advance();
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "mut" })
            {
                isMutable = true;
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.ValueType)) valueType = _tokens.Advance().Value;
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else
            {
                var instr = ParseInstructionInParens();
                if (instr is not null) initExpression.Add(instr);
            }
        }

        if (!isMutable && _tokens.Check(WatTokenType.ValueType)) valueType = _tokens.Advance().Value;

        while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd())
        {
            var instr = ParseInstruction();
            if (instr is not null) initExpression.Add(instr);
        }

        return new WatGlobal(valueType, isMutable, initExpression, name);
    }

    #endregion

    #region 数据段/元素段/start 解析

    /// <summary>
    ///     解析数据段 (data ...)
    /// </summary>
    private WatDataSegment ParseData()
    {
        _tokens.Expect(WatTokenType.Keyword, "data");

        if (_tokens.Check(WatTokenType.Identifier)) _tokens.Advance();

        uint memoryIndex = 0;
        if (_tokens.Check(WatTokenType.Number)) memoryIndex = uint.Parse(_tokens.Advance().Value);

        var offset = new List<WatInstruction>();
        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var instr = ParseInstructionInParens();
            if (instr is not null) offset.Add(instr);
        }

        byte[] data = [];
        if (_tokens.Check(WatTokenType.StringLiteral))
        {
            var stringValue = _tokens.Advance().Value;
            data = DecodeWatString(stringValue);
        }

        return new WatDataSegment(memoryIndex, offset, data);
    }


    /// <summary>
    ///     解析元素段 (elem ...)
    /// </summary>
    private WatElemSegment ParseElem()
    {
        _tokens.Expect(WatTokenType.Keyword, "elem");

        string? table = null;
        if (_tokens.Check(WatTokenType.Identifier)) table = _tokens.Advance().Value;

        string? offset = null;
        var elements = new List<string>();

        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken is { Type: WatTokenType.Keyword, Value: "item" })
            {
                _tokens.Advance();
                while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd())
                    if (_tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number))
                        elements.Add(_tokens.Advance().Value);
                    else
                        _tokens.Advance();
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "offset" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.Opcode)) offset = _tokens.Advance().Value;
                while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd()) _tokens.Advance();
            }
            else
            {
                var instr = ParseInstructionInParens();
                if (instr is not null) offset ??= instr.Opcode;
            }
        }

        if (_tokens.Check(WatTokenType.ValueType) && IsRefType(_tokens.Current.Value)) _tokens.Advance();

        while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd())
            if (_tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number))
                elements.Add(_tokens.Advance().Value);
            else
                _tokens.Advance();

        return new WatElemSegment(default, table, offset, elements);
    }


    /// <summary>
    ///     解析 start 函数声明 (start $func)
    /// </summary>
    private WatStart ParseStart()
    {
        _tokens.Expect(WatTokenType.Keyword, "start");

        var function = "";
        if (_tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number))
            function = _tokens.Advance().Value;

        return new WatStart(default, function);
    }

    #endregion

    #region 指令解析

    /// <summary>
    ///     解析指令，可能是折叠式或平铺式
    /// </summary>
    private WatInstruction? ParseInstruction()
    {
        if (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            return ParseInstructionInParens();
        }

        return ParsePlainInstruction();
    }


    /// <summary>
    ///     解析括号内的折叠式指令
    /// </summary>
    private WatInstruction? ParseInstructionInParens()
    {
        var token = _tokens.Current;
        WatInstruction? instruction = null;

        if (token.Type == WatTokenType.Opcode)
            instruction = IsConstOpcode(token.Value) ? ParseConstInstruction()
                : IsVariableOpcode(token.Value) ? ParseVariableInstruction()
                : IsCallOpcode(token.Value) ? ParseCallInstruction()
                : IsControlOpcode(token.Value) ? ParseControlInstruction()
                : IsMemoryOpcode(token.Value) ? ParseMemoryInstruction()
                : IsSimpleOpcode(token.Value) ? ParseSimpleInstruction()
                : ParseGenericInstruction();
        else if (token.Type == WatTokenType.Keyword && IsControlKeyword(token.Value))
            instruction = ParseControlInstruction();

        _tokens.Expect(WatTokenType.Punctuation, ")");
        return instruction;
    }


    /// <summary>
    ///     解析平铺式指令（不在括号内的指令）
    /// </summary>
    private WatInstruction? ParsePlainInstruction()
    {
        var token = _tokens.Current;

        if (token.Type != WatTokenType.Opcode && token.Type != WatTokenType.Keyword) return null;

        var opcode = token.Value;
        _tokens.Advance();

        if (IsConstOpcode(opcode))
        {
            var value = "";
            if (_tokens.Check(WatTokenType.Number)) value = _tokens.Advance().Value;
            return new WatConstInstruction(default, opcode, GetConstType(opcode), value);
        }

        if (IsVariableOpcode(opcode))
        {
            var variable = "";
            if (_tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number))
                variable = _tokens.Advance().Value;
            return new WatVariableInstruction(default, opcode, variable);
        }

        if (IsBinaryOpcode(opcode)) return new WatBinaryInstruction(default, opcode);

        if (IsUnaryOpcode(opcode)) return new WatUnaryInstruction(default, opcode);

        if (IsCompareOpcode(opcode)) return new WatCompareInstruction(default, opcode);

        if (IsMemoryOpcode(opcode))
        {
            var align = _tokens.Check(WatTokenType.Number) ? _tokens.Advance().Value : null;
            var offset = _tokens.Check(WatTokenType.Number) ? _tokens.Advance().Value : null;
            return new WatMemoryInstruction(default, opcode, align, offset);
        }

        if (IsSimpleOpcode(opcode)) return new WatSimpleInstruction(default, opcode);

        if (IsCallOpcode(opcode))
        {
            var function = _tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number)
                ? _tokens.Advance().Value
                : null;
            return new WatCallInstruction(default, opcode, function, null, null, null);
        }

        if (IsControlOpcode(opcode) || IsControlKeyword(opcode))
        {
            var label = _tokens.Check(WatTokenType.Identifier) ? _tokens.Advance().Value : null;
            return new WatControlInstruction(default, opcode, label, null, [], null, null);
        }

        return new WatGenericInstruction(default, opcode, []);
    }


    /// <summary>
    ///     解析常量指令（i32.const, i64.const, f32.const, f64.const）
    /// </summary>
    private WatConstInstruction ParseConstInstruction()
    {
        var token = _tokens.Advance();
        var value = "";
        if (_tokens.Check(WatTokenType.Number)) value = _tokens.Advance().Value;
        return new WatConstInstruction(default, token.Value, GetConstType(token.Value), value);
    }


    /// <summary>
    ///     解析变量指令（local.get, local.set, local.tee, global.get, global.set）
    /// </summary>
    private WatVariableInstruction ParseVariableInstruction()
    {
        var token = _tokens.Advance();
        var variable = "";
        if (_tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number))
            variable = _tokens.Advance().Value;
        return new WatVariableInstruction(default, token.Value, variable);
    }


    /// <summary>
    ///     解析调用指令（call, call_indirect）
    /// </summary>
    private WatCallInstruction ParseCallInstruction()
    {
        var token = _tokens.Advance();
        var function = _tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number)
            ? _tokens.Advance().Value
            : null;
        string? typeRef = null;
        var parameters = new List<WatParameter>();
        var results = new List<string>();

        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken is { Type: WatTokenType.Keyword, Value: "param" })
            {
                parameters.Add(ParseParam());
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "result" })
            {
                results.Add(ParseResult());
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "type" })
            {
                _tokens.Advance();
                if (_tokens.Check(WatTokenType.Identifier)) typeRef = _tokens.Advance().Value;
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else
            {
                while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd()) _tokens.Advance();
                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
        }

        return new WatCallInstruction(default, token.Value, function, typeRef, parameters, results);
    }


    /// <summary>
    ///     解析控制流指令（block, loop, if, br, br_if, br_table）
    /// </summary>
    private WatControlInstruction ParseControlInstruction()
    {
        var token = _tokens.Advance();
        var opcode = token.Value;

        var label = _tokens.Check(WatTokenType.Identifier) ? _tokens.Advance().Value : null;
        var results = new List<string>();
        var body = new List<WatInstruction>();
        var elseBody = new List<WatInstruction>();
        var targets = new List<string>();

        while (_tokens.Check(WatTokenType.Punctuation, "("))
        {
            _tokens.Advance();
            var innerToken = _tokens.Current;

            if (innerToken is { Type: WatTokenType.Keyword, Value: "result" })
            {
                results.Add(ParseResult());
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "then" })
            {
                _tokens.Advance();
                while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd())
                {
                    var innerInstr = ParseInstruction();
                    if (innerInstr is not null) body.Add(innerInstr);
                }

                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else if (innerToken is { Type: WatTokenType.Keyword, Value: "else" })
            {
                _tokens.Advance();
                while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd())
                {
                    var innerInstr = ParseInstruction();
                    if (innerInstr is not null) elseBody.Add(innerInstr);
                }

                _tokens.Expect(WatTokenType.Punctuation, ")");
            }
            else
            {
                var innerInstr = ParseInstructionInParens();
                if (innerInstr is not null) body.Add(innerInstr);
            }
        }

        if (opcode is "br" or "br_if")
        {
            if (_tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number))
                targets.Add(_tokens.Advance().Value);
        }
        else if (opcode == "br_table")
        {
            while (_tokens.Check(WatTokenType.Identifier) || _tokens.Check(WatTokenType.Number))
                targets.Add(_tokens.Advance().Value);
        }

        var elseBodyOrNull = elseBody.Count > 0 ? elseBody : null;
        var targetsOrNull = targets.Count > 0 ? targets : null;
        var resultsOrNull = results.Count > 0 ? results : null;

        return new WatControlInstruction(default, opcode, label, resultsOrNull, body, elseBodyOrNull, targetsOrNull);
    }


    /// <summary>
    ///     解析内存指令（各种 load/store）
    /// </summary>
    private WatMemoryInstruction ParseMemoryInstruction()
    {
        var token = _tokens.Advance();

        string? align = null;
        if (_tokens.Check(WatTokenType.Number)) align = _tokens.Advance().Value;

        string? offset = null;
        if (_tokens.Check(WatTokenType.Number)) offset = _tokens.Advance().Value;

        return new WatMemoryInstruction(default, token.Value, align, offset);
    }


    /// <summary>
    ///     解析简单指令（drop, select, return, nop, unreachable）
    /// </summary>
    private WatSimpleInstruction ParseSimpleInstruction()
    {
        var token = _tokens.Advance();
        return new WatSimpleInstruction(default, token.Value);
    }


    /// <summary>
    ///     解析通用指令（回退处理）
    /// </summary>
    private WatInstruction ParseGenericInstruction()
    {
        var token = _tokens.Advance();
        var opcode = token.Value;

        if (IsBinaryOpcode(opcode))
        {
            while (_tokens.Check(WatTokenType.Punctuation, "("))
            {
                _tokens.Advance();
                var inner = ParseInstructionInParens();
            }

            return new WatBinaryInstruction(default, opcode);
        }

        if (IsUnaryOpcode(opcode))
        {
            while (_tokens.Check(WatTokenType.Punctuation, "("))
            {
                _tokens.Advance();
                var inner = ParseInstructionInParens();
            }

            return new WatUnaryInstruction(default, opcode);
        }

        if (IsCompareOpcode(opcode))
        {
            while (_tokens.Check(WatTokenType.Punctuation, "("))
            {
                _tokens.Advance();
                var inner = ParseInstructionInParens();
            }

            return new WatCompareInstruction(default, opcode);
        }

        var operands = new List<string>();
        while (!_tokens.Check(WatTokenType.Punctuation, ")") && !_tokens.IsAtEnd())
            if (_tokens.Check(WatTokenType.Number) || _tokens.Check(WatTokenType.Identifier))
                operands.Add(_tokens.Advance().Value);
            else
                break;

        return new WatGenericInstruction(default, opcode, operands);
    }

    #endregion

    #region 指令分类辅助方法

    /// <summary>
    ///     判断是否为常量指令操作码
    /// </summary>
    private static bool IsConstOpcode(string opcode)
    {
        return opcode is "i32.const" or "i64.const" or "f32.const" or "f64.const";
    }


    /// <summary>
    ///     判断是否为变量指令操作码
    /// </summary>
    private static bool IsVariableOpcode(string opcode)
    {
        return opcode is "local.get" or "local.set" or "local.tee" or "global.get" or "global.set";
    }


    /// <summary>
    ///     判断是否为调用指令操作码
    /// </summary>
    private static bool IsCallOpcode(string opcode)
    {
        return opcode is "call" or "call_indirect";
    }


    /// <summary>
    ///     判断是否为控制流指令操作码
    /// </summary>
    private static bool IsControlOpcode(string opcode)
    {
        return opcode is "br" or "br_if" or "br_table";
    }


    /// <summary>
    ///     判断是否为控制流关键字（block/loop/if 在 WAT 中是关键字）
    /// </summary>
    private static bool IsControlKeyword(string value)
    {
        return value is "block" or "loop" or "if";
    }


    /// <summary>
    ///     判断是否为内存指令操作码
    /// </summary>
    private static bool IsMemoryOpcode(string opcode)
    {
        return opcode.EndsWith(".load") || opcode.EndsWith(".store") ||
               opcode.Contains(".load") || opcode.Contains(".store") ||
               opcode is "memory.size" or "memory.grow" or "memory.fill" or "memory.copy" or "memory.init";
    }


    /// <summary>
    ///     判断是否为简单指令操作码
    /// </summary>
    private static bool IsSimpleOpcode(string opcode)
    {
        return opcode is "drop" or "select" or "return" or "nop" or "unreachable";
    }


    /// <summary>
    ///     判断是否为二元运算指令操作码
    /// </summary>
    private static bool IsBinaryOpcode(string opcode)
    {
        var dotIndex = opcode.IndexOf('.');
        if (dotIndex < 0) return false;

        var suffix = opcode[(dotIndex + 1)..];
        return suffix is "add" or "sub" or "mul" or "div" or "rem" or "and" or "or" or "xor"
            or "shl" or "shr" or "rotl" or "rotr"
            or "div_s" or "div_u" or "rem_s" or "rem_u" or "shr_s" or "shr_u"
            or "min" or "max" or "copysign";
    }


    /// <summary>
    ///     判断是否为比较指令操作码
    /// </summary>
    private static bool IsCompareOpcode(string opcode)
    {
        var dotIndex = opcode.IndexOf('.');
        if (dotIndex < 0) return false;

        var suffix = opcode[(dotIndex + 1)..];
        return suffix is "eq" or "ne" or "lt" or "gt" or "le" or "ge" or "eqz"
            or "lt_s" or "lt_u" or "gt_s" or "gt_u" or "le_s" or "le_u" or "ge_s" or "ge_u";
    }


    /// <summary>
    ///     判断是否为一元运算指令操作码
    /// </summary>
    private static bool IsUnaryOpcode(string opcode)
    {
        var dotIndex = opcode.IndexOf('.');
        if (dotIndex < 0) return false;

        var suffix = opcode[(dotIndex + 1)..];
        return suffix is "clz" or "ctz" or "popcnt" or "abs" or "neg" or "sqrt"
            or "ceil" or "floor" or "trunc" or "nearest"
            or "extend_i32_s" or "extend_i32_u" or "wrap_i64"
            or "convert_i32_s" or "convert_i32_u" or "convert_i64_s" or "convert_i64_u"
            or "promote_f32" or "demote_f64"
            or "reinterpret_i32" or "reinterpret_i64" or "reinterpret_f32" or "reinterpret_f64";
    }


    /// <summary>
    ///     判断是否为值类型字符串
    /// </summary>
    private static bool IsValueType(string value)
    {
        return value is "i32" or "i64" or "f32" or "f64" or "funcref" or "externref" or "v128";
    }


    /// <summary>
    ///     判断是否为引用类型字符串
    /// </summary>
    private static bool IsRefType(string value)
    {
        return value is "funcref" or "externref";
    }


    /// <summary>
    ///     获取常量指令的类型前缀
    /// </summary>
    private static string GetConstType(string opcode)
    {
        return opcode switch
        {
            "i32.const" => "i32",
            "i64.const" => "i64",
            "f32.const" => "f32",
            "f64.const" => "f64",
            _ => "unknown"
        };
    }


    /// <summary>
    ///     将标识符或数字字符串解析为索引值
    /// </summary>
    private static uint ParseIndexValue(string value)
    {
        if (value.StartsWith('$')) return 0;
        return uint.TryParse(value, out var result) ? result : 0;
    }


    /// <summary>
    ///     解码 WAT 字符串字面量为字节数组
    /// </summary>
    private static byte[] DecodeWatString(string value)
    {
        if (value.StartsWith('"') && value.EndsWith('"')) value = value[1..^1];

        var result = new List<byte>();
        for (var i = 0; i < value.Length; i++)
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                i++;
                var escaped = value[i] switch
                {
                    'n' => (byte)'\n',
                    'r' => (byte)'\r',
                    't' => (byte)'\t',
                    '"' => (byte)'"',
                    '\\' => (byte)'\\',
                    '\'' => (byte)'\'',
                    _ => (byte)value[i]
                };
                result.Add(escaped);
            }
            else
            {
                result.Add((byte)value[i]);
            }

        return [.. result];
    }

    #endregion
}